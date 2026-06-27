using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace gtas_vpp_fe.Components.Pages.Lib.Tabs.Dialog
{
    public partial class Dialog_PriceEditor
    {
        [Inject] public DialogService DialogService { get; set; } = default!;
        [Parameter] public L06_PriceUpdateReqDTO Model { get; set; } = new();
        [Parameter] public List<L05_VPPSupplierResDTO> Suppliers { get; set; } = [];

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
