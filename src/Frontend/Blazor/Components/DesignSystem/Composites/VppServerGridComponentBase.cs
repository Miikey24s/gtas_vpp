using Microsoft.AspNetCore.Components;
using Radzen.Blazor;

namespace gtas_vpp_fe.Components.DesignSystem.Composites;

/// <summary>
/// Bảo đảm grid server-paging tải lần đầu sau khi circuit Blazor đã tương tác.
/// Prerender có thể dựng grid trước khi Radzen phát sự kiện LoadData.
/// </summary>
public abstract class VppServerGridComponentBase<TItem> : ComponentBase where TItem : notnull
{
    private bool initialGridLoadRequested;

    protected abstract RadzenDataGrid<TItem>? InitialGrid { get; }

    protected virtual bool CanRequestInitialGridLoad => true;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (!firstRender
            || initialGridLoadRequested
            || !CanRequestInitialGridLoad
            || InitialGrid is null)
        {
            return;
        }

        initialGridLoadRequested = true;
        await InitialGrid.Reload();
    }
}
