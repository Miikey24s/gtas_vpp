namespace gtas_vpp_be.Service.Helpers
{
    public class EnvironmentResolver : IEnvironmentResolver
    {
        private readonly DatabaseBinding _databaseBinding;

        public EnvironmentResolver(DatabaseBinding databaseBinding)
        {
            _databaseBinding = databaseBinding ?? throw new ArgumentNullException(nameof(databaseBinding));
        }

        public string Resolve() => _databaseBinding.EnvironmentName;
    }
}
