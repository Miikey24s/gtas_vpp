namespace gtas_vpp_shared.DTOs.Res.Library;

public sealed class PriceListImportIssueResDTO
{
    public int RowNumber { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Column { get; set; }
}

public sealed class PriceListImportColumnMappingResDTO
{
    public int SourceColumnIndex { get; set; }
    public string SourceColumn { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsCustom { get; set; }
}

public sealed class PriceListImportSourceColumnResDTO
{
    public int ColumnIndex { get; set; }
    public string SourceColumn { get; set; } = string.Empty;
    public string? SuggestedTargetField { get; set; }
    public bool IsAiSuggested { get; set; }
    public List<string> SampleValues { get; set; } = [];
}

public sealed class PriceListImportAnalysisResDTO
{
    public string FileName { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;
    public string FileFormat { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public bool CanPreviewAutomatically { get; set; }
    public bool AiSuggestionsAvailable { get; set; }
    public List<PriceListImportSourceColumnResDTO> Columns { get; set; } = [];
    public List<PriceListImportIssueResDTO> Issues { get; set; } = [];
}

public sealed class PriceListImportRowResDTO
{
    public int RowNumber { get; set; }
    public string? ItemCode { get; set; }
    public string? ItemName { get; set; }
    public string? MatchedItemName { get; set; }
    public string? UnitName { get; set; }
    public string? MatchedUnitName { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? VatRate { get; set; }
    public string? Note { get; set; }
    public string Action { get; set; } = string.Empty;
    public List<PriceListImportIssueResDTO> Issues { get; set; } = [];
}

public class PriceListImportBatchResDTO
{
    public Guid Id { get; set; }
    public Guid PriceListId { get; set; }
    public Guid SupplierId { get; set; }
    public string PriceListName { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;
    public string FileFormat { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int AddedRows { get; set; }
    public int UpdatedRows { get; set; }
    public int UnchangedRows { get; set; }
    public int WarningRows { get; set; }
    public int ErrorRows { get; set; }
    public bool DuplicateFileWarning { get; set; }
    public bool UsedCustomMapping { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? ResultMessage { get; set; }
    public byte[]? RowVersion { get; set; }
}

public sealed class PriceListImportPreviewResDTO : PriceListImportBatchResDTO
{
    public bool CanConfirm => Status == "Ready" && ErrorRows == 0;
    public List<PriceListImportColumnMappingResDTO> ColumnMappings { get; set; } = [];
    public List<PriceListImportIssueResDTO> Issues { get; set; } = [];
    public List<PriceListImportRowResDTO> Rows { get; set; } = [];
}
