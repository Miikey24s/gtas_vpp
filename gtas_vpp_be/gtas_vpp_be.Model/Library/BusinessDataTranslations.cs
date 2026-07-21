using gtas_vpp_be.Model.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gtas_vpp_be.Model.Library;

public enum BusinessTranslationStatus
{
    Draft = 0,
    Approved = 1
}

public enum BusinessTranslationSource
{
    Manual = 0,
    Import = 1,
    AiDraft = 2
}

public interface ITranslatableBusinessEntity
{
    string OriginalLanguageCode { get; set; }
}

public interface IBusinessDataTranslation
{
    Guid Id { get; set; }
    string LanguageCode { get; set; }
    string Name { get; set; }
    string? Description { get; set; }
    BusinessTranslationStatus Status { get; set; }
    BusinessTranslationSource Source { get; set; }
    int CreatedByUserId { get; set; }
    DateTime CreatedAtUtc { get; set; }
    int UpdatedByUserId { get; set; }
    DateTime UpdatedAtUtc { get; set; }
    bool IsDeleted { get; set; }
}

public abstract class BusinessDataTranslationBase : BaseModel, IBusinessDataTranslation
{
    [Required, StringLength(5)]
    public string LanguageCode { get; set; } = "vi";

    [Required, StringLength(250)]
    public string Name { get; set; } = string.Empty;

    public BusinessTranslationStatus Status { get; set; } = BusinessTranslationStatus.Draft;

    public BusinessTranslationSource Source { get; set; } = BusinessTranslationSource.Manual;
}

[Table("LookupCategoryTranslations")]
public sealed class LookupCategoryTranslation : BusinessDataTranslationBase
{
    public Guid LookupCategoryId { get; set; }
    public LookupCategory LookupCategory { get; set; } = default!;
}

[Table("LookupValueTranslations")]
public sealed class LookupValueTranslation : BusinessDataTranslationBase
{
    public Guid LookupValueId { get; set; }
    public LookupValue LookupValue { get; set; } = default!;
}

[Table("VppCategoryTranslations")]
public sealed class VppCategoryTranslation : BusinessDataTranslationBase
{
    public Guid VppCategoryId { get; set; }
    public VppCategory VppCategory { get; set; } = default!;
}

[Table("VppItemTranslations")]
public sealed class VppItemTranslation : BusinessDataTranslationBase
{
    public Guid VppItemId { get; set; }
    public VppItem VppItem { get; set; } = default!;
}

[Table("SupplierTranslations")]
public sealed class SupplierTranslation : BusinessDataTranslationBase
{
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = default!;
}

[Table("PriceListTranslations")]
public sealed class PriceListTranslation : BusinessDataTranslationBase
{
    public Guid PriceListId { get; set; }
    public PriceList PriceList { get; set; } = default!;
}

[Table("DepartmentTranslations")]
public sealed class DepartmentTranslation : BusinessDataTranslationBase
{
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = default!;
}
