using System.Collections.Concurrent;
using System.Reflection;
using gtas_vpp_shared.DTOs.Res;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Service.Services
{
    public class UserNameResolver : IUserNameResolver
    {
        private static readonly ConcurrentDictionary<(Type Type, string Name), PropertyInfo?> PropertyCache = new();

        public async Task<List<T>> WithUserNamesAsync<T>(List<T> entities, DbContext context) where T : class
        {
            var users = await context.Set<v_Users>().ToListAsync();

            foreach (var entity in entities)
            {
                var createUserId = GetProperty<T>("CreateUserId")?.GetValue(entity) as int?;
                var updateUserId = GetProperty<T>("UpdaterUserId")?.GetValue(entity) as int?;

                GetProperty<T>("CreateUserName")
                    ?.SetValue(entity, users.FirstOrDefault(x => x.UserID == createUserId)?.FullName ?? (createUserId == -1 ? "Create by System" : ""));

                GetProperty<T>("UpdaterUserName")
                    ?.SetValue(entity, users.FirstOrDefault(x => x.UserID == updateUserId)?.FullName ?? (createUserId == -1 ? "Create by System" : ""));
            }

            return entities;
        }

        public async Task IncludeUserInfoAsync<T>(T entity, DbContext context) where T : class
        {
            await SetUserNameIfExistsAsync(entity, context, "CreateUserId", "CreateUserName");
            await SetUserNameIfExistsAsync(entity, context, "UpdateUserId", "UpdateUserName");
        }

        private static async Task SetUserNameIfExistsAsync<T>(
            T entity,
            DbContext context,
            string userIdPropertyName,
            string userNamePropertyName) where T : class
        {
            var userIdProperty = GetProperty<T>(userIdPropertyName);
            var userNameProperty = GetProperty<T>(userNamePropertyName);

            if (userIdProperty == null || userNameProperty == null)
            {
                return;
            }

            var userId = userIdProperty.GetValue(entity) as int?;
            if (userId == null)
            {
                return;
            }

            var userName = await context.Set<v_Users>()
                .Where(x => x.UserID == userId)
                .Select(x => x.FullName)
                .FirstOrDefaultAsync();

            userNameProperty.SetValue(entity, userName);
        }

        private static PropertyInfo? GetProperty<T>(string propertyName)
            => PropertyCache.GetOrAdd((typeof(T), propertyName), key => key.Type.GetProperty(key.Name));
    }
}
