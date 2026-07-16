namespace gtas_vpp_shared.DTOs.Res.Library;

public enum PriceResolutionBlockerCode
{
    None = 0,
    InvalidRequest = 1,
    MissingPrice = 2,
    AmbiguousPrice = 3,
    ExpiredPrice = 4,
    SupplierMismatch = 5,
    MinimumOrderQuantity = 6,
    IncompleteLegacyBackfill = 7
}
