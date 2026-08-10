using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Serilog;
using System.Text.RegularExpressions;

namespace gtas_vpp_be.Service.Services
{
    /// <summary>
    /// Helper đọc và thực thi file SQL có batch separator GO.
    /// ExecuteSqlRaw của EF Core không xử lý được lệnh GO, nên class này tách nội dung
    /// SQL thành từng batch rồi thực thi tuần tự.
    /// </summary>
    public static class SqlBatchExecutor
    {
        /// <summary>
        /// Đọc file SQL từ thư mục base của ứng dụng và thực thi.
        /// Xử lý separator GO bằng cách tách thành batch.
        /// Escape { và } để EF Core không hiểu chúng là placeholder tham số.
        /// </summary>
        public static async Task ExecuteSqlFileAsync(DatabaseFacade database, string relativePath)
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, relativePath);

            EnsureFileExists(relativePath, filePath);

            Log.Information("[SqlBatchExecutor] Executing SQL file: {FileName}", Path.GetFileName(filePath));

            var sqlContent = await File.ReadAllTextAsync(filePath);
            var batches = SplitIntoBatches(sqlContent);
            EnsureContainsRequiredBatch(relativePath, batches);

            int batchCount = 0;
            foreach (var batch in batches)
            {
                if (!string.IsNullOrWhiteSpace(batch))
                {
                    try
                    {
                        // Escape { và } để EF Core không hiểu chúng là format parameter.
                        var safeBatch = batch.Replace("{", "{{").Replace("}", "}}");
                        await database.ExecuteSqlRawAsync(safeBatch);
                        batchCount++;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "[SqlBatchExecutor] Error executing batch #{BatchNum} in {FileName}",
                            batchCount + 1, Path.GetFileName(filePath));
                        throw;
                    }
                }
            }

            Log.Information("[SqlBatchExecutor] Completed {FileName}: {BatchCount} batches executed",
                Path.GetFileName(filePath), batchCount);
        }

        /// <summary>
        /// Tách nội dung SQL theo batch separator GO.
        /// LƯU Ý: Không loại câu lệnh USE vì một số script cần nhắm đến database khác.
        /// </summary>
        public static IReadOnlyList<string> ReadBatches(string relativePath)
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, relativePath);
            EnsureFileExists(relativePath, filePath);
            var batches = SplitIntoBatches(File.ReadAllText(filePath));
            EnsureContainsRequiredBatch(relativePath, batches);
            return batches;
        }

        public static IReadOnlyList<string> SplitIntoBatches(string sqlContent)
        {
            ArgumentNullException.ThrowIfNull(sqlContent);

            // Tách theo GO nằm riêng một dòng, đúng chuẩn batch separator của SSMS.
            var batches = Regex.Split(
                sqlContent,
                @"^\s*GO\s*$",
                RegexOptions.Multiline | RegexOptions.IgnoreCase);

            return batches
                .Where(b => !string.IsNullOrWhiteSpace(b))
                .ToArray();
        }

        private static void EnsureFileExists(string relativePath, string filePath)
        {
            if (File.Exists(filePath))
            {
                return;
            }

            var exception = new FileNotFoundException(
                $"Required SQL seed file was not found: {relativePath}",
                filePath);
            Log.Error(exception, "[SqlBatchExecutor] Required SQL file is missing: {FilePath}", filePath);
            throw exception;
        }

        private static void EnsureContainsRequiredBatch(
            string relativePath,
            IReadOnlyCollection<string> batches)
        {
            if (batches.Count > 0)
            {
                return;
            }

            var exception = new InvalidDataException(
                $"Required SQL seed file was empty or whitespace-only: {relativePath}");
            Log.Error(exception, "[SqlBatchExecutor] Required SQL file contains no executable batch: {RelativePath}",
                relativePath);
            throw exception;
        }
    }
}
