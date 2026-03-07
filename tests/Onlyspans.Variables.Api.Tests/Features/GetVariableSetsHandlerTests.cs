using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Onlyspans.Variables.Api.Data.Entities;
using Onlyspans.Variables.Api.Features.VariableSets;
using Onlyspans.Variables.Api.Tests.Helpers;

namespace Onlyspans.Variables.Api.Tests.Features;

public class GetVariableSetsHandlerTests
{
    [Fact]
    public async Task Handle_MultipleVariableSets_ReturnsAll()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var set1 = new VariableSet { Id = Guid.NewGuid(), Name = "Set 1", Description = "First", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var set2 = new VariableSet { Id = Guid.NewGuid(), Name = "Set 2", Description = "Second", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var set3 = new VariableSet { Id = Guid.NewGuid(), Name = "Set 3", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        db.VariableSets.AddRange(set1, set2, set3);
        await db.SaveChangesAsync();

        var query = new GetVariableSets();
        var handler = new GetVariableSetsHandler(db, NullLogger<GetVariableSetsHandler>.Instance);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(s => s.Name == "Set 1");
        result.Should().Contain(s => s.Name == "Set 2");
        result.Should().Contain(s => s.Name == "Set 3");
    }

    [Fact]
    public async Task Handle_NoVariableSets_ReturnsEmptyList()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var query = new GetVariableSets();
        var handler = new GetVariableSetsHandler(db, NullLogger<GetVariableSetsHandler>.Instance);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsCompleteSetInformation()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();
        var now = DateTime.UtcNow;

        var variableSet = new VariableSet { Id = Guid.NewGuid(), Name = "Complete Set", Description = "Full description", CreatedAt = now, UpdatedAt = now };

        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync();

        var query = new GetVariableSets();
        var handler = new GetVariableSetsHandler(db, NullLogger<GetVariableSetsHandler>.Instance);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        var response = result[0];
        response.Id.Should().Be(variableSet.Id);
        response.Name.Should().Be("Complete Set");
        response.Description.Should().Be("Full description");
        response.CreatedAt.Should().Be(now);
        response.UpdatedAt.Should().Be(now);
    }
}
