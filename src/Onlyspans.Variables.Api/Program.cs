using Onlyspans.Variables.Api;
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

app.MapGet("/", () => "Onlyspans.Variables.Api");

app.Run();
