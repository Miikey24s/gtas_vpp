using System.Reflection;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace gtas_vpp_be.Tests;

public class VPPCodeGeneratorTests
{
    [Fact]
    public void GenerateVPPCode_ValidInput_MatchesExpectedFormat()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var service = CreateService(context, new DateTime(2026, 4, 1, 9, 7, 8));

        var result = InvokeGenerateVPPCode(service, 2026, 4);

        Assert.Matches("^VPP-202604-[a-f0-9]{32}$", result);
        Assert.Equal(43, result.Length);
    }

    [Fact]
    public void GenerateVPPCode_1000Times_AllUnique()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var service = CreateService(context, new DateTime(2026, 4, 1, 9, 7, 8));
        var codes = new HashSet<string>();

        for (var i = 0; i < 1000; i++)
        {
            codes.Add(InvokeGenerateVPPCode(service, 2026, 4));
        }

        Assert.Equal(1000, codes.Count);
    }

    private static VPPRequestService CreateService(gtas_vpp_be.Service.Helpers.Context.VPPContext context, DateTime now)
    {
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var factory = ServiceTestHelpers.CreateUnitOfWorkFactoryMock(unitOfWork.Object);
        var httpContextAccessor = ServiceTestHelpers.CreateHttpContextAccessor();
        var dateTimeProvider = new FakeDateTimeProvider(now);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["VPPDeadlineDay"] = "5"
            })
            .Build();

        return new VPPRequestService(
            factory.Object,
            httpContextAccessor,
            unitOfWork.Object,
            dateTimeProvider,
            config,
                ServiceTestHelpers.CreateEnvironmentResolver(),
            new UserNameResolver(),
            NullLogger<BaseServices>.Instance,
            Options.Create(new JiraSettings()));
    }

    private static string InvokeGenerateVPPCode(VPPRequestService service, int year, int month)
        => (string)typeof(VPPRequestService)
            .GetMethod("GenerateVPPCode", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(service, new object[] { year, month })!;
}
