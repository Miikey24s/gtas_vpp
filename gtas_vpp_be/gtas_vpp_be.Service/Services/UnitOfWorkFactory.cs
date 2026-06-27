using Microsoft.AspNetCore.Http;
using gtas_vpp_be.Service.Helpers;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace gtas_vpp_be.Service.Services
{
    public interface IUnitOfWorkFactory
    {
        IUnitOfWork Create(string envKey);
    }
    [StructLayout(LayoutKind.Auto)]
    public class UnitOfWorkFactory : IUnitOfWorkFactory
    {
        private readonly IConfiguration _config;
        private readonly IDynamicDbContextFactory _dbContextFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEnvironmentResolver _environmentResolver;

        public UnitOfWorkFactory(IConfiguration config, IDynamicDbContextFactory dbContextFactory, IHttpContextAccessor httpContextAccessor, IEnvironmentResolver environmentResolver)
        {
            _config = config;
            _dbContextFactory = dbContextFactory;
            _httpContextAccessor = httpContextAccessor;
            _environmentResolver = environmentResolver;
        }

        public IUnitOfWork Create(string envKey)
        {
            var unitOfWork = new UnitOfWork(_dbContextFactory, _httpContextAccessor, _environmentResolver);
            unitOfWork.Init(envKey);
            return unitOfWork;
        }
    }
}
