using gtas_vpp_be.Model;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Helpers.Context;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;

namespace gtas_vpp_be.Service.Services
{
    public interface IDynamicDbContextFactory
    {
        VPPContext CreateVPPContext();
    }
    [StructLayout(LayoutKind.Auto)]
    public class DynamicDbContextFactory : IDynamicDbContextFactory
    {
        private readonly DatabaseBinding _databaseBinding;

        public DynamicDbContextFactory(DatabaseBinding databaseBinding)
        {
            _databaseBinding = databaseBinding;
        }

        public VPPContext CreateVPPContext()
        {
            var options = new DbContextOptionsBuilder<VPPContext>()
                .UseSqlServer(_databaseBinding.ConnectionString)
                .Options;

            var context = new VPPContext(options);
            context.Database.SetCommandTimeout(180);

            return context;
        }
    }
}
