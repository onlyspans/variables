namespace Onlyspans.Variables.Api.Startup;

public static partial class Startup
{
    private static WebApplication UseExceptionHandler(this WebApplication app)
    {
        app.UseExceptionHandler(exceptionHandlerApp =>
        {
            exceptionHandlerApp.Run(async context =>
            {
                var exceptionHandlerFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
                var exception = exceptionHandlerFeature?.Error;

                switch (exception)
                {
                    case InvalidOperationException:
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        context.Response.ContentType = "application/problem+json";
                        await context.Response.WriteAsJsonAsync(new
                        {
                            type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                            title = "Not Found",
                            status = 404,
                            detail = exception.Message
                        });
                        break;
                    default:
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        context.Response.ContentType = "application/problem+json";
                        await context.Response.WriteAsJsonAsync(new
                        {
                            type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                            title = "Internal Server Error",
                            status = 500,
                            detail = app.Environment.IsDevelopment() ? exception?.Message : "An error occurred"
                        });
                        break;
                }
            });
        });

        return app;
    }
}
