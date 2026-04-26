using System.Security.Claims;
using gtas_vpp_be.Service.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace gtas_vpp_be.Service.Services
{
    public interface IBaseServices
    {
    }

    public class BaseServices : IBaseServices
    {
        private readonly IUnitOfWorkFactory _unitOfWorkFactory;
        protected readonly IUnitOfWork _unitOfWork;
        private readonly IEnvironmentResolver _environmentResolver;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<BaseServices> _logger;
        private readonly JiraSettings _jiraSettings;

        public string JiraIssue { get; private set; } = string.Empty;

        public BaseServices(IUnitOfWorkFactory unitOfWorkFactory, IHttpContextAccessor httpContextAccessor, IEnvironmentResolver environmentResolver, IUserNameResolver userNameResolver, ILogger<BaseServices> logger, IOptions<JiraSettings> jiraSettings)
        {
            _unitOfWorkFactory = unitOfWorkFactory;
            _httpContextAccessor = httpContextAccessor;
            _environmentResolver = environmentResolver;
            _logger = logger;
            _jiraSettings = jiraSettings.Value;
            var environment = _environmentResolver.Resolve(Claims);
            ApplyJiraSettings(environment);
            _unitOfWork = _unitOfWorkFactory.Create(environment);
        }

        protected ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public IEnumerable<Claim> Claims => User?.Claims ?? Enumerable.Empty<Claim>();

        public virtual void WriteLog(Exception ex, string context, Dictionary<string, object>? properties = null)
        {
            _logger.LogError(ex, "Error in {Context} {@Properties}", context, properties);
        }

        private void ApplyJiraSettings(string environment)
        {
            JiraIssue = environment == nameof(Config.EnvType.LiveEnv)
                ? _jiraSettings.LiveJiraIssue
                : _jiraSettings.TestJiraIssue;
        }
    }
}
