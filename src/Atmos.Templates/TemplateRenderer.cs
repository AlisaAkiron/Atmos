using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace Atmos.Templates;

public class TemplateRenderer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILoggerFactory _loggerFactory;

    public TemplateRenderer(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
    {
        _serviceProvider = serviceProvider;
        _loggerFactory = loggerFactory;
    }

    public async Task<string> RenderTemplateAsync<TComposite, TViewModel>(TViewModel model)
        where TComposite : IComponentComposite<TViewModel>
        where TViewModel : class
    {
        await using var htmlRenderer = new HtmlRenderer(_serviceProvider, _loggerFactory);

        var html = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                { "Model", model }
            });

            var output = await htmlRenderer.RenderComponentAsync(TComposite.ComponentType, parameters);
            return output.ToHtmlString();
        });

        return html;
    }
}
