using System;
using System.Collections.Generic;
using System.Text;

namespace gtas_vpp_be.Service.Helpers
{
    public static class Config
    {
        public static class EnvConfig
        {
            public enum EnvType
            {
                LiveEnv,
                TestEnv
            }
            public enum ContextType
            {
                VPPMigrationDbContext
            }
        }
    }
}
