using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Library;
using gtas_vpp_fe.Services;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace gtas_vpp_fe.Components.Pages.Lib.Tabs.Dialog
{
    public partial class Dialog_PriceListEditor
    {
        [Inject] public DialogService DialogService { get; set; } = default!;
        [Inject] public IToastService ToastService { get; set; } = default!;
        [Parameter] public PriceListUpdateReqDTO Model { get; set; } = new();
        [Parameter] public bool IsClone { get; set; }
        [Parameter] public List<SupplierResDTO> Suppliers { get; set; } = [];

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(Model.Code) || string.IsNullOrWhiteSpace(Model.Name))
            {
                ToastService.Show(NotificationSeverity.Warning, Loc["ValidationTitle"], Loc["PriceListName"], 4000, false);
                return;
            }

            DialogService.Close(Model);
        }

        private void Cancel()
        {
            DialogService.Close(null);
        }
    }
}
