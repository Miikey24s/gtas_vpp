using System.Collections.Concurrent;
using System.Reflection;
using gtas_vpp_shared.DTOs.Res;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Service.Services
{
    public class UserNameResolver : IUserNameResolver
    {
        private static readonly ConcurrentDictionary<(Type Type, string Name), PropertyInfo?> PropertyCache = new();
        private const string SystemUserName = "Create by System";

        public async Task<List<T>> WithUserNamesAsync<T>(List<T> entities, DbContext context) where T : class
        {
            if (entities.Count == 0)
            {
                return entities;
            }

            var createUserIdProperty = GetProperty<T>("CreateUserId");
            var updateUserIdProperty = GetProperty<T>("UpdateUserId", "UpdaterUserId");
            var createUserNameProperty = GetProperty<T>("CreateUserName");
            var updateUserNameProperty = GetProperty<T>("UpdateUserName", "UpdaterUserName");

            if (createUserNameProperty == null && updateUserNameProperty == null)
            {
                return entities;
            }

            var userNames = await GetUserNamesByIdsAsync(
                context,
                GetDistinctUserIds(entities, createUserIdProperty, updateUserIdProperty));

            foreach (var entity in entities)
            {
                SetUserName(entity, createUserIdProperty, createUserNameProperty, userNames, useEmptyFallback: true);
                SetUserName(entity, updateUserIdProperty, updateUserNameProperty, userNames, useEmptyFallback: true);
            }

            return entities;
        }

        public async Task IncludeUserInfoAsync<T>(T entity, DbContext context) where T : class
        {
            var createUserIdProperty = GetProperty<T>("CreateUserId");
            var updateUserIdProperty = GetProperty<T>("UpdateUserId", "UpdaterUserId");
            var createUserNameProperty = GetProperty<T>("CreateUserName");
            var updateUserNameProperty = GetProperty<T>("UpdateUserName", "UpdaterUserName");

            if (createUserNameProperty == null && updateUserNameProperty == null)
            {
                return;
            }

            var userNames = await GetUserNamesByIdsAsync(
                context,
                GetDistinctUserIds(new[] { entity }, createUserIdProperty, updateUserIdProperty));

            SetUserName(entity, createUserIdProperty, createUserNameProperty, userNames, useEmptyFallback: false);
            SetUserName(entity, updateUserIdProperty, updateUserNameProperty, userNames, useEmptyFallback: false);
        }

        private static async Task<Dictionary<int, string?>> GetUserNamesByIdsAsync(DbContext context, int[] userIds)
        {
            if (userIds.Length == 0)
            {
                return new Dictionary<int, string?>();
            }

            return await context.Set<v_Users>()
                .AsNoTracking()
                .Where(x => userIds.Contains(x.UserID))
                .Select(x => new { x.UserID, x.FullName })
                .ToDictionaryAsync(x => x.UserID, x => x.FullName);
        }

        private static int[] GetDistinctUserIds<T>(
            IEnumerable<T> entities,
            PropertyInfo? createUserIdProperty,
            PropertyInfo? updateUserIdProperty) where T : class
            => entities
                .SelectMany(entity => new[]
                {
                    GetIntPropertyValue(createUserIdProperty, entity),
                    GetIntPropertyValue(updateUserIdProperty, entity)
                })
                .Where(userId => userId.HasValue && userId.Value > 0)
                .Select(userId => userId!.Value)
                .Distinct()
                .ToArray();

        private static void SetUserName<T>(
            T entity,
            PropertyInfo? userIdProperty,
            PropertyInfo? userNameProperty,
            IReadOnlyDictionary<int, string?> userNames,
            bool useEmptyFallback) where T : class
        {
            if (userIdProperty == null || userNameProperty is not { CanWrite: true })
            {
                return;
            }

            var userId = GetIntPropertyValue(userIdProperty, entity);
            if (!userId.HasValue)
            {
                return;
            }

            userNameProperty.SetValue(entity, ResolveUserName(userId.Value, userNames, useEmptyFallback));
        }

        private static string? ResolveUserName(int userId, IReadOnlyDictionary<int, string?> userNames, bool useEmptyFallback)
        {
            if (userId == -1)
            {
                return SystemUserName;
            }

            if (userNames.TryGetValue(userId, out var userName))
            {
                return userName;
            }

            return useEmptyFallback ? string.Empty : null;
        }

        private static int? GetIntPropertyValue<T>(PropertyInfo? property, T entity) where T : class
        {
            if (property == null)
            {
                return null;
            }

            var value = property.GetValue(entity);
            return value switch
            {
                int intValue => intValue,
                _ => null
            };
        }

        private static PropertyInfo? GetProperty<T>(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                var property = PropertyCache.GetOrAdd((typeof(T), propertyName), key => key.Type.GetProperty(key.Name));
                if (property != null)
                {
                    return property;
                }
            }

            return null;
        }

        private static PropertyInfo? GetProperty<T>(string propertyName)
            => PropertyCache.GetOrAdd((typeof(T), propertyName), key => key.Type.GetProperty(key.Name));
    }
}
