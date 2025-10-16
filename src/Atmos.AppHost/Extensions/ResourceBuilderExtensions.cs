namespace Atmos.AppHost.Extensions;

public static class ResourceBuilderExtensions
{
    public static async Task<string> GetStringAsync(this IResourceBuilder<ParameterResource> parameterResourceBuilder)
    {
        var value = await parameterResourceBuilder.Resource.GetValueAsync(CancellationToken.None);
        return value ?? string.Empty;
    }

    public static async Task<bool> GetBoolAsync(this IResourceBuilder<ParameterResource> parameterResourceBuilder)
    {
        var value = await parameterResourceBuilder.Resource.GetValueAsync(CancellationToken.None);
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        return bool.Parse(value);
    }
}
