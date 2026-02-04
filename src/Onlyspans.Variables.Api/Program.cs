var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet("/", () => "Onlyspans.Variables.Api");
app.Run();
