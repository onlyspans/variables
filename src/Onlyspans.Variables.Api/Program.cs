using Onlyspans.Variables.Api;
using Onlyspans.Variables.Api.Endpoints;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddSerilog();

builder.Services
    .AddDatabase(builder.Configuration)
    .AddGrpcServices()
    .AddFluentValidation()
    .AddHealthz(builder.Configuration)
    .AddMediatorServices();

var app = builder.Build();

app.UseHealthz();
app.UseGrpcServices();

app.MapVariablesEndpoints();
app.MapVariableSetsEndpoints();
app.MapProjectVariableSetsEndpoints();

app.MapGet("/", () => "Onlyspans.Variables.Api");

app.Run();
