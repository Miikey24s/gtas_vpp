using System;
using System.Collections.Generic;
using System.Text;

namespace gtas_vpp_be.Service.Helpers
{
    public static class Config
    {
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
