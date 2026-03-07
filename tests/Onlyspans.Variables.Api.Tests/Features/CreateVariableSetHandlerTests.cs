using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Onlyspans.Variables.Api.Data.Records;
using Onlyspans.Variables.Api.Features.VariableSets;
using Onlyspans.Variables.Api.Tests.Helpers;

namespace Onlyspans.Variables.Api.Tests.Features;

public class CreateVariableSetHandlerTests
{
    [Fact]
    public async Task Handle_ValidRequest_CreatesVariableSetSuccessfully()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var request = new CreateVariableSetRequest(
            Name: "Production Config",
            Description: "Production environment configuration");

        var command = new CreateVariableSet(request);
        var handler = new CreateVariableSetHandler(db, NullLogger<CreateVariableSetHandler>.Instance);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("Production Config");
        result.Description.Should().Be("Production environment configuration");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Handle_WithoutDescription_CreatesSuccessfully()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var request = new CreateVariableSetRequest(Name: "Simple Set");
        var command = new CreateVariableSet(request);
        var handler = new CreateVariableSetHandler(db, NullLogger<CreateVariableSetHandler>.Instance);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Name.Should().Be("Simple Set");
        result.Description.Should().BeNull();
    }

    [Fact]
    public async Task Handle_PersistsToDatabase_CanBeRetrieved()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var request = new CreateVariableSetRequest(
            Name: "Test Set",
            Description: "Test description");

        var command = new CreateVariableSet(request);
        var handler = new CreateVariableSetHandler(db, NullLogger<CreateVariableSetHandler>.Instance);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        var savedSet = await db.VariableSets.FirstOrDefaultAsync(vs => vs.Id == result.Id);
        savedSet.Should().NotBeNull();
        savedSet!.Name.Should().Be("Test Set");
        savedSet.Description.Should().Be("Test description");
    }

    [Fact]
    public async Task Handle_MultipleVariableSets_CreatesAllSuccessfully()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();
        var handler = new CreateVariableSetHandler(db, NullLogger<CreateVariableSetHandler>.Instance);

        var request1 = new CreateVariableSetRequest(Name: "Set 1", Description: "First");
        var request2 = new CreateVariableSetRequest(Name: "Set 2", Description: "Second");
        var request3 = new CreateVariableSetRequest(Name: "Set 3");

        // Act
        var result1 = await handler.Handle(new CreateVariableSet(request1), CancellationToken.None);
        var result2 = await handler.Handle(new CreateVariableSet(request2), CancellationToken.None);
        var result3 = await handler.Handle(new CreateVariableSet(request3), CancellationToken.None);

        // Assert
        var allSets = await db.VariableSets.ToListAsync();
        allSets.Should().HaveCount(3);
        allSets.Should().Contain(s => s.Name == "Set 1");
        allSets.Should().Contain(s => s.Name == "Set 2");
        allSets.Should().Contain(s => s.Name == "Set 3");
    }
}
