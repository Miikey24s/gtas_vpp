using Microsoft.AspNetCore.Http;
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

        public UnitOfWorkFactory(IConfiguration config, IDynamicDbContextFactory dbContextFactory, IHttpContextAccessor httpContextAccessor)
        {
            _config = config;
            _dbContextFactory = dbContextFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        public IUnitOfWork Create(string envKey)
        {
            return new UnitOfWork(_dbContextFactory, _httpContextAccessor);
        }
    }
}
