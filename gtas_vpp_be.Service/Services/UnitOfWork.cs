using gtas_vpp_be.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text;
using System.Transactions;

namespace gtas_vpp_be.Service.Services
{
    public interface IUnitOfWork : IDisposable
    {
        VPPMigrationDbContext VPPContext { get; }
        void Init(string envKey);

        void BeginTransaction();
        Task BeginTransactionAsync();
        void Commit();
        Task CommitAsync();
        void Rollback();
        Task RollbackAsync();
        int SaveChanges();
        Task<int> SaveChangesAsync();
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        void Dispose();
    }
    [StructLayout(LayoutKind.Auto)]
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IDynamicDbContextFactory _factory;
        private readonly IConfiguration _config;
        private string _currentEnv;
        private bool _disposed;

        private VPPMigrationDbContext _VPPContext;
        private TransactionScope _transactionScope;
        private readonly IHttpContextAccessor _httpContextAccessor;
        protected ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User ?? default!;
        public IEnumerable<Claim> Claims
        {
            get
            {
                return User?.Claims ?? Enumerable.Empty<Claim>();
            }
        }
        public string GetEnvironment()
        {
            string env = string.Empty;
            if (Claims is not null)
            {
                string env1 = Claims?.FirstOrDefault(x => x.Type == "Server")?.Value ?? string.Empty;
                switch (env1)
                {
                    case "Test":
                        env = "TestEnv";
                        break;
                    case "Live":
                        env = "LiveEnv";
                        break;
                    default:
                        env = "TestEnv";
                        break;
                }
            }
            else
            {
                switch (env)
                {
                    case "Test":
                        env = "TestEnv";
                        break;
                    case "Live":
                        env = "LiveEnv";
                        break;
                    default:
                        env = "TestEnv";
                        break;
                }
            }
            return env;
        }

        public UnitOfWork(IDynamicDbContextFactory factory, IHttpContextAccessor httpContextAccessor)
        {
            _factory = factory;
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            Init(GetEnvironment());
        }
        public VPPMigrationDbContext VPPContext
        {
            get
            {
                if (_VPPContext == null)
                {
                    _VPPContext = _factory.CreateVPPContext(_currentEnv);
                }
                return _VPPContext;
            }
        }
        public void Init(string envKey)
        {
            if (string.IsNullOrWhiteSpace(envKey))
                throw new ArgumentException("envKey is required.", nameof(envKey));

            _currentEnv = envKey;

            // Clear old contexts nếu có
            _VPPContext?.Dispose();
            _VPPContext = null;
        }
        public void BeginTransaction()
        {
            _transactionScope = new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions
                {
                    IsolationLevel = IsolationLevel.ReadCommitted,
                    Timeout = TransactionManager.DefaultTimeout
                },
                TransactionScopeAsyncFlowOption.Enabled
            );
        }
        public async Task BeginTransactionAsync()
        {
            if (_transactionScope != null)
                throw new InvalidOperationException("Transaction already started.");

            //_transactionScope = new TransactionScope(
            //    (TransactionScopeOption)TransactionScopeAsyncFlowOption.Enabled,
            //    new TransactionOptions
            //    {
            //        IsolationLevel = IsolationLevel.ReadCommitted
            //    });

            _transactionScope = new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions
                {
                    IsolationLevel = IsolationLevel.ReadCommitted,
                    Timeout = TransactionManager.DefaultTimeout
                },
                TransactionScopeAsyncFlowOption.Enabled
            );
        }

        public void Commit()
        {
            _transactionScope?.Complete();
            _transactionScope?.Dispose();
            _transactionScope = null;
        }
        public async Task CommitAsync()
        {
            if (_transactionScope == null)
                throw new InvalidOperationException("No transaction to commit.");

            try
            {
                await SaveChangesAsync();

                _transactionScope.Complete();
                _transactionScope.Dispose();
                _transactionScope = null;
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public void Rollback()
        {
            _transactionScope?.Dispose();
            _transactionScope = null;
        }
        public async Task RollbackAsync()
        {
            if (_transactionScope == null) return;

            // Dispose mà không gọi Complete => rollback
            _transactionScope.Dispose();
            _transactionScope = null;

            // Clear tracked entities
            if (_VPPContext != null)
                foreach (var entry in _VPPContext.ChangeTracker.Entries())
                    entry.State = EntityState.Detached;
        }

        public int SaveChanges()
        {
            var total = 0;
            if (_VPPContext != null)
                total += _VPPContext.SaveChanges();
            return total;
        }
        public async Task<int> SaveChangesAsync()
        {
            var result = 0;

            if (_VPPContext != null)
                result += await _VPPContext.SaveChangesAsync();

            return result;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var total = 0;
            if (_VPPContext != null)
                total += await _VPPContext.SaveChangesAsync(cancellationToken);
            return total;
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _transactionScope?.Dispose();
                    _VPPContext?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
