using System.Diagnostics;
using System.Text.Json;
using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class LongSessionStabilityTests : TestBase, IAuthenticatedUiTest
{
    private readonly ITestOutputHelper output;

    public LongSessionStabilityTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public async Task RepeatedEnhancedNavigation_DoesNotAccumulateDomOrEventListeners()
    {
        await Page.SetViewportSizeAsync(1366, 768);
        await LoginAsDefaultUserAsync();

        var cdp = await Page.Context.NewCDPSessionAsync(Page);
        await cdp.SendAsync("HeapProfiler.enable");
        await NavigateToPrimaryTabAsync("Lịch sử đơn");
        var scrollBurstFrames = await MeasureScrollBurstFramesAsync();
        output.WriteLine("A burst of 250 scroll events scheduled {0} animation frames.", scrollBurstFrames);

        // Vòng đầu làm nóng module JS, cache route và Radzen để phép đo sau chỉ phản ánh
        // tài nguyên bị giữ lại qua enhanced navigation trong cùng một Blazor circuit.
        await NavigateRoundAsync();
        var baseline = await ReadBrowserCountersAsync(cdp);

        var roundDurations = new List<double>();
        for (var round = 0; round < 8; round++)
        {
            var stopwatch = Stopwatch.StartNew();
            await NavigateRoundAsync();
            stopwatch.Stop();
            roundDurations.Add(stopwatch.Elapsed.TotalMilliseconds);
            var sample = await ReadBrowserCountersAsync(cdp);
            output.WriteLine(
                "Round {0}: documents={1}, nodes={2}, listeners={3}, duration={4:F0}ms.",
                round + 1,
                sample.Documents,
                sample.Nodes,
                sample.JsEventListeners,
                stopwatch.Elapsed.TotalMilliseconds);
        }

        var final = await ReadBrowserCountersAsync(cdp);
        var firstHalfAverage = roundDurations.Take(4).Average();
        var secondHalfAverage = roundDurations.Skip(4).Average();
        var unrelatedDomChurnDuration = await MeasureUnrelatedDomChurnAsync();
        output.WriteLine(
            "Baseline documents={0}, nodes={1}, listeners={2}; final documents={3}, nodes={4}, listeners={5}; navigation averages={6:F0}ms/{7:F0}ms.",
            baseline.Documents,
            baseline.Nodes,
            baseline.JsEventListeners,
            final.Documents,
            final.Nodes,
            final.JsEventListeners,
            firstHalfAverage,
            secondHalfAverage);
        output.WriteLine("Unrelated DOM churn settled in {0:F1}ms.", unrelatedDomChurnDuration);

        final.Documents.Should().BeLessThanOrEqualTo(
            baseline.Documents + 1,
            "enhanced navigation must keep one live document instead of retaining old route documents");
        final.Nodes.Should().BeLessThanOrEqualTo(
            baseline.Nodes + 80,
            "route components and virtualized rows must be collectible after navigation");
        final.JsEventListeners.Should().BeLessThanOrEqualTo(
            baseline.JsEventListeners + 8,
            "route-local observers and listeners must be detached when their component leaves the DOM");
        secondHalfAverage.Should().BeLessThan(
            firstHalfAverage * 1.75,
            "later navigation rounds should not progressively slow down because retained DOM work keeps growing");
        unrelatedDomChurnDuration.Should().BeLessThan(
            250,
            "global UI observers must ignore unrelated DOM churn instead of rescanning every inserted subtree");
        scrollBurstFrames.Should().BeLessThanOrEqualTo(
            12,
            "high-frequency scroll handlers must coalesce work to one animation frame instead of building a backlog");
    }

    private async Task NavigateRoundAsync()
    {
        await NavigateToPrimaryTabAsync("Lịch sử đơn");
        await NavigateToPrimaryTabAsync("Danh mục mặt hàng");
        await NavigateToPrimaryTabAsync("Tổng hợp phòng ban");
        await NavigateToPrimaryTabAsync("Đơn hàng của tôi");
    }

    private async Task NavigateToPrimaryTabAsync(string label)
    {
        var tab = Page.Locator(".vpp-header-tabs > .vpp-header-tab")
            .Filter(new LocatorFilterOptions { HasText = label });
        await tab.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30_000
        });
        await tab.ClickAsync();
        await Page.WaitForFunctionAsync(
            """
            expected => [...document.querySelectorAll('.vpp-header-tabs > .vpp-header-tab.is-active')]
                .some(tab => tab.textContent.trim() === expected)
            """,
            label,
            new PageWaitForFunctionOptions { Timeout = 30_000 });
        await WaitForRenderSettleAsync();
    }

    private static async Task<BrowserCounters> ReadBrowserCountersAsync(ICDPSession cdp)
    {
        await cdp.SendAsync("HeapProfiler.collectGarbage");
        var response = await cdp.SendAsync("Memory.getDOMCounters")
            ?? throw new InvalidOperationException("CDP Memory.getDOMCounters returned no payload.");
        return new BrowserCounters(
            ReadInt32(response, "documents"),
            ReadInt32(response, "nodes"),
            ReadInt32(response, "jsEventListeners"));
    }

    private static int ReadInt32(JsonElement response, string propertyName)
        => response.TryGetProperty(propertyName, out var value)
            ? value.GetInt32()
            : throw new InvalidOperationException($"CDP Memory.getDOMCounters omitted '{propertyName}'.");

    private sealed record BrowserCounters(int Documents, int Nodes, int JsEventListeners);

    private Task<double> MeasureUnrelatedDomChurnAsync()
        => Page.EvaluateAsync<double>(
            """
            async () => {
                const host = document.createElement('table');
                const body = document.createElement('tbody');
                host.appendChild(body);
                document.body.appendChild(host);
                await new Promise(resolve => requestAnimationFrame(resolve));
                const fragment = document.createDocumentFragment();
                for (let index = 0; index < 500; index++) {
                    const row = document.createElement('tr');
                    row.className = 'rz-data-row performance-probe-row';
                    row.append(document.createElement('td'), document.createElement('td'));
                    fragment.appendChild(row);
                }
                const startedAt = performance.now();
                body.appendChild(fragment);
                await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
                body.replaceChildren();
                await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
                host.remove();
                return performance.now() - startedAt;
            }
            """);

    private Task<int> MeasureScrollBurstFramesAsync()
        => Page.EvaluateAsync<int>(
            """
            async () => {
                const original = window.requestAnimationFrame;
                let scheduled = 0;
                window.requestAnimationFrame = function (callback) {
                    scheduled++;
                    return original.call(window, callback);
                };
                try {
                    for (let index = 0; index < 250; index++) {
                        window.dispatchEvent(new Event('scroll'));
                    }
                    await new Promise(resolve => original.call(window, () => original.call(window, resolve)));
                    return scheduled;
                } finally {
                    window.requestAnimationFrame = original;
                }
            }
            """);
}
