using System.Collections;
using System.Reflection;
using gtas_vpp_be.Model.VPP;
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
        }

        var request1 = CreateOrderRequest(2026, 4, isAdditionalOrder: false, vppId);
        var request2 = CreateOrderRequest(2026, 4, isAdditionalOrder: false, vppId);
        var barrier = new AsyncBarrier(2);

        // P1: Clock at day 10 ⇒ current period = April 2026, matching the requests' Y/M.
        var results = await Task.WhenAll(
            CaptureAsync(() => CreateService(database, new DateTime(2026, 4, 10, 9, 0, 0), barrier).CreateOrderAsync(request1, userId, "IT", "77500")),
            CaptureAsync(() => CreateService(database, new DateTime(2026, 4, 10, 9, 0, 0), barrier).CreateOrderAsync(request2, userId, "IT", "77500")));

        Assert.Equal(1, results.Count(x => x.Success));
        Assert.Equal(1, results.Count(x => x.Exception is ConflictException));

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

        // P1: Regular submitted mid-April (current period = 2026-04).
        // Additional submitted mid-May (current = 2026-05, previous = 2026-04).
        await CreateService(database, new DateTime(2026, 4, 10, 9, 0, 0)).CreateOrderAsync(regular, userId, "IT", "77500");
        await CreateService(database, new DateTime(2026, 5, 10, 9, 0, 0)).CreateOrderAsync(additional, userId, "IT", "77500");

        using var verifyContext = database.CreateContext();
        Assert.Equal(2, await verifyContext.Set<VPP01_RequestHeader>()
            .CountAsync(x => x.CreateUserId == userId && x.Y == 2026 && x.M == 4 && !x.IsDeleted));
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

    private static VPPRequestService CreateService(SqliteTestDatabase database, DateTime now, AsyncBarrier? barrier = null)
    {
        var unitOfWork = new TestUnitOfWork(database.CreateContext(), barrier);
        var factory = new Mock<IUnitOfWorkFactory>();
        factory.Setup(x => x.Create(It.IsAny<string>())).Returns(unitOfWork);
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
            new EnvironmentResolver(),
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
            Items = new List<VPP02_ItemReqDTO>
            {
                new() { VPPId = vppId, Qty = 1, Description = "Item" }
            }
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

        public void Init(string envKey)
        {
        }

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
               && sqliteException.SqliteExtendedErrorCode == 2067;

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
