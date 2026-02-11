using Onlyspans.Variables.Api;
using Onlyspans.Variables.Api.Data.Exceptions;
using Onlyspans.Variables.Api.Endpoints;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddSerilog();

builder.Services
    .AddDatabase(builder.Configuration)
    .AddGrpcServices(builder.Environment)
    .AddFluentValidation()
    .AddHealthz(builder.Configuration)
    .AddMediatorServices();

var app = builder.Build();

// Add global exception handler for business logic exceptions
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exceptionHandlerFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var exception = exceptionHandlerFeature?.Error;

        if (exception is InvalidOperationException)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                title = "Not Found",
                status = 404,
                detail = exception.Message
            });
        }
        else if (exception is ConflictException)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                title = "Conflict",
                status = 409,
                detail = exception.Message
            });
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                title = "Internal Server Error",
                status = 500,
                detail = app.Environment.IsDevelopment() ? exception?.Message : "An error occurred"
            });
        }
    });
});

app.UseHealthz();
app.UseGrpcServices();

app.MapVariablesEndpoints();
app.MapVariableSetsEndpoints();
app.MapProjectVariableSetsEndpoints();

app.MapGet("/", () => "Onlyspans.Variables.Api");

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
