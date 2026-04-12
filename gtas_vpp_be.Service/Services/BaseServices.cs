using gtas_vpp_be.Service.Helpers;
using gtas_vpp_shared.DTOs;
using gtas_vpp_shared.DTOs.Res;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using static gtas_vpp_be.Service.Helpers.Config.EnvConfig;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace gtas_vpp_be.Service.Services
{
    public interface IBaseServices
    {
        Task<sp_ResDTO> SP(string typeofdbContext, string spName, string spType, object param, int? timeout = 300, string? env = null);
        Task<sp_ResDTO> Query(string typeofdbContext, string query, int? timeout = 300, string? env = null);
    }
    public class BaseServices : IBaseServices
    {
        [Inject] public IUnitOfWorkFactory _unitOfWork { get; set; } = default!;
        public DeployEnv DeployEnv { get; set; } = new DeployEnv();
        public JiraIssueLive _JiraIssueLive { get; set; } = new JiraIssueLive();
        public JiraIssueTest _JiraIssueTest { get; set; } = new JiraIssueTest();
        private readonly IHttpContextAccessor _httpContextAccessor;
        public BaseServices(IUnitOfWorkFactory unitOfWork, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
        }
        protected ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User;
        public IEnumerable<Claim> Claims
        {
            get
            {
                return User?.Claims;
            }
        }

        /// <summary>
        /// Gọi SP
        /// </summary>
        /// <param name="typeofdbContext"></param>
        /// <param name="spName"></param>
        /// <param name="spType"></param>
        /// <param name="param"></param>
        /// <param name="env"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>

        public async Task<bool> DeleteAsync<T>(object Id, string typeofdbContext, string? env = null) where T : class
        {
            using var uow = _unitOfWork.Create(GetEnvironment());
            try
            {
                await uow.BeginTransactionAsync();
                // Xác định context
                DbContext context;
                //DbContext context = typeofdbContext switch
                //{
                //    "AuthenticationContext" => uow.AuthenticationContext,
                //    "TransportationContext" => uow.TransportationContext,
                //    _ => throw new ArgumentException("Invalid DbContext type")
                //};
                switch (typeofdbContext)
                {
                    //case "AuthenticationContext":
                    //    context = uow.AuthenticationContext;
                    //    break;
                    case nameof(Config.ContextType.VPPContext):
                        context = uow.VPPContext;
                        break;
                    default:
                        throw new ArgumentException("Invalid DbContext type");
                }

                // Tìm entity theo Id
                var entity = await context.Set<T>().FindAsync(Id);
                if (entity == null)
                {
                    return false; // không tìm thấy => không xóa
                }

                // Xóa
                context.Set<T>().Remove(entity);

                // Lưu thay đổi
                //await uow.SaveChangesAsync();
                await uow.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                uow.Rollback();
                WriteLog(ex, "Delete " + typeof(T).Name + " in " + typeofdbContext);
                throw new Exception($"Error deleting entity: {ex.Message}", ex);
            }
            finally
            {
                //await uow.CommitAsync();
                uow.Dispose();
            }
        }
        public async Task<List<T>> UpdateRangeTAsync<T>(List<T> entities, string typeofdbContext, Expression<Func<T, bool>> expression, string? env = null) where T : class
        {
            using var uow = _unitOfWork.Create(GetEnvironment());
            try
            {
                await uow.BeginTransactionAsync();

                DbSet<T> dbSet;
                DbContext dbContext;
                switch (typeofdbContext)
                {
                    //case "AuthenticationContext":
                    //    dbSet = uow.AuthenticationContext.Set<T>();
                    //    dbContext = uow.AuthenticationContext;
                    //break;
                    case "BudgetContext":
                        dbSet = uow.VPPContext.Set<T>();
                        dbContext = uow.VPPContext;
                        break;
                    default:
                        throw new ArgumentException("Invalid DbContext type");
                }

                // Lấy danh sách theo điều kiện
                var entitiesToUpdate = await dbSet.Where(expression).ToListAsync();

                if (!entitiesToUpdate.Any())
                    throw new Exception("No entity found to update");

                // Dùng phần tử đầu tiên trong entities làm "mẫu"
                var templateEntity = entities.First();
                var entryValues = dbContext.Entry(templateEntity).CurrentValues;

                foreach (var dbEntity in entitiesToUpdate)
                {
                    var dbEntry = dbContext.Entry(dbEntity);

                    foreach (var prop in entryValues.Properties)
                    {
                        if (prop.IsKey()) continue; // Bỏ qua khóa chính

                        var newValue = entryValues[prop];
                        dbEntry.Property(prop.Name).CurrentValue = newValue;
                    }
                }

                await uow.SaveChangesAsync();

                return entitiesToUpdate;
            }
            catch (Exception ex)
            {
                await uow.RollbackAsync();
                WriteLog(ex, "Update " + typeof(T).Name + " in " + typeofdbContext);
                throw new Exception($"Error in UpdateRangeTAsync: {ex.Message}", ex);
            }
            finally
            {
                await uow.CommitAsync();
                uow.Dispose();
            }
        }
        public async Task<T> UpdateAsync<T>(T entity, string typeofdbContext, Expression<Func<T, object>>[]? properties = null, string? env = null) where T : class
        {
            using var uow = _unitOfWork.Create(GetEnvironment());
            try
            {
                await uow.BeginTransactionAsync();
                var context = typeofdbContext switch
                {
                    nameof(Config.ContextType.VPPContext) => uow.VPPContext,
                    _ => throw new ArgumentException("Invalid DbContext type")
                };

                if (properties is not null)
                {
                    context.Set<T>().Attach(entity);
                    var entry = context.Entry(entity);
                    foreach (var prop in properties)
                    {
                        entry.Property(prop).IsModified = true;
                    }
                }
                else
                {
                    context.Set<T>().Update(entity);
                }

                await uow.SaveChangesAsync();
                return entity;
            }
            catch (Exception ex)
            {
                await uow.RollbackAsync();
                WriteLog(ex, "Update " + typeof(T).Name + " in " + typeofdbContext);
                throw new Exception($"Error updating entity: {ex.Message}", ex);
            }
            finally
            {
                await uow.CommitAsync();
                uow.Dispose();
            }
        }
        public virtual void WriteLog(Exception ex, string spname, Dictionary<string, object>? properties = null)
        {
            //string Mess = $"Error when call ef {spname} - User call: {glb?.UserInfo?.UserID} - {glb?.UserInfo?.FullName}";
            //string key = string.Empty;
            //string value = string.Empty;
            //if (properties != null)
            //{
            //    foreach (var item in properties)
            //    {
            //        key += item.Key + "|::|";
            //        value += item.Value + "|::|";
            //    }
            //    if (!string.IsNullOrEmpty(key))
            //    {
            //        key = key.Remove(key.Length - 4, 4);
            //    }
            //    if (!string.IsNullOrEmpty(value))
            //    {
            //        value = value.Remove(value.Length - 4);
            //    }
            //    Mess = Mess + " with param(s): " + key + " and value(s) " + value;
            //}

            //Log.Error(ex, Mess);
            //Console.WriteLine(Mess);
        }
        public async Task<T?> AddAsync<T>(T? entity, string typeofdbContext, string? env = null) where T : class
        {
            using var uow = _unitOfWork.Create(GetEnvironment());
            try
            {
                await uow.BeginTransactionAsync();
                switch (typeofdbContext)
                {
                    //case "AuthenticationContext":
                    //    await uow.AuthenticationContext.Set<T>().AddAsync(entity);
                    //    break;
                    case nameof(Config.ContextType.VPPContext):
                        await uow.VPPContext.Set<T>().AddAsync(entity);
                        break;
                    default:
                        throw new ArgumentException("Invalid DbContext type");
                }
                await uow.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                uow.Rollback();
                entity = null;
                WriteLog(ex, "Create " + typeof(T).Name + " in " + typeofdbContext);
                throw new Exception($"Error adding entity: {ex.Message}", ex);
            }
            finally
            {
                await uow.CommitAsync();
                uow.Dispose();
            }
            return entity;
        }
        private static async Task SetUserNameIfExists<T>(
                                                            IUnitOfWork uow,
                                                            T entity,
                                                            string userIdPropName,
                                                            string userNamePropName,
                                                            CancellationToken cancellationToken
                                                        ) where T : class
        {
            var userIdProp = typeof(T).GetProperty(userIdPropName);
            var userNameProp = typeof(T).GetProperty(userNamePropName);

            if (userIdProp == null || userNameProp == null)
                return;

            var userId = userIdProp.GetValue(entity) as int?;
            if (userId == null)
                return;

            var userName = await uow.VPPContext.Set<v_Users>()
                .Where(u => u.UserID == userId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(cancellationToken);

            userNameProp.SetValue(entity, userName);
        }
        private static async Task IncludeUserInfoIfNeeded<T>(
                                                                IUnitOfWork uow,
                                                                string contextType,
                                                                T entity,
                                                                CancellationToken cancellationToken
                                                            ) where T : class
        {
            if (contextType != nameof(Config.ContextType.VPPContext))
                return;

            await SetUserNameIfExists(
                uow,
                entity,
                "CreateUserId",
                "CreateUserName",
                cancellationToken
            );

            await SetUserNameIfExists(
                uow,
                entity,
                "UpdateUserId",
                "UpdateUserName",
                cancellationToken
            );
        }
        private static IQueryable<T> GetQueryable<T>(IUnitOfWork uow, string contextType) where T : class
        {
            return contextType switch
            {
                nameof(Config.ContextType.VPPContext)
                    => uow.VPPContext.Set<T>(),

                // nameof(Config.ContextType.AuthenticationContext)
                //     => uow.AuthenticationContext.Set<T>(),

                _ => throw new ArgumentException("Invalid DbContext type")
            };
        }
        public async Task<T?> GetByIdIncludeAsync<T>(
                                                string typeOfDbContext,
                                                object id,
                                                bool? includeUser = null,
                                                Func<IQueryable<T>, IQueryable<T>>? include = null,
                                                string? env = null,
                                                CancellationToken cancellationToken = default
                                            ) where T : class
        {
            using var uow = _unitOfWork.Create(GetEnvironment());

            try
            {
                IQueryable<T> queryable = GetQueryable<T>(uow, typeOfDbContext);

                // Apply include
                if (include != null)
                    queryable = include(queryable);

                var entity = await queryable.FirstOrDefaultAsync(
                    e => EF.Property<object>(e, "Id").Equals(id),
                    cancellationToken
                );

                if (entity == null || includeUser != true)
                    return entity;

                await IncludeUserInfoIfNeeded(uow, typeOfDbContext, entity, cancellationToken);

                return entity;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting entity: {ex.Message}", ex);
            }
        }
        public async Task<T?> GetByIdAsync<T>(string typeofdbContext, object id, bool? includeUser = null, string? env = null, CancellationToken cancellationToken = default) where T : class
        {
            using var uow = _unitOfWork.Create(GetEnvironment());
            T? query;
            try
            {
                switch (typeofdbContext)
                {
                    case nameof(Config.ContextType.VPPContext):
                        query = await uow.VPPContext.Set<T>().FindAsync(id);
                        break;
                    default:
                        throw new ArgumentException("Invalid DbContext type");
                }
                if (includeUser == true && query is not null)
                {
                    switch (typeofdbContext)
                    {
                        case nameof(Config.ContextType.VPPContext):
                            {
                                var createUserIdProp = typeof(T).GetProperty("CreateUserId");
                                var updateUserIdProp = typeof(T).GetProperty("UpdateUserId");
                                if (createUserIdProp != null)
                                {
                                    var createUserId = (int?)createUserIdProp.GetValue(query);
                                    var createUserName = await uow.VPPContext.Set<v_Users>()
                                        .Where(u => u.UserID == createUserId)
                                        .Select(u => u.FullName)
                                        .FirstOrDefaultAsync();
                                    var createUserNameProp = typeof(T).GetProperty("CreateUserName");
                                    createUserNameProp?.SetValue(query, createUserName);
                                }
                                if (updateUserIdProp != null)
                                {
                                    var updateUserId = (int?)updateUserIdProp.GetValue(query);
                                    var updateUserName = await uow.VPPContext.Set<v_Users>()
                                        .Where(u => u.UserID == updateUserId)
                                        .Select(u => u.FullName)
                                        .FirstOrDefaultAsync();
                                    var updateUserNameProp = typeof(T).GetProperty("UpdateUserName");
                                    updateUserNameProp?.SetValue(query, updateUserName);
                                }
                                break;
                            }
                        //case "BudgetContext":
                        //    // Similar logic for TransportationContext if needed
                        //    break;
                        default:
                            throw new ArgumentException("Invalid DbContext type");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error adding entity: {ex.Message}", ex);
            }
            return query;
        }
        public async Task<List<T>> WithUserNames<T>(List<T> list, DbContext context) where T : class
        {
            var userlist = await context.Set<v_Users>().ToListAsync();
            foreach (var e in list)
            {
                var createUserId = (int?)typeof(T)
                    .GetProperty("CreateUserId")
                    ?.GetValue(e);

                var updateUserId = (int?)typeof(T)
                    .GetProperty("UpdaterUserId")
                    ?.GetValue(e);
                typeof(T).GetProperty("CreateUserName")
                    ?.SetValue(e, userlist.FirstOrDefault(x => x.UserID == createUserId)?.FullName ?? (createUserId == -1 ? "Create by System" : ""));

                typeof(T).GetProperty("UpdaterUserName")
                    ?.SetValue(e, userlist.FirstOrDefault(x => x.UserID == updateUserId)?.FullName ?? (createUserId == -1 ? "Create by System" : ""));
            }

            return list;
        }

        public string GetEnvironment(string? env = null)
        {
            if (Claims?.FirstOrDefault(x => x.Type == "Server")?.Value is not null)
            {
                string env1 = Claims?.FirstOrDefault(x => x.Type == "Server")?.Value ?? string.Empty;
                switch (env1)
                {
                    case "Test":
                        env = "TestEnv";
                        DeployEnv.JiraIssue = _JiraIssueTest.JiraIssue;
                        break;
                    case "Live":
                        env = "LiveEnv";
                        DeployEnv.JiraIssue = _JiraIssueLive.JiraIssue;
                        break;
                    default:
                        env = "TestEnv";
                        DeployEnv.JiraIssue = _JiraIssueTest.JiraIssue;
                        break;
                }
            }
            else
            {
                switch (env)
                {
                    case "Test":
                        env = "TestEnv";
                        DeployEnv.JiraIssue = _JiraIssueTest.JiraIssue;
                        break;
                    case "Live":
                        env = "LiveEnv";
                        DeployEnv.JiraIssue = _JiraIssueLive.JiraIssue;
                        break;
                    default:
                        env = "TestEnv";
                        DeployEnv.JiraIssue = _JiraIssueTest.JiraIssue;
                        break;
                }
            }
            return env;
        }
        public async Task<List<T>> ReadAsync<T>(
                                            string typeofdbContext,
                                            bool? getFullName = null,
                                            Expression<Func<T, bool>>? expression = null,
                                            Func<IQueryable<T>, IQueryable<T>>? include = null,
                                            string? env = null,
                                            CancellationToken cancellationToken = default
                                        ) where T : class
        {
            using var uow = _unitOfWork.Create(env ?? GetEnvironment());
            IQueryable<T> query = typeofdbContext switch
            {
                nameof(Config.ContextType.VPPContext) => uow.VPPContext.Set<T>().AsNoTracking(),
                _ => throw new ArgumentException("Invalid DbContext type")
            };

            // lấy data bảng con nếu có
            if (include != null)
                query = include(query);

            // điều kiện where lọc nếu có
            if (expression != null)
                query = query.Where(expression);

            List<T> result = await query.ToListAsync();
            if (getFullName == true)
            {
                result = typeofdbContext switch
                {
                    //"AuthenticationContext" => await WithUserNames(result, uow.AuthenticationContext),
                    nameof(Config.ContextType.VPPContext) => await WithUserNames<T>(result, uow.VPPContext),
                    _ => throw new ArgumentException("Invalid DbContext type")
                };
            }
            return result;
        }
        public async Task<sp_ResDTO> SP(string typeofdbContext, string spName, string spType, object param, int? timeout = 300, string? env = null)
        {
            //sp_ResDTO sp_ResDTO = new sp_ResDTO();
            using var uow = _unitOfWork.Create(nameof(Config.EnvType.TestEnv));
            uow.VPPContext.Database.SetCommandTimeout(timeout);
            try
            {
                switch (typeofdbContext)
                {
                    case nameof(Config.ContextType.VPPContext):
                        var sp_ResDTO = await uow.VPPContext.Set<sp_ResDTO>()
                                    .FromSqlRaw("exec {0} @SpType={1}, @Param={2}", spName, spType, JsonConvert.SerializeObject(param))
                                    .ToListAsync();
                        return sp_ResDTO.FirstOrDefault() ?? new sp_ResDTO
                        {
                            IsSuccess = false,
                            ErrorMess = "No data returned from stored procedure."
                        };
                    default:
                        return new sp_ResDTO
                        {
                            IsSuccess = false,
                            ErrorMess = "Unsupported database context."
                        };
                }
            }
            catch (Exception ex)
            {
                return new sp_ResDTO
                {
                    IsSuccess = false,
                    ErrorMess = ex.Message
                };
                //sp_ResDTO.IsSuccess = false;
                //sp_ResDTO.ErrorMess = ex.Message;
                //_logger.LogError(ex, "Lỗi khi chạy EFBaseServiceRead " + spName + " - " + spType + " - " + JsonConvert.SerializeObject(param));
                throw new Exception($"Error in EFBaseServiceRead: {ex.Message}", ex);
            }
            finally
            {
                uow.Dispose();
            }
            //return sp_ResDTO;
        }
        public async Task<sp_ResDTO> Query(string typeofdbContext, string query, int? timeout = 300, string? env = null)
        {
            sp_ResDTO sp_ResDTO = new sp_ResDTO();
            using var uow = _unitOfWork.Create(nameof(Config.EnvType.TestEnv));
            uow.VPPContext.Database.SetCommandTimeout(timeout);
            try
            {
                switch (typeofdbContext)
                {
                    case nameof(Config.ContextType.VPPContext):
                        sp_ResDTO = (await uow.VPPContext.Set<sp_ResDTO>()
                                    .FromSqlRaw(query)
                                    .ToListAsync()).FirstOrDefault() ?? new sp_ResDTO();
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                sp_ResDTO.IsSuccess = false;
                sp_ResDTO.ErrorMess = ex.Message;
                //_logger.LogError(ex, "Lỗi khi chạy EFBaseServiceRead " + spName + " - " + spType + " - " + JsonConvert.SerializeObject(param));
                throw new Exception($"Error in EFBaseServiceRead: {ex.Message}", ex);
            }
            finally
            {
                uow.Dispose();
            }
            return sp_ResDTO;
        }
    }
}

