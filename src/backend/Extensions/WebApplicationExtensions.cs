using backend.Endpoints.Documents;

namespace backend.Extensions;

internal static class WebApplicationExtensions
{
    public static WebApplication UsePresentation(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            app.UseExceptionHandler();
        }

        app.UseHttpsRedirection();
        app.UseCors(ServiceCollectionExtensions.FrontendCorsPolicy);

        app.MapDocumentEndpoint();

        return app;
    }
}
