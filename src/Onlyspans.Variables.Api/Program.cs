using Onlyspans.Variables.Api.Startup;

try
{
    await WebApplication
        .CreateBuilder(args)
        .ConfigureServices()
        .Build()
        .Configure()
        .RunAsync();
}
catch (Exception ex)
    when (ex is not HostAbortedException)
{
    Console.WriteLine($"ERR : {ex}");
}
