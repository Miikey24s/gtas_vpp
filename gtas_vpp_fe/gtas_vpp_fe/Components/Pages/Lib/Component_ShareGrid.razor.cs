
using gtas_vpp_fe.Helpers;
using Microsoft.AspNetCore.Components.Authorization;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Share;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using Radzen;
using Radzen.Blazor;
using System.Reflection;
using System.Text.RegularExpressions;

namespace gtas_vpp_fe.Components.Pages.Lib
{
    public partial class Component_ShareGrid<TType> : ComponentBase where TType : BaseDTO, new ()
    {
        [Inject]
        private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

        [Inject]
        private DialogService DialogService { get; set; } = default!;

        private List<TType> data = new();
        [Parameter]
        public List<TType> Data
        {
            get => data;
            set
            {
                data = value;
            }
        }
        [Parameter] public EventCallback<List<TType>> DataChanged { get; set; }
        [Parameter] public Dictionary<string, IList<DropdownModel>> DicDropdownData { get; set; } = default!;

        [Parameter] public Func<TType, Task<TType>> Add { get; set; } = default!;
        [Parameter] public Func<TType, Task<TType>> Update { get; set; } = default!;
        [Parameter] public Func<TType, Task<bool>> Delete { get; set; } = default!;
        [Parameter] public sp_Authentication_GetPermissionSinglePage sp_Authentication_GetPermissionSinglePage { get; set; } = default!;

        private RadzenDataGrid<TType> DataGrid { get; set; } = default!;
        private TType item = default!;
        private string editItem = "";

        private bool onEdit = false;
        private bool IsAdminUser { get; set; } = false;
        private bool CanModifyGrid =>
            sp_Authentication_GetPermissionSinglePage.List_Component.Any(y => y.IsVisible && y.IsEnable);

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            if (authState != null)
            {
                IsAdminUser = authState.User.Claims.GetBool(ClaimKeys.IsAdmin);
            }
        }
        void OnUpdateRow(TType context)
        {
            if (context.Equals(item))
            {
                item = default!;
            }
        }
        void OnCreateRow(TType context)
        {
        }
        private async Task InsertRow()
        {
            editItem = JsonConvert.SerializeObject(data);
            onEdit = true;
            item = new TType();
            await DataGrid.InsertRow(item);
        }
        private Task EditRow(TType context)
        {
            editItem = JsonConvert.SerializeObject(data);
            onEdit = true;
            return DataGrid.EditRow(context);
        }
        private async Task SaveRow(TType context)
        {
            onEdit = false;
            if (context.Equals(item))
            {
                item = default!;
                var result = await Add(context);
                if (result.Id != Guid.Empty) data.Insert(0, result);
                else data = JsonConvert.DeserializeObject<List<TType>>(editItem) ?? new List<TType>();
            }
            else
            {
                _ = await Update(context);
            }
            await DataGrid.Reload();

            await Task.WhenAll(DataChanged.InvokeAsync(data), DataGrid.UpdateRow(context));
        }
        private async Task DeleteRow(TType context)
        {
            onEdit = false;
            if (context == item)
            {
                item = default!;
            }

            if (data.Contains(context))
            {
                var result = await Delete(context);
                if (result == true)
                    data.Remove(context);

                await Task.WhenAll(DataGrid.Reload());//, DataChanged.InvokeAsync(data));
            }
            else
            {
                DataGrid.CancelEditRow(context);
            }
        }
        private Task CancelEdit(TType context)
        {
            onEdit = false;
            if (context == item)
            {
                item = default!;
            }
            DataGrid.CancelEditRow(context);
            data = JsonConvert.DeserializeObject<List<TType>>(editItem) ?? new List<TType>();
            return Task.CompletedTask;
        }

        async Task RowDoubleClickHandle(DataGridRowMouseEventArgs<TType> args)
        {
            if (args.Data == null || onEdit)
            {
                return;
            }

            var recordTitle = "Record Details";
            var nameProp = typeof(TType).GetProperty("Name") ?? 
                           typeof(TType).GetProperties().FirstOrDefault(p => p.Name.EndsWith("Name") || p.Name.EndsWith("Code"));
            if (nameProp != null)
            {
                var val = nameProp.GetValue(args.Data)?.ToString();
                if (!string.IsNullOrEmpty(val))
                {
                    recordTitle = $"{val} - Details";
                }
            }

            await DialogService.OpenSideAsync<Component_RecordInspector<TType>>(
                recordTitle,
                new Dictionary<string, object?> { { "Record", args.Data } },
                options: new SideDialogOptions { Position = DialogPosition.Right, Width = "500px" }
            );
        }

        protected void OnRowRender(RowRenderEventArgs<TType> args)
        {
            var isDeletedProp = typeof(TType).GetProperty("IsDeleted");
            if (isDeletedProp != null)
            {
                var isDeletedVal = isDeletedProp.GetValue(args.Data);
                if (isDeletedVal is bool isDeleted && isDeleted)
                {
                    args.Attributes.Add("style", "opacity: 0.6; background-color: var(--rz-danger-lighter, rgba(255, 0, 0, 0.05)) !important;");
                }
            }
        }

        protected string GetColumnMinWidth(
            PropertyInfo prop,
            GridColumnPropertyAttribute? attr,
            TypeCode typeCode,
            bool isAuditProperty)
        {
            var desiredWidth = GetDesiredColumnWidth(prop, typeCode, isAuditProperty);
            var configuredWidth = ParsePxWidth(attr?.Width);

            return configuredWidth.HasValue && configuredWidth.Value > desiredWidth
                ? $"{configuredWidth.Value}px"
                : $"{desiredWidth}px";
        }

        protected IReadOnlyDictionary<string, object> GetInputAttributes(string label)
            => new Dictionary<string, object>
            {
                ["aria-label"] = label
            };

        private static int GetDesiredColumnWidth(PropertyInfo prop, TypeCode typeCode, bool isAuditProperty)
        {
            if (prop.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
            {
                return 250;
            }

            if (prop.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
            {
                return 160;
            }

            if (typeCode == TypeCode.DateTime)
            {
                return 160;
            }

            if (typeCode == TypeCode.Boolean || prop.Name.StartsWith("Is", StringComparison.OrdinalIgnoreCase))
            {
                return 120;
            }

            if (prop.Name.Contains("Description", StringComparison.OrdinalIgnoreCase))
            {
                return 220;
            }

            if (prop.Name.EndsWith("Name", StringComparison.OrdinalIgnoreCase))
            {
                return 180;
            }

            if (isAuditProperty)
            {
                return 150;
            }

            return typeCode switch
            {
                TypeCode.Decimal or TypeCode.Double or TypeCode.Single or
                TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 or
                TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 => 120,
                _ => 150
            };
        }

        private static int? ParsePxWidth(string? width)
        {
            if (string.IsNullOrWhiteSpace(width))
            {
                return null;
            }

            var match = Regex.Match(width, @"^\s*(\d+)\s*px\s*$", RegexOptions.IgnoreCase);
            return match.Success && int.TryParse(match.Groups[1].Value, out var value)
                ? value
                : null;
        }
    }
}
