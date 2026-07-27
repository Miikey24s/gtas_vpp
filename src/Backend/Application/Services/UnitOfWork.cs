using gtas_vpp_be.Model;
using gtas_vpp_be.Service.Helpers.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace gtas_vpp_be.Service.Services
{
    public interface IUnitOfWork : IDisposable
    {
        VPPContext VPPContext { get; }
        void BeginTransaction();
        Task BeginTransactionAsync();
        void Commit();
        Task CommitAsync();
        void Rollback();
        Task RollbackAsync();
        int SaveChanges();
        Task<int> SaveChangesAsync();
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IDynamicDbContextFactory _factory;
        private bool _disposed;

        private VPPContext? _VPPContext;
        private IDbContextTransaction? _transaction;
        public UnitOfWork(IDynamicDbContextFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }
        public VPPContext VPPContext
        {
            get
            {
                if (_VPPContext == null)
                {
                    _VPPContext = _factory.CreateVPPContext();
                }
                return _VPPContext;
            }
        }
        public void BeginTransaction()
        {
            if (_transaction != null)
                throw new InvalidOperationException("Transaction already started.");

            _transaction = VPPContext.Database.BeginTransaction();
        }
        public async Task BeginTransactionAsync()
        {
            if (_transaction != null)
                throw new InvalidOperationException("Transaction already started.");

            _transaction = await VPPContext.Database.BeginTransactionAsync();
        }

        public void Commit()
        {
            if (_transaction == null)
                throw new InvalidOperationException("No transaction to commit.");

            SaveChanges();
            _transaction.Commit();
            _transaction.Dispose();
            _transaction = null;
        }
        public async Task CommitAsync()
        {
            if (_transaction == null)
                throw new InvalidOperationException("No transaction to commit.");

            try
            {
                await SaveChangesAsync();
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
            catch
            {

                throw;
            }
        }

        public void Rollback()
        {
            if (_transaction == null) return;

            _transaction.Rollback();
            _transaction.Dispose();
            _transaction = null;

            ClearTrackedEntities();
        }
        public async Task RollbackAsync()
        {
            if (_transaction == null) return;

            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;

            ClearTrackedEntities();
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
                    _transaction?.Dispose();
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

        private void ClearTrackedEntities()
        {
            if (_VPPContext == null) return;

            foreach (var entry in _VPPContext.ChangeTracker.Entries())
            {
                entry.State = EntityState.Detached;
            }
        }
    }
}
