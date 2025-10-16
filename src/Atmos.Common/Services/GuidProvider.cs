using Atmos.Common.Abstract;

namespace Atmos.Common.Services;

public class GuidProvider : IGuidProvider
{
    private readonly TimeProvider _timeProvider;

    public GuidProvider(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public Guid Create()
    {
        return Guid.CreateVersion7(_timeProvider.GetUtcNow());
    }
}
