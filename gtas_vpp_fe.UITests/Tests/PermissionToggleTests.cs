using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using gtas_vpp_fe.UITests.Pages;
using Microsoft.Playwright;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests.Permission
{
    public class PermissionToggleTests : TestBase, IMutatingUiTest
    {
        private const string AdminGroupName = "Admin";
        private const string ReportPageTab = "Report (REPORT)";
        private const string ReportViewComponentCode = "REPORT_VIEW";

        [Fact]
        public async Task Toggle_Report_Permission_Updates_Current_Session_Immediately()
        {
            await LoginAsDefaultUserAsync();

            var permissionPage = new PermissionManagementPage(Page);
            await permissionPage.GotoAsync(BaseUrl);

            await permissionPage.SetComponentVisibilityAsync(
                AdminGroupName,
                ReportPageTab,
                ReportViewComponentCode,
                true);
            await permissionPage.WaitForReportMenuVisibleAsync();

            try
            {
                await permissionPage.SetComponentVisibilityAsync(
                    AdminGroupName,
                    ReportPageTab,
                    ReportViewComponentCode,
                    false);
                await permissionPage.WaitForReportMenuHiddenAsync();

                await Page.GotoAsync($"{BaseUrl}report");
                await Page.WaitForURLAsync(
                    new Regex(@".*dashboard(\?tab=0)?"),
                    new PageWaitForURLOptions { Timeout = 60000 });

                Page.Url.Should().Contain("/dashboard");
            }
            finally
            {
                await permissionPage.GotoAsync(BaseUrl);
                await permissionPage.SetComponentVisibilityAsync(
                    AdminGroupName,
                    ReportPageTab,
                    ReportViewComponentCode,
                    true);
                await permissionPage.WaitForReportMenuVisibleAsync();
            }

            await Page.GotoAsync($"{BaseUrl}report");
            await Page.WaitForURLAsync(
                new Regex(@".*report/?$"),
                new PageWaitForURLOptions { Timeout = 60000 });
            await Page.GetByText("Reports", new PageGetByTextOptions { Exact = true }).WaitForAsync();

            Page.Url.Should().Contain("/report");
        }
    }
}
