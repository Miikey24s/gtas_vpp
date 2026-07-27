using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Service.Services
{
    public interface IUserNameResolver
    {
        Task<List<T>> WithUserNamesAsync<T>(List<T> entities, DbContext context) where T : class;
        Task IncludeUserInfoAsync<T>(T entity, DbContext context) where T : class;
    }
}
