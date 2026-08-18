using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.Library;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Req.Permission;
using gtas_vpp_shared.DTOs.Res.Library;
using gtas_vpp_shared.DTOs.Res.Permission;
using Mapster;

namespace gtas_vpp_be.Mappings
{
    public static class MapsterConfig
    {
        public static void Register(TypeAdapterConfig config)
        {
            config.NewConfig<PermissionGroup, PermissionGroupResDTO>();

            config.NewConfig<PermissionGroupUpdateReqDTO, PermissionGroup>()
                .Ignore(d => d.Id)
                .Ignore(d => d.CreatedByUserId)
                .Ignore(d => d.CreatedAtUtc)
                .Ignore(d => d.UserGroupMemberships!)
                .Ignore(d => d.GroupPageComponentMappings!);

            config.NewConfig<PatchComponentMappingReqDTO, GroupPageComponentMapping>()
                .Ignore(d => d.PageComponentMapping!)
                .Ignore(d => d.PermissionGroup!)
                .Ignore(d => d.CreatedByUserId)
                .Ignore(d => d.CreatedAtUtc)
                .Ignore(d => d.MemberCompanyCode);

            // Ánh xạ danh mục.
            config.NewConfig<LookupCategory, LookupCategoryResDTO>()
                .Ignore(dest => dest.LookupValues!);
            config.NewConfig<LookupCategoryResDTO, LookupCategory>()
                .Ignore(dest => dest.LookupValues!);

            config.NewConfig<LookupValue, LookupValueResDTO>()
                .Ignore(dest => dest.Category!)
                .Ignore(dest => dest.VppItemsByUom!);
            config.NewConfig<LookupValueResDTO, LookupValue>()
                .Ignore(dest => dest.Category!)
                .Ignore(dest => dest.VppItemsByUom!);

            config.NewConfig<VppCategory, VppCategoryResDTO>();
            config.NewConfig<VppCategoryResDTO, VppCategory>();

            config.NewConfig<VppItem, VppItemResDTO>();
            config.NewConfig<VppItemResDTO, VppItem>();

            config.NewConfig<Supplier, SupplierResDTO>();
            config.NewConfig<SupplierResDTO, Supplier>();

            config.NewConfig<SupplierProductMapping, SupplierProductMappingResDTO>();
            config.NewConfig<SupplierProductMappingResDTO, SupplierProductMapping>();

            config.NewConfig<PriceList, PriceListResDTO>();
            config.NewConfig<PriceListResDTO, PriceList>();

            // Ánh xạ VPP.
            config.NewConfig<gtas_vpp_be.Model.VPP.VppRequestDetail, gtas_vpp_shared.DTOs.Res.VPP.VppRequestDetailResDTO>()
                .Map(dest => dest.VppCode, src => src.VppItem != null ? src.VppItem.VppCode : null)
                .Map(dest => dest.VppName, src => src.VppItem != null ? src.VppItem.VppName : null)
                .Map(dest => dest.UomCode, src => src.VppItem != null && src.VppItem.Uom != null ? src.VppItem.Uom.Code : null)
                .Map(dest => dest.UomName, src => src.VppItem != null && src.VppItem.Uom != null ? src.VppItem.Uom.Value : null)
                .Map(dest => dest.CategoryName, src => src.VppItem != null && src.VppItem.VppCategory != null ? src.VppItem.VppCategory.VppCategoryName : null)
                .Map(dest => dest.MaxQuantityPerOrder, src => src.VppItem != null
                    ? src.VppItem.MaxQuantityPerOrder
                    : VppOrderQuantityLimits.Default);

            config.NewConfig<gtas_vpp_be.Model.VPP.VppRequest, gtas_vpp_shared.DTOs.Res.VPP.VppRequestResDTO>()
                .Map(dest => dest.TotalLines, src => src.RequestDetails.Count(d => !d.IsDeleted))
                .Map(dest => dest.TotalQty, src => src.RequestDetails.Where(d => !d.IsDeleted).Sum(d => (int?)d.Qty) ?? 0)
                .Map(dest => dest.TotalAmount, src => src.RequestDetails.Where(d => !d.IsDeleted).Sum(d => (long?)(d.Qty * d.CurrentSinglePrice)) ?? 0)
                .Map(dest => dest.SettledByPriceListName, src => src.SettledByPriceList != null ? src.SettledByPriceList.PriceListName : null)
                .Map(dest => dest.Items, src => src.RequestDetails.Where(d => !d.IsDeleted));
        }
    }
}
