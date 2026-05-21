using gtas_vpp_shared.DTOs.Req.Library;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace gtas_vpp_fe.Components.Pages.Lib.Tabs.Dialog
{
    public partial class Dialog_PriceListEditor
    {
        [Inject] public DialogService DialogService { get; set; } = default!;
        [Parameter] public L07_PriceListUpdateReqDTO Model { get; set; } = new();
        [Parameter] public bool IsClone { get; set; }

        private void Save()
        {
            DialogService.Close(Model);
        }

        private void Cancel()
        {
            DialogService.Close(null);
        }
    }
}
