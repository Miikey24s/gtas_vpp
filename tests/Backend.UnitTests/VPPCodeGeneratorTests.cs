using System.Reflection;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace gtas_vpp_be.Tests;

public class VppCodeGeneratorTests
{
    [Fact]
    public void GenerateVppCode_ValidInput_MatchesExpectedFormat()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var service = CreateService(context, new DateTime(2026, 4, 1, 9, 7, 8));

        var result = InvokeGenerateVppCode(service, 2026, 4);

        Assert.Matches("^VPP-202604-[a-f0-9]{32}$", result);
        Assert.Equal(43, result.Length);
    }

    [Fact]
    public void GenerateVppCode_1000Times_AllUnique()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var service = CreateService(context, new DateTime(2026, 4, 1, 9, 7, 8));
        var codes = new HashSet<string>();

        for (var i = 0; i < 1000; i++)
        {
            codes.Add(InvokeGenerateVppCode(service, 2026, 4));
        }

        Assert.Equal(1000, codes.Count);
    }

    private static VPPRequestService CreateService(gtas_vpp_be.Service.Helpers.Context.VPPContext context, DateTime now)
    {
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var dateTimeProvider = new FakeDateTimeProvider(now);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["VPPDeadlineDay"] = "5"
            })
            .Build();

        return new VPPRequestService(
            unitOfWork.Object,
            dateTimeProvider,
            config);
    }

    private static string InvokeGenerateVppCode(VPPRequestService service, int year, int month)
        => (string)typeof(VPPRequestService)
            .GetMethod("GenerateVppCode", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(service, new object[] { year, month })!;
}
