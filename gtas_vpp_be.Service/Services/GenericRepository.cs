using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Service.Services
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly IUnitOfWork _unitOfWork;

        public GenericRepository(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<T?> AddAsync(T entity)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                await _unitOfWork.VPPContext.Set<T>().AddAsync(entity);
                await _unitOfWork.CommitAsync();
                return entity;
            }
            catch
            {
                _unitOfWork.Rollback();
                throw;
            }
        }

        public async Task<T> UpdateAsync(T entity, Expression<Func<T, object>>[]? properties = null)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                if (properties is not null)
                {
                    _unitOfWork.VPPContext.Set<T>().Attach(entity);
                    var entry = _unitOfWork.VPPContext.Entry(entity);
                    foreach (var property in properties)
                    {
                        entry.Property(property).IsModified = true;
                    }
                }
                else
                {
                    _unitOfWork.VPPContext.Set<T>().Update(entity);
                }

                await _unitOfWork.CommitAsync();
                return entity;
            }
            catch
            {
                _unitOfWork.Rollback();
                throw;
            }
        }

        public async Task<List<T>> UpdateRangeAsync(List<T> entities, Expression<Func<T, bool>> expression)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var entitiesToUpdate = await _unitOfWork.VPPContext.Set<T>()
                    .Where(expression)
                    .ToListAsync();

                if (!entitiesToUpdate.Any())
                {
                    throw new InvalidOperationException("No entity found to update");
                }

                var templateEntity = entities.First();
                var entryValues = _unitOfWork.VPPContext.Entry(templateEntity).CurrentValues;

                foreach (var dbEntity in entitiesToUpdate)
                {
                    var dbEntry = _unitOfWork.VPPContext.Entry(dbEntity);

                    foreach (var property in entryValues.Properties)
                    {
                        if (property.IsKey())
                        {
                            continue;
                        }

                        dbEntry.Property(property.Name).CurrentValue = entryValues[property];
                    }
                }

                await _unitOfWork.CommitAsync();
                return entitiesToUpdate;
            }
            catch
            {
                _unitOfWork.Rollback();
                throw;
            }
        }

        public async Task<bool> DeleteAsync(object id)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var entity = await _unitOfWork.VPPContext.Set<T>().FindAsync(id);
                if (entity == null)
                {
                    _unitOfWork.Rollback();
                    return false;
                }

                _unitOfWork.VPPContext.Set<T>().Remove(entity);
                await _unitOfWork.CommitAsync();
                return true;
            }
            catch
            {
                _unitOfWork.Rollback();
                throw;
            }
        }

        public async Task<List<T>> ReadAsync(Expression<Func<T, bool>>? filter = null, Func<IQueryable<T>, IQueryable<T>>? include = null)
        {
            IQueryable<T> query = _unitOfWork.VPPContext.Set<T>().AsNoTracking();

            if (include != null)
            {
                query = include(query);
            }

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.ToListAsync();
        }

        public async Task<T?> GetByIdAsync(object id)
        {
            return await _unitOfWork.VPPContext.Set<T>().FindAsync(id);
        }
    }
}
