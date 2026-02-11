using Microsoft.EntityFrameworkCore;
using Onlyspans.Variables.Api.Data.Contexts;

namespace Onlyspans.Variables.Api.Tests.Helpers;

public static class MockDbContextFactory
{
    public static ApplicationDbContext CreateInMemoryDbContext(string databaseName = "")
    {
        if (string.IsNullOrEmpty(databaseName))
        {
            databaseName = Guid.NewGuid().ToString();
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new ApplicationDbContext(options);
    }
}
