using gtas_vpp_be.Model;
using gtas_vpp_be.Service.Helpers.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace gtas_vpp_be.Service.Services
{
    public interface IDynamicDbContextFactory
    {
        VPPContext CreateVPPContext(string envKey);
    }
    [StructLayout(LayoutKind.Auto)]
    public class DynamicDbContextFactory : IDynamicDbContextFactory
    {
        private readonly IConfiguration _configuration;

        public DynamicDbContextFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public string GetConnectionString(string envKey)
           => _configuration.GetConnectionString(envKey)
              ?? throw new InvalidOperationException($"Connection string '{envKey}' not found.");

        public VPPContext CreateVPPContext(string envKey)
        {
            var connStr = _configuration.GetConnectionString(envKey)
                ?? throw new InvalidOperationException($"Connection string '{envKey}' not found.");

            var options = new DbContextOptionsBuilder<VPPContext>()
                .UseSqlServer(connStr)
                .Options;

            var context = new VPPContext(options);
            context.Database.SetCommandTimeout(180);

            return context;
        }
    }
}
