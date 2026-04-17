using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace gtas_vpp_be.Service.Helpers
{
    public static class Config
    {
        private static IConfiguration? _configuration;

        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public static class JwtSettings
        {
            public static string Key => GetConfigValue("JwtSettings:Key", "GTAS_VPP_BE_DEV_ONLY_KEY_CHANGE_IN_PRODUCTION_2026");
            public static string Issuer => GetConfigValue("JwtSettings:Issuer", "gtas_vpp_be");
            public static string Audience => GetConfigValue("JwtSettings:Audience", "gtas_vpp_clients");
            public static int ClockSkewMinutes => 2;
        }

        public static class DatabaseSettings
        {
            public static string MigrationsAssembly => "gtas_vpp_be.Migrations";
        }

        private static string GetConfigValue(string key, string defaultValue)
        {
            return _configuration?[key] ?? defaultValue;
        }

        public static class EnvConfig
        {
            public class JiraIssueTest
            {
                public List<string> JiraIssue { get; set; }
                public JiraIssueTest()
                {
                    JiraIssue = new List<string>()
                    {
                        "GC2_55"
                    };
                }
            }
            public class JiraIssueLive
            {
                public List<string> JiraIssue { get; set; }
                public JiraIssueLive()
                {
                    JiraIssue = new List<string>()
                    {
                        ""
                    };
                }
            }
            public enum JrTest
            {
                GC2_55
            }
            public class DeployEnv
            {
                public List<string> JiraIssue { get; set; }
                public string Title { get; set; }
                public DeployEnv()
                {
                    Title = default!;
                    JiraIssue = default!;
                }
            }
        }
        public enum EnvType
        {
            LiveEnv,
            TestEnv
        }
        public enum ContextType
        {
            VPPMigrationDbContext,
            VPPContext,
        }
        public enum EF_BASEMETHOD
        {
            EF_GetTAsync,
            EF_GetTAsync_Paging,
            EF_GetTByIdAsync,
            EF_GetTByIdIncludeAsync,
            EF_Create,
            EF_Update,
            EF_UpdateRange,
            EF_DeleteAsync
        }
    }
}
