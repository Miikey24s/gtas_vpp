using System.Security.Claims;
using gtas_vpp_be.Service.Helpers;
using Microsoft.AspNetCore.Http;
using static gtas_vpp_be.Service.Helpers.Config.EnvConfig;

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

        public DeployEnv DeployEnv { get; set; } = new DeployEnv();
        public JiraIssueLive _JiraIssueLive { get; set; } = new JiraIssueLive();
        public JiraIssueTest _JiraIssueTest { get; set; } = new JiraIssueTest();

        public BaseServices(IUnitOfWorkFactory unitOfWorkFactory, IHttpContextAccessor httpContextAccessor, IEnvironmentResolver environmentResolver, IUserNameResolver userNameResolver)
        {
            _unitOfWorkFactory = unitOfWorkFactory;
            _httpContextAccessor = httpContextAccessor;
            _environmentResolver = environmentResolver;
            var environment = _environmentResolver.Resolve(Claims);
            ApplyDeployEnvironment(environment);
            _unitOfWork = _unitOfWorkFactory.Create(environment);
        }

        protected ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public IEnumerable<Claim> Claims => User?.Claims ?? Enumerable.Empty<Claim>();

        public virtual void WriteLog(Exception ex, string spname, Dictionary<string, object>? properties = null)
        {
        }

        private void ApplyDeployEnvironment(string environment)
        {
            DeployEnv.JiraIssue = environment == "LiveEnv"
                ? _JiraIssueLive.JiraIssue
                : _JiraIssueTest.JiraIssue;
        }
    }
}
