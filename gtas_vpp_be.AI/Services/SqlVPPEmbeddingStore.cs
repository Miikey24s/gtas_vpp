using System.Diagnostics;
using gtas_vpp_be.AI.Data;
using gtas_vpp_be.AI.Helpers;
using gtas_vpp_be.Model.AI;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.AI;
using gtas_vpp_be.Service.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace gtas_vpp_be.AI.Services;

public class SqlVPPEmbeddingStore : IVPPEmbeddingStore
{
    private readonly AIDbContext _db;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly IGenericRepository<L04_VPP> _vppRepository;
    private readonly ILogger<SqlVPPEmbeddingStore> _logger;

    public SqlVPPEmbeddingStore(
        AIDbContext db,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IGenericRepository<L04_VPP> vppRepository,
        ILogger<SqlVPPEmbeddingStore> logger)
    {
        _db = db;
        _embeddingGenerator = embeddingGenerator;
        _vppRepository = vppRepository;
        _logger = logger;
    }

    public async Task<List<VPPEmbeddingSearchResult>> SearchSimilarAsync(float[] queryVector, int topK, CancellationToken ct = default)
    {
        var embeddings = await _db.VPPEmbeddings
            .AsNoTracking()
            .ToListAsync(ct);

        var ranked = embeddings
            .Select(x => new
            {
                Embedding = x,
                Similarity = VectorMath.CosineSimilarity(queryVector, VectorMath.BytesToFloatArray(x.EmbeddingVector))
            })
            .OrderByDescending(x => x.Similarity)
            .Take(topK)
            .ToList();

        var vppIds = ranked.Select(x => x.Embedding.VPPId).ToHashSet();
        var vpps = await _vppRepository.ReadAsync(
            x => vppIds.Contains(x.Id),
            q => q.Include(x => x.VPPCategory).Include(x => x.UOM));
        var vppById = vpps.ToDictionary(x => x.Id);

        return ranked
            .Select(x =>
            {
                vppById.TryGetValue(x.Embedding.VPPId, out var vpp);

                return new VPPEmbeddingSearchResult
                {
                    VPPId = x.Embedding.VPPId,
                    VPPCode = vpp?.VPPCode ?? string.Empty,
                    VPPName = vpp?.VPPName ?? string.Empty,
                    CategoryName = vpp?.VPPCategory?.VPPCategoryName ?? string.Empty,
                    UOMName = vpp?.UOM?.ClassDetailValue ?? string.Empty,
                    EmbeddingText = x.Embedding.EmbeddingText,
                    SimilarityScore = x.Similarity
                };
            })
            .ToList();
    }

    public async Task UpsertEmbeddingAsync(Guid vppId, string text, float[] vector, string modelName, CancellationToken ct = default)
    {
        var bytes = VectorMath.FloatArrayToBytes(vector);
        var existing = await _db.VPPEmbeddings.SingleOrDefaultAsync(x => x.VPPId == vppId, ct);

        if (existing is null)
        {
            await _db.VPPEmbeddings.AddAsync(new AI_VPPEmbedding
            {
                VPPId = vppId,
                EmbeddingText = text,
                EmbeddingVector = bytes,
                ModelName = modelName,
                VectorDimension = vector.Length
            }, ct);
        }
        else
        {
            existing.EmbeddingText = text;
            existing.EmbeddingVector = bytes;
            existing.ModelName = modelName;
            existing.VectorDimension = vector.Length;
            existing.UpdatedDate = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task RebuildAllAsync(CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var vpps = await _vppRepository.ReadAsync(
            x => !x.IsDeleted,
            q => q.Include(x => x.VPPCategory).Include(x => x.UOM));

        var count = 0;
        foreach (var vpp in vpps)
        {
            ct.ThrowIfCancellationRequested();

            var text = BuildEmbeddingText(vpp);
            var embedding = await _embeddingGenerator.GenerateAsync(text, options: null, cancellationToken: ct);
            await UpsertEmbeddingAsync(vpp.Id, text, embedding.Vector.ToArray(), "nomic-embed-text", ct);
            count++;
        }

        stopwatch.Stop();
        _logger.LogInformation("AI {Action} completed in {ElapsedMs}ms for {Count} VPP embeddings",
            "RebuildEmbeddings",
            stopwatch.ElapsedMilliseconds,
            count);
    }

    public async Task<bool> HasEmbeddingsAsync(CancellationToken ct = default)
    {
        return await _db.VPPEmbeddings.AnyAsync(ct);
    }

    private static string BuildEmbeddingText(L04_VPP vpp)
    {
        var categoryName = vpp.VPPCategory?.VPPCategoryName ?? string.Empty;
        var uomName = vpp.UOM?.ClassDetailValue ?? string.Empty;

        return $"{vpp.VPPName} - Loại: {categoryName} - Đơn vị: {uomName} - Mô tả: {vpp.Description}";
    }
}
