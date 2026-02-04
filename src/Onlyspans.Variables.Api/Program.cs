using Onlyspans.Variables.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDatabase(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => "Onlyspans.Variables.Api");

app.Run();
