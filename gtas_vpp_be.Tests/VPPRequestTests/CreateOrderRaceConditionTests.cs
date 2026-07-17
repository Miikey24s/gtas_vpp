using System.Collections;
using System.Reflection;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Domain;
using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.DTOs.Req.VPP;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace gtas_vpp_be.Tests.VPPRequestTests;

public class CreateOrderRaceConditionTests
{
    [Fact]
    public async Task CreateOrder_TwoConcurrentRegular_OneSucceedsOneFails()
    {
        using var database = new SqliteTestDatabase();
        var userId = 5615;
        var vppId = Guid.NewGuid();
        using (var context = database.CreateContext())
        {
            await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
            await SeedOpenPeriodAsync(context, 2026, 4);
        }

        var request1 = CreateOrderRequest(2026, 4, isAdditionalOrder: false, vppId);
        var request2 = CreateOrderRequest(2026, 4, isAdditionalOrder: false, vppId);
        var barrier = new AsyncBarrier(2);

        // P1: Clock at day 10 ⇒ current period = April 2026, matching the requests' Y/M.
        var results = await Task.WhenAll(
            CaptureAsync(() => CreateService(database, new DateTime(2026, 4, 10, 9, 0, 0), barrier).CreateOrderAsync(request1, userId, "IT", "77500")),
            CaptureAsync(() => CreateService(database, new DateTime(2026, 4, 10, 9, 0, 0), barrier).CreateOrderAsync(request2, userId, "IT", "77500")));

        var outcomeDetails = string.Join(
            " | ",
            results.Select(result => result.Success
                ? "success"
                : DescribeException(result.Exception)));
        Assert.True(results.Count(x => x.Success) == 1, outcomeDetails);
        Assert.True(results.Count(x => x.Exception is ConflictException) == 1, outcomeDetails);

        using var verifyContext = database.CreateContext();
        Assert.Equal(1, await verifyContext.Set<VPP01_RequestHeader>()
            .CountAsync(x => x.CreateUserId == userId && x.Y == 2026 && x.M == 4 && !x.IsAdditionalOrder && !x.IsDeleted));
    }

    [Fact]
    public async Task CreateOrder_DuplicateRegularDifferentPeriod_BothSucceed()
    {
        using var database = new SqliteTestDatabase();
        var userId = 5615;
        var vppId = Guid.NewGuid();
        using (var context = database.CreateContext())
        {
            await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        }

        var first = CreateOrderRequest(2026, 4, isAdditionalOrder: false, vppId);
        var second = CreateOrderRequest(2026, 5, isAdditionalOrder: false, vppId);

        // P1: Use mid-month clocks so each Y/M matches the BE-computed current period.
        await CreateService(database, new DateTime(2026, 4, 10, 9, 0, 0)).CreateOrderAsync(first, userId, "IT", "77500");
        await CreateService(database, new DateTime(2026, 5, 10, 9, 0, 0)).CreateOrderAsync(second, userId, "IT", "77500");

        using var verifyContext = database.CreateContext();
        Assert.Equal(2, await verifyContext.Set<VPP01_RequestHeader>()
            .CountAsync(x => x.CreateUserId == userId && !x.IsAdditionalOrder && !x.IsDeleted));
    }

    [Fact]
    public async Task CreateOrder_OneRegularOneAdditional_BothSucceed()
    {
        using var database = new SqliteTestDatabase();
        var userId = 5615;
        var vppId = Guid.NewGuid();
        using (var context = database.CreateContext())
        {
            await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        }

        var regular = CreateOrderRequest(2026, 4, isAdditionalOrder: false, vppId);
        var additional = CreateOrderRequest(2026, 4, isAdditionalOrder: true, vppId);

        // Supplements belong to the same current period and must point to the
        // requester's submitted regular request.
        var baseOrder = await CreateService(database, new DateTime(2026, 4, 10, 9, 0, 0))
            .CreateOrderAsync(regular, userId, "IT", "77500");
        additional.BaseRequestId = baseOrder.Id;
        await CreateService(database, new DateTime(2026, 4, 10, 9, 0, 0))
            .CreateOrderAsync(additional, userId, "IT", "77500");

        using var verifyContext = database.CreateContext();
        Assert.Equal(2, await verifyContext.Set<VPP01_RequestHeader>()
            .CountAsync(x => x.CreateUserId == userId && x.Y == 2026 && x.M == 4 && !x.IsDeleted));
    }

    [Fact]
    public async Task CreateOrder_FourConcurrentSupplements_LeavesExactlyOnePending()
    {
        using var database = new SqliteTestDatabase();
        const int userId = 5615;
        var vppId = Guid.NewGuid();
        using (var context = database.CreateContext())
        {
            await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
            await SeedOpenPeriodAsync(context, 2026, 4);
        }

        var now = new DateTime(2026, 4, 10, 9, 0, 0);
        var baseOrder = await CreateService(database, now).CreateOrderAsync(
            CreateOrderRequest(2026, 4, isAdditionalOrder: false, vppId),
            userId,
            "IT",
            "77500");
        var barrier = new AsyncBarrier(4);
        var requests = Enumerable.Range(1, 4).Select(index =>
        {
            var request = CreateOrderRequest(2026, 4, isAdditionalOrder: true, vppId);
            request.BaseRequestId = baseOrder.Id;
            request.IdempotencyKey = $"supplement-race-{index}";
            return request;
        }).ToArray();

        var results = await Task.WhenAll(requests.Select(request =>
            CaptureAsync(() => CreateService(database, now, barrier)
                .CreateOrderAsync(request, userId, "IT", "77500"))));

        Assert.Equal(1, results.Count(x => x.Success));
        Assert.Equal(3, results.Count(x => !x.Success));
        using var verifyContext = database.CreateContext();
        var pending = await verifyContext.Set<VPP01_RequestHeader>()
            .Where(x => x.CreateUserId == userId
                && x.IsAdditionalOrder
                && x.IsCurrentRevision
                && !x.IsDeleted
                && x.Status == (int)VPPStatus.Pending)
            .ToListAsync();
        var winner = Assert.Single(pending);
        Assert.Equal(baseOrder.RequestSeriesId, winner.BaseRequestSeriesId);
        Assert.Equal(1, winner.SupplementAttemptNumber);
    }

    [Fact]
    public async Task UpdateOrder_TwoConcurrentReplacements_LeavesOneCurrentRevision()
    {
        using var database = new SqliteTestDatabase();
        const int userId = 5615;
        var vppId = Guid.NewGuid();
        using (var context = database.CreateContext())
        {
            await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
            await SeedOpenPeriodAsync(context, 2026, 4);
        }

        var now = new DateTime(2026, 4, 10, 9, 0, 0);
        var original = await CreateService(database, now).CreateOrderAsync(
            CreateOrderRequest(2026, 4, isAdditionalOrder: false, vppId),
            userId,
            "IT",
            "77500");
        var rowVersion = Guid.NewGuid().ToByteArray();
        using (var context = database.CreateContext())
        {
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE VPP01_RequestHeader
                SET RowVersion = {rowVersion}
                WHERE Id = {original.Id};
                """);
        }

        var barrier = new AsyncBarrier(2);
        var first = CreateUpdateRequest(original.Id, userId, vppId, 2, rowVersion, "replace-race-1");
        var second = CreateUpdateRequest(original.Id, userId, vppId, 3, rowVersion, "replace-race-2");
        var results = await Task.WhenAll(
            CaptureAsync(() => CreateService(database, now, barrier).UpdateOrderAsync(first)),
            CaptureAsync(() => CreateService(database, now, barrier).UpdateOrderAsync(second)));

        var outcomeDetails = string.Join(
            " | ",
            results.Select(result => result.Success
                ? "success"
                : $"{result.Exception?.GetType().Name}: {result.Exception?.Message}"));
        Assert.True(results.Count(x => x.Success) == 1, outcomeDetails);
        Assert.True(results.Count(x => x.Exception is ConflictException) == 1, outcomeDetails);
        using var verifyContext = database.CreateContext();
        var series = await verifyContext.Set<VPP01_RequestHeader>()
            .Where(x => x.RequestSeriesId == original.RequestSeriesId)
            .OrderBy(x => x.RevisionNumber)
            .ToListAsync();
        Assert.Equal(2, series.Count);
        Assert.Single(series, x => x.IsCurrentRevision);
        Assert.Equal(2, series.Max(x => x.RevisionNumber));
    }

    private static async Task<(bool Success, Exception? Exception)> CaptureAsync(Func<Task> action)
    {
        try
        {
            await action();
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex);
        }
    }

    private static string DescribeException(Exception? exception)
    {
        if (exception is null)
        {
            return "unknown failure";
        }

        var current = exception;
        var parts = new List<string>();
        while (current is not null)
        {
            parts.Add(current is SqliteException sqliteException
                ? $"{current.GetType().Name}[{sqliteException.SqliteErrorCode}/{sqliteException.SqliteExtendedErrorCode}]: {current.Message}"
                : $"{current.GetType().Name}: {current.Message}");
            current = current.InnerException;
        }

        return string.Join(" -> ", parts);
    }

    private static VPPRequestService CreateService(SqliteTestDatabase database, DateTime now, AsyncBarrier? barrier = null)
    {
        var unitOfWork = new TestUnitOfWork(database.CreateContext(), barrier);
        var factory = new Mock<IUnitOfWorkFactory>();
        factory.Setup(x => x.Create()).Returns(unitOfWork);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["VPPDeadlineDay"] = "5"
            })
            .Build();

        return new VPPRequestService(
            factory.Object,
            new HttpContextAccessor(),
            unitOfWork,
            new FakeDateTimeProvider(now),
            config,
                ServiceTestHelpers.CreateEnvironmentResolver(),
            new UserNameResolver(),
            NullLogger<BaseServices>.Instance,
            Options.Create(new JiraSettings()));
    }

    private static VPP01_CreateReqDTO CreateOrderRequest(int year, int month, bool isAdditionalOrder, Guid vppId)
        => new()
        {
            Y = year,
            M = month,
            Description = "Test order",
            IsAdditionalOrder = isAdditionalOrder,
            SupplementReason = isAdditionalOrder ? "Needed for a new employee" : null,
            Items = new List<VPP02_ItemReqDTO>
            {
                new() { VPPId = vppId, Qty = 1, Description = "Item" }
            }
        };

    private static async Task SeedOpenPeriodAsync(VPPContext context, int year, int month)
    {
        var calculator = new PeriodCalculator();
        var period = new Period(year, month);
        var timestamp = calculator.StartAtUtc(period);
        context.Set<VPP00_Period>().Add(new VPP00_Period
        {
            Id = Guid.NewGuid(),
            MemberCompanyCode = "77500",
            TimeZoneId = "Asia/Ho_Chi_Minh",
            Y = year,
            M = month,
            StartAtUtc = timestamp,
            SubmissionDeadlineUtc = calculator.SubmissionDeadlineUtc(period),
            SupplementApprovalDeadlineUtc = calculator.SupplementApprovalDeadlineUtc(
                period, TimeSpan.FromDays(2)),
            State = VppPeriodState.Open,
            CreateUserId = 5615,
            CreateDate = timestamp,
            UpdateUserId = 5615,
            UpdateDate = timestamp,
            IsDeleted = false
        });
        await context.SaveChangesAsync();
    }

    private static VPP01_UpdateReqDTO CreateUpdateRequest(
        Guid requestId,
        int userId,
        Guid vppId,
        int quantity,
        byte[] rowVersion,
        string idempotencyKey)
        => new()
        {
            Id = requestId,
            UpdateUserId = userId,
            Description = "Concurrent replacement",
            RowVersion = rowVersion,
            IdempotencyKey = idempotencyKey,
            Items =
            [
                new VPP02_ItemReqDTO
                {
                    VPPId = vppId,
                    Qty = quantity,
                    Description = "Item"
                }
            ]
        };

    private sealed class SqliteTestDatabase : IDisposable
    {
        private readonly SqliteConnection _keeperConnection;
        private readonly string _connectionString;
        private readonly DbContextOptions<VPPContext> _options;

        public SqliteTestDatabase()
        {
            _connectionString = $"Data Source={Guid.NewGuid()};Mode=Memory;Cache=Shared;Foreign Keys=False";
            _keeperConnection = new SqliteConnection(_connectionString);
            _keeperConnection.Open();
            _options = new DbContextOptionsBuilder<VPPContext>()
                .UseSqlite(_connectionString)
                .Options;

            using var context = CreateContext();
            context.Database.EnsureCreated();
            context.Database.ExecuteSqlRaw("CREATE TABLE v_Users (UserID INTEGER NOT NULL, FullName TEXT NULL);");
        }

        public VPPContext CreateContext()
        {
            return new VPPContext(_options);
        }

        public void Dispose()
        {
            _keeperConnection.Dispose();
        }
    }

    private sealed class TestUnitOfWork : IUnitOfWork
    {
        private readonly VPPContext _context;
        private readonly AsyncBarrier? _barrier;

        public TestUnitOfWork(VPPContext context, AsyncBarrier? barrier)
        {
            _context = context;
            _barrier = barrier;
        }

        public VPPContext VPPContext => _context;

        public void BeginTransaction()
        {
        }

        public Task BeginTransactionAsync()
        {
            return _barrier?.SignalAndWaitAsync() ?? Task.CompletedTask;
        }

        public void Commit()
        {
            _context.SaveChanges();
        }

        public async Task CommitAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsSqliteUniqueViolation(ex))
            {
                throw CreateSqlServerUniqueViolation(ex);
            }
        }

        public void Rollback()
        {
            _context.ChangeTracker.Clear();
        }

        public Task RollbackAsync()
        {
            Rollback();
            return Task.CompletedTask;
        }

        public int SaveChanges()
        {
            return _context.SaveChanges();
        }

        public Task<int> SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private static bool IsSqliteUniqueViolation(DbUpdateException exception)
            => exception.InnerException is SqliteException sqliteException
               && sqliteException.SqliteErrorCode == 19
               && sqliteException.SqliteExtendedErrorCode is 1555 or 2067;

        private static DbUpdateException CreateSqlServerUniqueViolation(DbUpdateException source)
        {
            var sqlException = SqlServerExceptionFactory.Create(2601, "Cannot insert duplicate key row.");
            return new DbUpdateException(source.Message, sqlException, source.Entries);
        }
    }

    private sealed class AsyncBarrier
    {
        private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _remaining;

        public AsyncBarrier(int participantCount)
        {
            _remaining = participantCount;
        }

        public Task SignalAndWaitAsync()
        {
            if (Interlocked.Decrement(ref _remaining) == 0)
            {
                _completion.SetResult();
            }

            return _completion.Task;
        }
    }

    private static class SqlServerExceptionFactory
    {
        public static SqlException Create(int number, string message)
        {
            var error = CreateSqlError(number, message);
            var collection = CreateSqlErrorCollection();
            typeof(SqlErrorCollection)
                .GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(collection, new object[] { error });

            return (SqlException)typeof(SqlException)
                .GetMethod("CreateException", BindingFlags.Static | BindingFlags.NonPublic, new[] { typeof(SqlErrorCollection), typeof(string) })!
                .Invoke(null, new object[] { collection, "15.0.0" })!;
        }

        private static SqlError CreateSqlError(int number, string message)
        {
            var constructor = typeof(SqlError)
                .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .OrderByDescending(x => x.GetParameters().Length)
                .First();
            var arguments = constructor.GetParameters()
                .Select(parameter => GetDefaultValue(parameter.ParameterType, parameter.Name, number, message))
                .ToArray();

            return (SqlError)constructor.Invoke(arguments);
        }

        private static SqlErrorCollection CreateSqlErrorCollection()
            => (SqlErrorCollection)typeof(SqlErrorCollection)
                .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, Type.EmptyTypes)!
                .Invoke(null);

        private static object? GetDefaultValue(Type type, string? name, int number, string message)
        {
            if (type == typeof(int) && name == "infoNumber") return number;
            if (type == typeof(string) && name == "errorMessage") return message;
            if (type == typeof(byte)) return (byte)0;
            if (type == typeof(int)) return 0;
            if (type == typeof(uint)) return 0U;
            if (type == typeof(long)) return 0L;
            if (type == typeof(bool)) return false;
            if (type == typeof(string)) return string.Empty;
            if (type == typeof(Exception)) return null;
            if (typeof(IDictionary).IsAssignableFrom(type)) return null;
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
}
