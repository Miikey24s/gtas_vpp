using System.Runtime.InteropServices;

namespace gtas_vpp_be.Service.Services
{
    public interface IUnitOfWorkFactory
    {
        IUnitOfWork Create();
    }

    [StructLayout(LayoutKind.Auto)]
    public class UnitOfWorkFactory : IUnitOfWorkFactory
    {
        private readonly IDynamicDbContextFactory _dbContextFactory;

        public UnitOfWorkFactory(IDynamicDbContextFactory dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public IUnitOfWork Create() => new UnitOfWork(_dbContextFactory);
    }
}
