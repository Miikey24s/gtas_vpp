using gtas_vpp_be.Model.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gtas_vpp_be.Model.Library;

[Table("PriceListImportBatches")]
public class PriceListImportBatch : BaseModel
{
    public Guid PriceListId { get; set; }
    public PriceList? PriceList { get; set; }
    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    [StringLength(260)]
    public string OriginalFileName { get; set; } = string.Empty;

    [StringLength(64)]
    public string FileHash { get; set; } = string.Empty;

    [StringLength(8)]
    public string FileFormat { get; set; } = string.Empty;

    [StringLength(32)]
    public string SchemaVersion { get; set; } = "pricing-import-v1";

    public PriceListImportBatchStatus Status { get; set; } = PriceListImportBatchStatus.Validating;
    public int TotalRows { get; set; }
    public int AddedRows { get; set; }
    public int UpdatedRows { get; set; }
    public int UnchangedRows { get; set; }
    public int WarningRows { get; set; }
    public int ErrorRows { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int? ConfirmedByUserId { get; set; }

    [StringLength(1000)]
    public string? ResultMessage { get; set; }

    public bool UsedCustomMapping { get; set; }
    public string? ColumnMappingsJson { get; set; }
    public string? NormalizedRowsJson { get; set; }
    public string? IssuesJson { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];
}
