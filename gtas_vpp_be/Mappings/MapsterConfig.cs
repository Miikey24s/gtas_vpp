using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.Library;
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
            config.NewConfig<P02_Group, P02_GroupResDTO>();

            config.NewConfig<P02_GroupUpdateReqDTO, P02_Group>()
                .Ignore(d => d.Id)
                .Ignore(d => d.CreateUserId)
                .Ignore(d => d.CreateDate)
                .Ignore(d => d.P04_UserGroups)
                .Ignore(d => d.P06_GroupPageComponentMapping);

            config.NewConfig<PatchComponentMappingReqDTO, P06_GroupPageComponentMapping>()
                .Ignore(d => d.P05_PageComponentMapping)
                .Ignore(d => d.P02_Group)
                .Ignore(d => d.CreateUserId)
                .Ignore(d => d.CreateDate)
                .Ignore(d => d.MemberCompanyCode);

            // Library mappings
            config.NewConfig<L01_Class, L01_ClassResDTO>()
                .Ignore(dest => dest.L02_ClassDetails);
            config.NewConfig<L01_ClassResDTO, L01_Class>()
                .Ignore(dest => dest.L02_ClassDetails);
            
            config.NewConfig<L02_ClassDetail, L02_ClassDetailResDTO>()
                .Ignore(dest => dest.Class)
                .Ignore(dest => dest.VPPs_UOM);
            config.NewConfig<L02_ClassDetailResDTO, L02_ClassDetail>()
                .Ignore(dest => dest.Class)
                .Ignore(dest => dest.VPPs_UOM);
            
            config.NewConfig<L03_VPPCategory, L03_VPPCategoryResDTO>();
            config.NewConfig<L03_VPPCategoryResDTO, L03_VPPCategory>();
            
            config.NewConfig<L04_VPP, L04_VPPResDTO>();
            config.NewConfig<L04_VPPResDTO, L04_VPP>();
            
            config.NewConfig<L05_VPPSupplier, L05_VPPSupplierResDTO>();
            config.NewConfig<L05_VPPSupplierResDTO, L05_VPPSupplier>();
            
            config.NewConfig<L06_VPPSupplierMapping, L06_VPPSupplierMappingResDTO>();
            config.NewConfig<L06_VPPSupplierMappingResDTO, L06_VPPSupplierMapping>();
        }
    }
}
