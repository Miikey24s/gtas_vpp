using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace gtas_vpp_be.Service.Services;

public sealed class PriceListImportService(
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    PriceListImportFileParser parser,
    IPriceListColumnMappingSuggester mappingSuggester) : IPriceListImportService
{
    private const string SchemaVersion = "pricing-import-v1";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> SupportedMappingFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "ItemCode",
        "SupplierSku",
        "ItemName",
        "UnitPrice",
        "VatRate",
        "MinimumOrderQuantity",
        "LeadTimeDays",
        "IsDefault",
        "Note"
    };

    public async Task<PriceListImportAnalysisResDTO> AnalyzeAsync(
        Guid priceListId,
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        await GetEditablePriceListAsync(priceListId, cancellationToken);
        var analysis = await parser.AnalyzeAsync(content, fileName, cancellationToken);
        analysis.AiSuggestionsAvailable = mappingSuggester.IsAvailable;
        if (!mappingSuggester.IsAvailable)
        {
            return analysis;
        }

        var suggestions = await mappingSuggester.SuggestAsync(analysis.Columns, cancellationToken);
        var usedTargets = analysis.Columns
            .Where(column => !string.IsNullOrWhiteSpace(column.SuggestedTargetField))
            .Select(column => column.SuggestedTargetField!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var suggestion in suggestions)
        {
            var column = analysis.Columns.FirstOrDefault(item => item.ColumnIndex == suggestion.ColumnIndex);
            if (column is null
                || !string.IsNullOrWhiteSpace(column.SuggestedTargetField)
                || !SupportedMappingFields.Contains(suggestion.TargetField)
                || !usedTargets.Add(suggestion.TargetField))
            {
                continue;
            }

            column.SuggestedTargetField = SupportedMappingFields.First(field => string.Equals(
                field,
                suggestion.TargetField,
                StringComparison.OrdinalIgnoreCase));
            column.IsAiSuggested = true;
        }

        analysis.CanPreviewAutomatically = new[] { "ItemCode", "UnitPrice" }
            .All(required => analysis.Columns.Any(column => string.Equals(
                column.SuggestedTargetField,
                required,
                StringComparison.OrdinalIgnoreCase)));
        if (analysis.CanPreviewAutomatically)
        {
            analysis.Issues.RemoveAll(issue => issue.Code == "MAPPING_REQUIRED");
        }
        return analysis;
    }

    public async Task<PriceListImportPreviewResDTO> PreviewAsync(
        Guid priceListId,
        string fileName,
        Stream content,
        IReadOnlyDictionary<int, string>? columnMappings,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var priceList = await GetEditablePriceListAsync(priceListId, cancellationToken);
        var parsed = await parser.ParseAsync(content, fileName, columnMappings, cancellationToken);
        var evaluation = await EvaluateAsync(priceList, parsed.Rows, parsed.GlobalIssues, cancellationToken);
        var now = dateTimeProvider.Now;
        var duplicateFile = await unitOfWork.VPPContext.Set<PriceListImportBatch>()
            .AsNoTracking()
            .AnyAsync(batch => batch.PriceListId == priceListId
                               && batch.FileHash == parsed.FileHash
                               && batch.Status == PriceListImportBatchStatus.Completed,
                cancellationToken);

        var batch = new PriceListImportBatch
        {
            Id = Guid.NewGuid(),
            PriceListId = priceList.Id,
            SupplierId = priceList.SupplierId!.Value,
            OriginalFileName = SanitizeFileName(fileName),
            FileHash = parsed.FileHash,
            FileFormat = parsed.FileFormat,
            SchemaVersion = SchemaVersion,
            Status = evaluation.ErrorRows == 0
                ? PriceListImportBatchStatus.Ready
                : PriceListImportBatchStatus.Failed,
            TotalRows = evaluation.Rows.Count,
            AddedRows = evaluation.AddedRows,
            UpdatedRows = evaluation.UpdatedRows,
            UnchangedRows = evaluation.UnchangedRows,
            WarningRows = evaluation.WarningRows,
            ErrorRows = evaluation.ErrorRows,
            ResultMessage = evaluation.ErrorRows == 0
                ? "File đã sẵn sàng để nhập."
                : "Vui lòng xử lý các dòng lỗi trước khi nhập.",
            ColumnMappingsJson = JsonSerializer.Serialize(parsed.ColumnMappings, JsonOptions),
            UsedCustomMapping = parsed.ColumnMappings.Any(mapping => mapping.IsCustom),
            NormalizedRowsJson = JsonSerializer.Serialize(parsed.Rows, JsonOptions),
            IssuesJson = JsonSerializer.Serialize(
                evaluation.GlobalIssues.Concat(evaluation.Rows.SelectMany(row => row.Issues)),
                JsonOptions),
            CreatedByUserId = userId,
            CreatedAtUtc = now,
            UpdatedByUserId = userId,
            UpdatedAtUtc = now,
            IsDeleted = false
        };

        unitOfWork.VPPContext.Set<PriceListImportBatch>().Add(batch);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToPreview(batch, priceList, evaluation, parsed.ColumnMappings, duplicateFile);
    }

    public async Task<PriceListImportBatchResDTO> ConfirmAsync(
        Guid priceListId,
        Guid batchId,
        byte[]? rowVersion,
        int userId,
        CancellationToken cancellationToken = default)
    {
        await unitOfWork.BeginTransactionAsync();
        try
        {
            var batch = await unitOfWork.VPPContext.Set<PriceListImportBatch>()
                .FirstOrDefaultAsync(item => item.Id == batchId && item.PriceListId == priceListId && !item.IsDeleted,
                    cancellationToken)
                ?? throw new BusinessException("Không tìm thấy lần nhập.");

            if (batch.Status == PriceListImportBatchStatus.Completed)
            {
                await unitOfWork.RollbackAsync();
                return await ToBatchAsync(batch, cancellationToken);
            }

            if (batch.Status != PriceListImportBatchStatus.Ready)
            {
                throw new BusinessException("Lần nhập này không còn ở trạng thái chờ xác nhận.");
            }

            if (batch.RowVersion is { Length: > 0 })
            {
                if (rowVersion is not { Length: > 0 } || !batch.RowVersion.SequenceEqual(rowVersion))
                {
                    throw new ConflictException("Lần nhập đã thay đổi. Vui lòng xem trước lại.");
                }
                unitOfWork.VPPContext.Entry(batch).Property(item => item.RowVersion).OriginalValue = rowVersion;
            }

            var priceList = await GetEditablePriceListAsync(priceListId, cancellationToken);
            var parsedRows = JsonSerializer.Deserialize<List<PriceListImportParsedRow>>(
                batch.NormalizedRowsJson ?? "[]",
                JsonOptions) ?? [];
            var evaluation = await EvaluateAsync(priceList, parsedRows, [], cancellationToken);
            if (evaluation.ErrorRows > 0)
            {
                throw new ConflictException("Dữ liệu hệ thống đã thay đổi. Vui lòng xem trước file lại trước khi nhập.");
            }

            var existingIds = evaluation.Rows
                .Where(row => row.ExistingMappingId.HasValue)
                .Select(row => row.ExistingMappingId!.Value)
                .ToHashSet();
            var existingMappings = await unitOfWork.VPPContext.Set<SupplierProductMapping>()
                .Where(mapping => existingIds.Contains(mapping.Id))
                .ToDictionaryAsync(mapping => mapping.Id, cancellationToken);
            var now = dateTimeProvider.Now;

            foreach (var row in evaluation.Rows)
            {
                if (row.Action == PriceListImportAction.Unchanged || row.Action == PriceListImportAction.Error)
                {
                    continue;
                }

                if (row.Action == PriceListImportAction.Add)
                {
                    unitOfWork.VPPContext.Set<SupplierProductMapping>().Add(new SupplierProductMapping
                    {
                        Id = Guid.NewGuid(),
                        PriceListId = priceList.Id,
                        SupplierId = priceList.SupplierId!.Value,
                        VppItemId = row.VppItemId!.Value,
                        Price = row.UnitPrice,
                        NetPrice = row.UnitPrice,
                        VatRate = row.VatRate,
                        MinimumOrderQuantity = row.MinimumOrderQuantity,
                        LeadTimeDays = row.LeadTimeDays,
                        SupplierSku = row.SupplierSku,
                        IsDefault = row.IsDefault,
                        Description = row.Note,
                        CreatedByUserId = userId,
                        CreatedAtUtc = now,
                        UpdatedByUserId = userId,
                        UpdatedAtUtc = now,
                        IsDeleted = false
                    });
                    continue;
                }

                var mapping = existingMappings[row.ExistingMappingId!.Value];
                mapping.Price = row.UnitPrice;
                mapping.NetPrice = row.UnitPrice;
                mapping.VatRate = row.VatRate;
                mapping.MinimumOrderQuantity = row.MinimumOrderQuantity;
                mapping.LeadTimeDays = row.LeadTimeDays;
                mapping.SupplierSku = row.SupplierSku;
                mapping.IsDefault = row.IsDefault;
                mapping.Description = row.Note;
                mapping.UpdatedByUserId = userId;
                mapping.UpdatedAtUtc = now;
            }

            batch.Status = PriceListImportBatchStatus.Completed;
            batch.AddedRows = evaluation.AddedRows;
            batch.UpdatedRows = evaluation.UpdatedRows;
            batch.UnchangedRows = evaluation.UnchangedRows;
            batch.WarningRows = evaluation.WarningRows;
            batch.ErrorRows = 0;
            batch.CompletedAtUtc = now;
            batch.ConfirmedByUserId = userId;
            batch.ResultMessage = $"Đã nhập {evaluation.AddedRows + evaluation.UpdatedRows:N0} dòng giá.";
            batch.NormalizedRowsJson = null;
            batch.UpdatedByUserId = userId;
            batch.UpdatedAtUtc = now;

            await unitOfWork.CommitAsync();
            return await ToBatchAsync(batch, cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await unitOfWork.RollbackAsync();
            throw new ConflictException("Lần nhập đã được xử lý ở một phiên khác.", ex);
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<IReadOnlyList<PriceListImportBatchResDTO>> ListAsync(
        Guid priceListId,
        int top = 20,
        CancellationToken cancellationToken = default)
    {
        await EnsurePriceListExistsAsync(priceListId, cancellationToken);
        var batches = await unitOfWork.VPPContext.Set<PriceListImportBatch>()
            .AsNoTracking()
            .Where(batch => batch.PriceListId == priceListId && !batch.IsDeleted)
            .OrderByDescending(batch => batch.CreatedAtUtc)
            .Take(Math.Clamp(top, 1, 100))
            .ToListAsync(cancellationToken);
        var priceList = await unitOfWork.VPPContext.Set<PriceList>()
            .AsNoTracking()
            .Where(item => item.Id == priceListId)
            .Select(item => new
            {
                Name = item.PriceListName ?? item.PriceListCode ?? "–",
                SupplierName = item.Supplier == null ? "–" : item.Supplier.SupplierName ?? item.Supplier.SupplierShortName ?? "–"
            })
            .FirstAsync(cancellationToken);

        return batches.Select(batch => ToBatch(batch, priceList.Name, priceList.SupplierName)).ToArray();
    }

    public async Task<PriceListImportTemplateResult> BuildTemplateAsync(
        Guid? priceListId = null,
        CancellationToken cancellationToken = default)
    {
        var priceList = priceListId.HasValue
            ? await EnsurePriceListExistsAsync(priceListId.Value, cancellationToken)
            : null;
        var code = SanitizeFileName(priceList?.PriceListCode ?? "bang-gia");
        return new PriceListImportTemplateResult(
            PriceListWorkbookBuilder.BuildTemplate(),
            $"GTAS-VPP-Mau-nhap-bang-gia-{code}.xlsx",
            ExportFileContract.ExcelContentType);
    }

    private async Task<EvaluationResult> EvaluateAsync(
        PriceList priceList,
        IReadOnlyList<PriceListImportParsedRow> parsedRows,
        IReadOnlyList<PriceListImportIssueResDTO> globalIssues,
        CancellationToken cancellationToken)
    {
        var items = await unitOfWork.VPPContext.Set<VppItem>()
            .AsNoTracking()
            .Where(item => !item.IsDeleted)
            .Select(item => new { item.Id, item.VppCode, item.VppName })
            .ToListAsync(cancellationToken);
        var itemByCode = items
            .Where(item => !string.IsNullOrWhiteSpace(item.VppCode))
            .GroupBy(item => item.VppCode!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var mappings = await unitOfWork.VPPContext.Set<SupplierProductMapping>()
            .AsNoTracking()
            .Where(mapping => mapping.PriceListId == priceList.Id && !mapping.IsDeleted)
            .ToListAsync(cancellationToken);
        var mappingsByItem = mappings
            .GroupBy(mapping => mapping.VppItemId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var duplicateCodes = parsedRows
            .Where(row => !string.IsNullOrWhiteSpace(row.ItemCode))
            .GroupBy(row => row.ItemCode!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var evaluatedRows = new List<EvaluatedRow>(parsedRows.Count);
        foreach (var parsed in parsedRows)
        {
            var issues = parsed.Issues.Select(CloneIssue).ToList();
            if (!string.IsNullOrWhiteSpace(parsed.ItemCode) && duplicateCodes.Contains(parsed.ItemCode))
            {
                issues.Add(Issue(parsed.RowNumber, "Error", "DUPLICATE_ITEM_CODE", "Mã mặt hàng bị lặp trong file.", "ItemCode"));
            }

            if (string.IsNullOrWhiteSpace(parsed.ItemCode)
                || !itemByCode.TryGetValue(parsed.ItemCode.Trim(), out var item))
            {
                if (!string.IsNullOrWhiteSpace(parsed.ItemCode))
                {
                    issues.Add(Issue(parsed.RowNumber, "Error", "ITEM_NOT_FOUND", "Không tìm thấy mã mặt hàng trong hệ thống.", "ItemCode"));
                }
                evaluatedRows.Add(EvaluatedRow.Error(parsed, issues));
                continue;
            }

            mappingsByItem.TryGetValue(item.Id, out var existingRows);
            if (existingRows is { Length: > 1 })
            {
                issues.Add(Issue(parsed.RowNumber, "Error", "MULTIPLE_EXISTING_PRICES", "Mặt hàng đang có nhiều dòng giá trong bảng giá. Cần xử lý dữ liệu trước khi import.", "ItemCode"));
                evaluatedRows.Add(EvaluatedRow.Error(parsed, issues, item.Id, item.VppName));
                continue;
            }

            var existing = existingRows?.SingleOrDefault();
            if (!string.IsNullOrWhiteSpace(parsed.ItemName)
                && !string.Equals(parsed.ItemName.Trim(), item.VppName?.Trim(), StringComparison.CurrentCultureIgnoreCase))
            {
                issues.Add(Issue(parsed.RowNumber, "Warning", "ITEM_NAME_MISMATCH", "Tên mặt hàng trong file khác tên hệ thống; hệ thống vẫn dùng mã mặt hàng để đối chiếu.", "ItemName"));
            }

            if (!parsed.UnitPrice.HasValue)
            {
                evaluatedRows.Add(EvaluatedRow.Error(parsed, issues, item.Id, item.VppName));
                continue;
            }

            var unitPrice = parsed.UnitPrice.Value;
            var vatRate = parsed.VatRate ?? existing?.VatRate ?? 0m;
            var minimumOrderQuantity = parsed.MinimumOrderQuantity ?? existing?.MinimumOrderQuantity ?? 0m;
            var leadTimeDays = parsed.LeadTimeDays ?? existing?.LeadTimeDays ?? 0;
            var supplierSku = parsed.SupplierSku ?? existing?.SupplierSku;
            var isDefault = parsed.IsDefault ?? existing?.IsDefault ?? false;
            var note = parsed.Note ?? existing?.Description;
            var action = existing is null
                ? PriceListImportAction.Add
                : IsChanged(existing, unitPrice, vatRate, minimumOrderQuantity, leadTimeDays, supplierSku, isDefault, note)
                    ? PriceListImportAction.Update
                    : PriceListImportAction.Unchanged;
            if (issues.Any(issue => issue.Severity == "Error"))
            {
                action = PriceListImportAction.Error;
            }

            evaluatedRows.Add(new EvaluatedRow(
                parsed,
                item.Id,
                item.VppName,
                existing?.Id,
                unitPrice,
                vatRate,
                minimumOrderQuantity,
                leadTimeDays,
                supplierSku,
                isDefault,
                note,
                action,
                issues));
        }

        return new EvaluationResult(evaluatedRows, globalIssues.Select(CloneIssue).ToList());
    }

    private async Task<PriceList> GetEditablePriceListAsync(Guid id, CancellationToken cancellationToken)
    {
        var priceList = await EnsurePriceListExistsAsync(id, cancellationToken);
        if (priceList.IsDeleted || priceList.Status != PriceListStatus.Published)
        {
            throw new BusinessException("Bảng giá không còn hoạt động nên không thể nhập giá.");
        }
        if (!priceList.SupplierId.HasValue)
        {
            throw new BusinessException("Bảng giá chưa có nhà cung cấp.");
        }
        return priceList;
    }

    private async Task<PriceList> EnsurePriceListExistsAsync(Guid id, CancellationToken cancellationToken)
        => await unitOfWork.VPPContext.Set<PriceList>()
            .AsNoTracking()
            .Include(item => item.Supplier)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
           ?? throw new BusinessException("Không tìm thấy bảng giá.");

    private async Task<PriceListImportBatchResDTO> ToBatchAsync(
        PriceListImportBatch batch,
        CancellationToken cancellationToken)
    {
        var names = await unitOfWork.VPPContext.Set<PriceList>()
            .AsNoTracking()
            .Where(item => item.Id == batch.PriceListId)
            .Select(item => new
            {
                PriceList = item.PriceListName ?? item.PriceListCode ?? "–",
                Supplier = item.Supplier == null ? "–" : item.Supplier.SupplierName ?? item.Supplier.SupplierShortName ?? "–"
            })
            .FirstAsync(cancellationToken);
        return ToBatch(batch, names.PriceList, names.Supplier);
    }

    private static PriceListImportPreviewResDTO ToPreview(
        PriceListImportBatch batch,
        PriceList priceList,
        EvaluationResult evaluation,
        IReadOnlyList<PriceListImportColumnMappingResDTO> mappings,
        bool duplicateFile)
    {
        var result = new PriceListImportPreviewResDTO
        {
            Id = batch.Id,
            PriceListId = batch.PriceListId,
            SupplierId = batch.SupplierId,
            PriceListName = priceList.PriceListName ?? priceList.PriceListCode ?? "–",
            SupplierName = priceList.Supplier?.SupplierName ?? priceList.Supplier?.SupplierShortName ?? "–",
            FileName = batch.OriginalFileName,
            FileHash = batch.FileHash,
            FileFormat = batch.FileFormat,
            Status = batch.Status.ToString(),
            TotalRows = batch.TotalRows,
            AddedRows = batch.AddedRows,
            UpdatedRows = batch.UpdatedRows,
            UnchangedRows = batch.UnchangedRows,
            WarningRows = batch.WarningRows,
            ErrorRows = batch.ErrorRows,
            UsedCustomMapping = batch.UsedCustomMapping,
            DuplicateFileWarning = duplicateFile,
            CreatedAtUtc = batch.CreatedAtUtc,
            ResultMessage = batch.ResultMessage,
            RowVersion = batch.RowVersion,
            ColumnMappings = mappings.ToList(),
            Issues = evaluation.GlobalIssues,
            Rows = evaluation.Rows.Select(ToPreviewRow).ToList()
        };
        return result;
    }

    private static PriceListImportBatchResDTO ToBatch(
        PriceListImportBatch batch,
        string priceListName,
        string supplierName)
        => new()
        {
            Id = batch.Id,
            PriceListId = batch.PriceListId,
            SupplierId = batch.SupplierId,
            PriceListName = priceListName,
            SupplierName = supplierName,
            FileName = batch.OriginalFileName,
            FileHash = batch.FileHash,
            FileFormat = batch.FileFormat,
            Status = batch.Status.ToString(),
            TotalRows = batch.TotalRows,
            AddedRows = batch.AddedRows,
            UpdatedRows = batch.UpdatedRows,
            UnchangedRows = batch.UnchangedRows,
            WarningRows = batch.WarningRows,
            ErrorRows = batch.ErrorRows,
            UsedCustomMapping = batch.UsedCustomMapping,
            CreatedAtUtc = batch.CreatedAtUtc,
            CompletedAtUtc = batch.CompletedAtUtc,
            ResultMessage = batch.ResultMessage,
            RowVersion = batch.RowVersion
        };

    private static PriceListImportRowResDTO ToPreviewRow(EvaluatedRow row)
        => new()
        {
            RowNumber = row.Source.RowNumber,
            ItemCode = row.Source.ItemCode,
            ItemName = row.Source.ItemName,
            MatchedItemName = row.MatchedItemName,
            SupplierSku = row.SupplierSku,
            UnitPrice = row.Source.UnitPrice,
            VatRate = row.Source.VatRate,
            MinimumOrderQuantity = row.Source.MinimumOrderQuantity,
            LeadTimeDays = row.Source.LeadTimeDays,
            IsDefault = row.Source.IsDefault,
            Note = row.Source.Note,
            Action = row.Action.ToString(),
            Issues = row.Issues
        };

    private static bool IsChanged(
        SupplierProductMapping existing,
        decimal unitPrice,
        decimal vatRate,
        decimal minimumOrderQuantity,
        int leadTimeDays,
        string? supplierSku,
        bool isDefault,
        string? note)
        => existing.Price != unitPrice
           || existing.NetPrice != unitPrice
           || existing.VatRate != vatRate
           || existing.MinimumOrderQuantity != minimumOrderQuantity
           || existing.LeadTimeDays != leadTimeDays
           || !string.Equals(existing.SupplierSku, supplierSku, StringComparison.Ordinal)
           || existing.IsDefault != isDefault
           || !string.Equals(existing.Description, note, StringComparison.Ordinal);

    private static string SanitizeFileName(string value)
    {
        var fileName = Path.GetFileName(value ?? string.Empty);
        foreach (var character in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(character, '-');
        }
        return string.IsNullOrWhiteSpace(fileName) ? "bang-gia" : fileName[..Math.Min(fileName.Length, 260)];
    }

    private static PriceListImportIssueResDTO CloneIssue(PriceListImportIssueResDTO issue)
        => new()
        {
            RowNumber = issue.RowNumber,
            Severity = issue.Severity,
            Code = issue.Code,
            Message = issue.Message,
            Column = issue.Column
        };

    private static PriceListImportIssueResDTO Issue(int row, string severity, string code, string message, string? column)
        => new() { RowNumber = row, Severity = severity, Code = code, Message = message, Column = column };

    private enum PriceListImportAction
    {
        Add,
        Update,
        Unchanged,
        Error
    }

    private sealed record EvaluatedRow(
        PriceListImportParsedRow Source,
        Guid? VppItemId,
        string? MatchedItemName,
        Guid? ExistingMappingId,
        decimal UnitPrice,
        decimal VatRate,
        decimal MinimumOrderQuantity,
        int LeadTimeDays,
        string? SupplierSku,
        bool IsDefault,
        string? Note,
        PriceListImportAction Action,
        List<PriceListImportIssueResDTO> Issues)
    {
        public static EvaluatedRow Error(
            PriceListImportParsedRow source,
            List<PriceListImportIssueResDTO> issues,
            Guid? itemId = null,
            string? matchedItemName = null)
            => new(source, itemId, matchedItemName, null, 0, 0, 0, 0, source.SupplierSku, source.IsDefault ?? false, source.Note, PriceListImportAction.Error, issues);
    }

    private sealed record EvaluationResult(
        List<EvaluatedRow> Rows,
        List<PriceListImportIssueResDTO> GlobalIssues)
    {
        public int AddedRows => Rows.Count(row => row.Action == PriceListImportAction.Add);
        public int UpdatedRows => Rows.Count(row => row.Action == PriceListImportAction.Update);
        public int UnchangedRows => Rows.Count(row => row.Action == PriceListImportAction.Unchanged);
        public int WarningRows => Rows.Count(row => row.Issues.Any(issue => issue.Severity == "Warning"));
        public int ErrorRows => Rows.Count(row => row.Action == PriceListImportAction.Error)
                                + (GlobalIssues.Any(issue => issue.Severity == "Error") ? 1 : 0);
    }
}
