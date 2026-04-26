using System.Linq.Expressions;

namespace gtas_vpp_be.Service.Services
{
    public interface IGenericRepository<T> where T : class
    {
        Task<T?> AddAsync(T entity);
        Task<T> UpdateAsync(T entity, Expression<Func<T, object>>[]? properties = null);
        Task<List<T>> UpdateRangeAsync(List<T> entities, Expression<Func<T, bool>> expression);
        Task<bool> DeleteAsync(object id);
        Task<List<T>> ReadAsync(Expression<Func<T, bool>>? filter = null, Func<IQueryable<T>, IQueryable<T>>? include = null);
        Task<T?> GetByIdAsync(object id);
    }
}
