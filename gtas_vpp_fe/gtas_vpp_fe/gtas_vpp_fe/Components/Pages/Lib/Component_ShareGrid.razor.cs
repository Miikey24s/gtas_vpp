
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Models;
using gtas_vpp_fe.Services;
using Microsoft.AspNetCore.Components.Authorization;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Share;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using Radzen;
using Radzen.Blazor;
using gtas_vpp_shared.DTOs.Res.Library;
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

        [Inject]
        private IAPIServices ApiServices { get; set; } = default!;

        [Inject]
        private IToastService ToastService { get; set; } = default!;

        [Inject]
        private ILogger<Component_ShareGrid<TType>> Logger { get; set; } = default!;

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
        [Parameter] public Func<TType, bool, Task<TType>>? SetStatus { get; set; }
        [Parameter] public string? DataEndpoint { get; set; }
        [Parameter] public bool AllowHardDelete { get; set; } = true;
        [Parameter] public PagePermissionResDTO PagePermissionResDTO { get; set; } = default!;

        private RadzenDataGrid<TType> DataGrid { get; set; } = default!;
        private TType item = default!;
        private string editItem = "";
        private int totalCount;
        private int currentSkip;
        private string? currentFilterExpression;

        private bool onEdit = false;
        private bool IsAdminUser { get; set; } = false;
        private bool CanModifyGrid =>
            PagePermissionResDTO.Components.Any(y => y.IsVisible && y.IsEnable);

        private TType? SelectedRecord => selected_item.FirstOrDefault();

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            if (authState != null)
            {
                IsAdminUser = authState.User.Claims.GetBool(ClaimKeys.IsAdmin);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && DataGrid is not null)
            {
                await DataGrid.Reload();
            }

            await base.OnAfterRenderAsync(firstRender);
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

            await DataGrid.UpdateRow(context);
            await DataGrid.Reload();
            await DataChanged.InvokeAsync(data);
        }
        private async Task DisableRow(TType context)
        {
            await SetDeletedStateAsync(context, true);
        }

        private async Task RestoreRow(TType context)
        {
            await SetDeletedStateAsync(context, false);
        }

        private async Task SetDeletedStateAsync(TType context, bool isDeleted)
        {
            onEdit = false;

            var isDeletedProp = typeof(TType).GetProperty(nameof(BaseDTO.IsDeleted));
            if (isDeletedProp == null || !data.Contains(context))
            {
                return;
            }

            var previousValue = IsDeletedRow(context);
            isDeletedProp.SetValue(context, isDeleted);

            try
            {
                var result = SetStatus is null
                    ? await Update(context)
                    : await SetStatus(context, isDeleted);
                if (result is not null)
                {
                    var index = data.FindIndex(x => x.Id == result.Id);
                    if (index >= 0)
                    {
                        data[index] = result;
                    }
                }
                else
                {
                    isDeletedProp.SetValue(context, previousValue);
                }

                await DataChanged.InvokeAsync(data);
                await DataGrid.Reload();
            }
            catch
            {
                isDeletedProp.SetValue(context, previousValue);
                throw;
            }
        }

        private async Task HardDeleteRow(TType context)
        {
            var confirm = await DialogService.Confirm(
                Loc["PermanentDeleteWarning"].Value,
                Loc["HardDelete"].Value,
                new ConfirmOptions { OkButtonText = Loc["Delete"].Value, CancelButtonText = Loc["Cancel"].Value });

            if (confirm != true)
            {
                return;
            }

            await DeleteRow(context);
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

                await DataGrid.Reload();
                await DataChanged.InvokeAsync(data);
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

        private void OnRowSelect(TType record)
        {
            if (record is null || onEdit)
            {
                return;
            }

            selected_item = [record];
        }

        protected void OnRowRender(RowRenderEventArgs<TType> args)
        {
            if (IsDeletedRow(args.Data))
            {
                AppendRowClass(args.Attributes, "vpp-admin-row-deleted");
            }
        }

        protected bool IsDeletedRow(TType? context)
        {
            if (context is null)
            {
                return false;
            }

            var isDeletedProp = typeof(TType).GetProperty(nameof(BaseDTO.IsDeleted));
            return isDeletedProp?.GetValue(context) is bool isDeleted && isDeleted;
        }

        protected Task ReloadGridAsync()
        {
            return DataGrid.Reload();
        }

        protected string GetColumnDisplayName(PropertyInfo prop, GridColumnMetadata? metadata)
        {
            var key = GetColumnResourceKey(prop.Name);
            if (!string.IsNullOrWhiteSpace(key))
            {
                return Loc[key].Value;
            }

            if (!string.IsNullOrWhiteSpace(metadata?.DisplayName))
            {
                return metadata.DisplayName;
            }

            return HumanizePropertyName(prop.Name);
        }

        protected async Task LoadDataAsync(LoadDataArgs args)
        {
            glb.isBusyPage = true;
            currentSkip = args.Skip ?? 0;
            currentFilterExpression = args.Filter;
            StateHasChanged();

            string? endpoint = null;
            try
            {
                endpoint = BuildGridEndpoint(
                    filter: args.Filter,
                    skip: args.Skip ?? 0,
                    top: args.Top ?? 20,
                    orderby: args.OrderBy);

                var result = await ApiServices.GetFromApiWithTotalCountAsync<List<TType>>(endpoint);
                data = result.Data ?? [];
                totalCount = result.TotalCount;
                if (data.Count > 0 && (SelectedRecord is null || data.All(row => row.Id != SelectedRecord.Id)))
                {
                    selected_item = [data[0]];
                }
                else if (data.Count == 0)
                {
                    selected_item = [];
                }
                await DataChanged.InvokeAsync(data);
            }
            catch (Exception ex)
            {
                data = [];
                totalCount = 0;
                await DataChanged.InvokeAsync(data);
                NotifyLoadFailure(ex, endpoint);
            }
            finally
            {
                glb.isBusyPage = false;
                StateHasChanged();
            }
        }

        protected async Task LoadColumnFilterDataAsync(DataGridLoadColumnFilterDataEventArgs<TType> args)
        {
            if (args.Column is null)
            {
                return;
            }

            var property = args.Column.GetFilterProperty();
            if (string.IsNullOrWhiteSpace(property))
            {
                return;
            }

            string? endpoint = null;
            try
            {
                endpoint = BuildGridEndpoint(
                    filter: currentFilterExpression,
                    skip: args.Skip,
                    top: args.Top,
                    distinct: property,
                    distinctFilter: args.Filter);

                var result = await ApiServices.GetFromApiWithTotalCountAsync<List<TType>>(endpoint);
                args.Data = result.Data ?? [];
                args.Count = result.TotalCount;
            }
            catch (Exception ex)
            {
                args.Data = Array.Empty<TType>();
                args.Count = 0;
                NotifyLoadFailure(ex, endpoint);
            }
        }

        protected string GetColumnMinWidth(
            PropertyInfo prop,
            GridColumnMetadata? metadata,
            TypeCode typeCode,
            bool isAuditProperty)
            => GetColumnWidth(prop, metadata, typeCode, isAuditProperty);

        protected string GetColumnWidth(
            PropertyInfo prop,
            GridColumnMetadata? metadata,
            TypeCode typeCode,
            bool isAuditProperty)
        {
            var desiredWidth = GetDesiredColumnWidth(prop, typeCode, isAuditProperty);
            var configuredWidth = ParsePxWidth(metadata?.Width);
            var effectiveWidth = Math.Max(desiredWidth, configuredWidth ?? 150);

            return $"{effectiveWidth}px";
        }

        protected static IEnumerable<PropertyInfo> GetOrderedGridProperties()
        {
            return typeof(TType)
                .GetProperties()
                .Where(ShouldRenderProperty)
                .OrderBy(GetColumnOrderGroup)
                .ThenBy(prop => GridColumnMetadataRegistry.Get(typeof(TType), prop.Name)?.Order ?? int.MaxValue)
                .ThenBy(prop => prop.MetadataToken);
        }

        protected static bool GetColumnVisible(
            PropertyInfo prop,
            GridColumnMetadata? metadata,
            bool isAuditProperty,
            bool isDeletedProperty)
        {
            if (isDeletedProperty)
            {
                return true;
            }

            if (isAuditProperty)
            {
                return false;
            }

            if (IsTechnicalIdProperty(prop, metadata))
            {
                return false;
            }

            if (IsInspectorFirstProperty(prop))
            {
                return false;
            }

            return true;
        }

        private static bool IsInspectorFirstProperty(PropertyInfo prop)
        {
            if (prop.Name is "OriginalLanguageCode" or "ResolvedLanguageCode"
                or "DisplayName" or "DisplayDescription" or "IsTranslationFallback")
            {
                return true;
            }

            return typeof(TType) == typeof(VppItemResDTO)
                   && prop.Name is nameof(VppItemResDTO.DefaultSupplierName)
                       or nameof(VppItemResDTO.DefaultPrice)
                       or nameof(VppItemResDTO.DefaultVatRate)
                       or nameof(VppItemResDTO.SupplierCount)
                       or nameof(VppItemResDTO.Description);
        }

        protected static bool IsDeletedProperty(PropertyInfo prop)
            => prop.Name.Equals(nameof(BaseDTO.IsDeleted), StringComparison.OrdinalIgnoreCase);

        protected static bool IsAuditProperty(PropertyInfo prop)
        {
            return prop.Name.Equals(nameof(BaseDTO.Id), StringComparison.OrdinalIgnoreCase) ||
                   prop.Name.Equals(nameof(BaseDTO.CreatedByUserId), StringComparison.OrdinalIgnoreCase) ||
                   prop.Name.Equals(nameof(BaseDTO.CreatedAtUtc), StringComparison.OrdinalIgnoreCase) ||
                   prop.Name.Equals("CreatedByUserName", StringComparison.OrdinalIgnoreCase) ||
                   prop.Name.Equals(nameof(BaseDTO.UpdatedByUserId), StringComparison.OrdinalIgnoreCase) ||
                   prop.Name.Equals(nameof(BaseDTO.UpdatedAtUtc), StringComparison.OrdinalIgnoreCase) ||
                   prop.Name.Equals("UpdatedByUserName", StringComparison.OrdinalIgnoreCase) ||
                   IsDeletedProperty(prop);
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
                return 230;
            }

            if (prop.Name.Equals("UomId", StringComparison.OrdinalIgnoreCase))
            {
                return 86;
            }

            if (prop.Name.Equals("VppCategoryId", StringComparison.OrdinalIgnoreCase))
            {
                return 160;
            }

            if (prop.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
            {
                return 180;
            }

            if (typeCode == TypeCode.DateTime)
            {
                return 160;
            }

            if (typeCode == TypeCode.Boolean || prop.Name.StartsWith("Is", StringComparison.OrdinalIgnoreCase))
            {
                return prop.Name.Equals("IsDeleted", StringComparison.OrdinalIgnoreCase) ? 150 : 112;
            }

            if (prop.Name.Contains("Description", StringComparison.OrdinalIgnoreCase))
            {
                return 210;
            }

            if (prop.Name.Equals("DefaultSupplierName", StringComparison.OrdinalIgnoreCase))
            {
                return 150;
            }

            if (prop.Name.Equals("DefaultPrice", StringComparison.OrdinalIgnoreCase))
            {
                return 112;
            }

            if (prop.Name.StartsWith("Address", StringComparison.OrdinalIgnoreCase))
            {
                return 130;
            }

            if (prop.Name.Equals("Ward", StringComparison.OrdinalIgnoreCase) ||
                prop.Name.Equals("City", StringComparison.OrdinalIgnoreCase))
            {
                return 112;
            }

            if (prop.Name.Contains("ShortName", StringComparison.OrdinalIgnoreCase))
            {
                return 150;
            }

            if (prop.Name.EndsWith("Code", StringComparison.OrdinalIgnoreCase) ||
                prop.Name.Contains("Code", StringComparison.OrdinalIgnoreCase))
            {
                return 150;
            }

            if (prop.Name.EndsWith("Name", StringComparison.OrdinalIgnoreCase))
            {
                return 190;
            }

            if (isAuditProperty)
            {
                return 150;
            }

            return typeCode switch
            {
                TypeCode.Decimal or TypeCode.Double or TypeCode.Single or
                TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 or
                TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 => 112,
                _ => 150
            };
        }

        private static string? GetColumnResourceKey(string propertyName)
        {
            return propertyName switch
            {
                nameof(BaseDTO.IsDeleted) => "IsDeleted",
                nameof(BaseDTO.Description) => "Description",
                nameof(BaseDTO.Id) => "Id",
                nameof(BaseDTO.CreatedByUserId) => "CreatedByUserId",
                nameof(BaseDTO.CreatedAtUtc) => "CreatedAtUtc",
                "CreatedByUserName" => "CreatedByUserName",
                nameof(BaseDTO.UpdatedByUserId) => "UpdatedByUserId",
                nameof(BaseDTO.UpdatedAtUtc) => "UpdatedAtUtc",
                "UpdatedByUserName" => "UpdatedByUserName",
                "Code" => "Code",
                "Name" => "Name",
                nameof(LookupCategoryResDTO.ModuleName) => "ModuleName",
                nameof(LookupValueResDTO.Value) => "LookupValue",
                "ExtraField1" => "ExtraField1",
                "ExtraField2" => "ExtraField2",
                "ExtraField3" => "ExtraField3",
                "Sort" => "SortOrder",
                "VppCategoryCode" => "CategoryCode",
                "VppCategoryName" => "CategoryName",
                "VppCategoryId" => "Category",
                "VppCode" => "ProductCode",
                "VppName" => "ProductName",
                "UomId" => "UOM",
                "UomName" => "UOM",
                "Supplier" => "Supplier",
                "SupplierShortName" => "SupplierCode",
                "SupplierName" => "SupplierName",
                "Address1" => "Address1",
                "Address2" => "Address2",
                "Address3" => "Address3",
                "Ward" => "Ward",
                "City" => "City",
                "Price" => "Price",
                _ => null
            };
        }

        private static string HumanizePropertyName(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                return string.Empty;
            }

            var withoutPrefixes = propertyName
                .Replace("VPP", "VPP ", StringComparison.Ordinal)
                .Trim();

            return Regex.Replace(withoutPrefixes, "(?<=[a-z0-9])(?=[A-Z])", " ");
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

        private int GetGridMinWidth()
        {
            var width = CanModifyGrid ? 164 : 52;
            foreach (var prop in GetOrderedGridProperties())
            {
                var metadata = GridColumnMetadataRegistry.Get(typeof(TType), prop.Name);
                var effectiveType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                var typeCode = Type.GetTypeCode(effectiveType);
                var isDeletedProperty = IsDeletedProperty(prop);
                var isAuditProperty = IsAuditProperty(prop);

                if (GetColumnVisible(prop, metadata, isAuditProperty, isDeletedProperty))
                {
                    width += ParsePxWidth(GetColumnWidth(prop, metadata, typeCode, isAuditProperty)) ?? 150;
                }
            }

            return Math.Max(width, 860);
        }

        private string GetEndpointBase()
            => string.IsNullOrWhiteSpace(DataEndpoint)
                ? LibraryEndpointResolver.Resolve<TType>()
                : DataEndpoint.TrimEnd('/');

        private void NotifyLoadFailure(Exception exception, string? endpoint)
        {
            Logger.LogError(
                exception,
                "Failed to load library grid {ResponseType} from {Endpoint}",
                typeof(TType).Name,
                endpoint ?? "<unresolved>");
            ToastService.Error(Loc["Error"].Value, UiErrorMapper.GetMessage(exception, Loc));
        }

        private string BuildGridEndpoint(
            string? filter = null,
            int? skip = null,
            int? top = null,
            string? orderby = null,
            string? distinct = null,
            string? distinctFilter = null)
        {
            var query = new List<string> { "showDeleted=true" };

            if (!string.IsNullOrWhiteSpace(filter))
            {
                query.Add($"filter={Uri.EscapeDataString(filter)}");
            }

            if (skip.HasValue)
            {
                query.Add($"skip={skip.Value}");
            }

            if (top.HasValue)
            {
                query.Add($"top={top.Value}");
            }

            if (!string.IsNullOrWhiteSpace(orderby))
            {
                query.Add($"orderby={Uri.EscapeDataString(orderby)}");
            }

            if (!string.IsNullOrWhiteSpace(distinct))
            {
                query.Add($"distinct={Uri.EscapeDataString(distinct)}");
            }

            if (!string.IsNullOrWhiteSpace(distinctFilter))
            {
                query.Add($"distinctFilter={Uri.EscapeDataString(distinctFilter)}");
            }

            return $"{GetEndpointBase()}?{string.Join("&", query)}";
        }

        private static bool ShouldRenderProperty(PropertyInfo prop)
        {
            if (typeof(TType) == typeof(VppItemResDTO)
                && prop.Name is nameof(VppItemResDTO.UomCode)
                    or nameof(VppItemResDTO.UomName)
                    or nameof(VppItemResDTO.VppCategoryCode)
                    or nameof(VppItemResDTO.VppCategoryName))
            {
                return false;
            }

            if ((prop.PropertyType.IsClass && prop.PropertyType != typeof(string)) ||
                (typeof(System.Collections.IEnumerable).IsAssignableFrom(prop.PropertyType) && prop.PropertyType != typeof(string)))
            {
                return false;
            }

            var metadata = GridColumnMetadataRegistry.Get(typeof(TType), prop.Name);
            return !(metadata?.Ignore ?? false) || IsAuditProperty(prop);
        }

        private static int GetColumnOrderGroup(PropertyInfo prop)
        {
            var metadata = GridColumnMetadataRegistry.Get(typeof(TType), prop.Name);

            if (IsDeletedProperty(prop))
            {
                return 0;
            }

            if (prop.Name.EndsWith("Code", StringComparison.OrdinalIgnoreCase) ||
                prop.Name.Contains("Code", StringComparison.OrdinalIgnoreCase))
            {
                return 10;
            }

            if (prop.Name.EndsWith("Name", StringComparison.OrdinalIgnoreCase))
            {
                return 20;
            }

            if (metadata?.IsDropdownList == true)
            {
                return 30;
            }

            if (prop.Name.Contains("Price", StringComparison.OrdinalIgnoreCase) ||
                prop.Name.Contains("Amount", StringComparison.OrdinalIgnoreCase))
            {
                return 40;
            }

            if (prop.Name.Contains("Description", StringComparison.OrdinalIgnoreCase))
            {
                return 80;
            }

            if (IsAuditProperty(prop) || IsTechnicalIdProperty(prop, metadata))
            {
                return 100;
            }

            return 50;
        }

        private static bool IsTechnicalIdProperty(PropertyInfo prop, GridColumnMetadata? metadata)
        {
            if (metadata?.IsDropdownList == true)
            {
                return false;
            }

            return prop.Name.Equals(nameof(BaseDTO.Id), StringComparison.OrdinalIgnoreCase) ||
                   prop.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase);
        }

        private static void AppendRowClass(IDictionary<string, object> attributes, string className)
        {
            if (attributes.TryGetValue("class", out var current) && current is not null)
            {
                attributes["class"] = $"{current} {className}";
                return;
            }

            attributes["class"] = className;
        }
    }
}
