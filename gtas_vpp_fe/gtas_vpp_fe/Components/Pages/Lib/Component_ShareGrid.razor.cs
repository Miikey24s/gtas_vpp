
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Share;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using Radzen;
using Radzen.Blazor;

namespace gtas_vpp_fe.Components.Pages.Lib
{
    public partial class Component_ShareGrid<TType> : ComponentBase where TType : BaseDTO, new ()
    {
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
        protected override Task OnInitializedAsync()
        {
            return base.OnInitializedAsync();
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
                //if (result != null)
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
                // For demo purposes only
                if (result == true)
                    data.Remove(context);
                // For production
                //dbContext.SaveChanges();

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

        Task RowDoubleClickHandle(DataGridRowMouseEventArgs<TType> args)
        {
            if (args.Data == null)
            {
                return Task.CompletedTask;
            }

            if (sp_Authentication_GetPermissionSinglePage.List_Component.Any(y => (y.ComponentCode?.Equals("0001_HD")??false) && y.IsVisible))
            {
                if (onEdit == true)
                    return Task.CompletedTask;
                else
                {
                    onEdit = true;
                    editItem = JsonConvert.SerializeObject(data);
                    return DataGrid.EditRow(args.Data);
                }
            }
            else return Task.CompletedTask;
        }
    }
}
