using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Service.Helpers.DTOs.Req.Permission;
using gtas_vpp_be.Service.Helpers.DTOs.Res.Permission;
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
        }
    }
}