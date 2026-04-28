using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Serilog;
using System.Text.RegularExpressions;

namespace gtas_vpp_be.Service.Services
{
    /// <summary>
    /// Helper class to read and execute SQL files that contain GO batch separators.
    /// EF Core's ExecuteSqlRaw cannot handle GO statements, so this class splits
    /// the SQL content into individual batches and executes them sequentially.
    /// </summary>
    public static class SqlBatchExecutor
    {
        /// <summary>
        /// Reads a SQL file from the application's base directory and executes it.
        /// Handles GO separators by splitting into batches.
        /// Strips USE [database] statements since the connection string determines the DB.
        /// </summary>
        /// <param name="database">The DatabaseFacade from DbContext</param>
        /// <param name="relativePath">Path relative to AppContext.BaseDirectory (e.g., "Helpers/SQL/01_Views.sql")</param>
        public static async Task ExecuteSqlFileAsync(DatabaseFacade database, string relativePath)
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, relativePath);

            if (!File.Exists(filePath))
            {
                Log.Warning("[SqlBatchExecutor] File not found: {FilePath}", filePath);
                return;
            }

            Log.Information("[SqlBatchExecutor] Executing SQL file: {FileName}", Path.GetFileName(filePath));

            var sqlContent = await File.ReadAllTextAsync(filePath);
            var batches = SplitIntoBatches(sqlContent);

            int batchCount = 0;
            foreach (var batch in batches)
            {
                if (!string.IsNullOrWhiteSpace(batch))
                {
                    try
                    {
                        await database.ExecuteSqlRawAsync(batch);
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
        /// Splits SQL content by GO batch separator.
        /// Also strips USE [database] statements for portability.
        /// </summary>
        private static IEnumerable<string> SplitIntoBatches(string sqlContent)
        {
            // Remove USE [DatabaseName] statements (we rely on connection string)
            sqlContent = Regex.Replace(
                sqlContent,
                @"^\s*USE\s+\[?[\w]+\]?\s*;?\s*$",
                string.Empty,
                RegexOptions.Multiline | RegexOptions.IgnoreCase);

            // Split by GO on its own line (standard SSMS batch separator)
            var batches = Regex.Split(
                sqlContent,
                @"^\s*GO\s*$",
                RegexOptions.Multiline | RegexOptions.IgnoreCase);

            return batches.Where(b => !string.IsNullOrWhiteSpace(b));
        }
    }
}
