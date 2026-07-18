using DesignDnaStudio.Engine;

namespace DesignDnaStudio.Web.Services.Assessment;

public interface IDesignProfileNarrativeService
{
    Task<string> CreateAsync(ProfileSnapshot profile, CancellationToken cancellationToken = default);
}
