using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Onlyspans.Variables.Api.Data.Entities;
using Onlyspans.Variables.Api.Data.Records;
using Onlyspans.Variables.Api.Features.VariableSets;
using Onlyspans.Variables.Api.Tests.Helpers;

namespace Onlyspans.Variables.Api.Tests.Features;

public class AddVariableToSetHandlerTests
{
    [Fact]
    public async Task Handle_ValidRequest_AddsVariableToSet()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var setId = Guid.NewGuid();
        var variableSet = new VariableSet { Id = setId, Name = "Test Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync();

        var request = new CreateVariableRequest(
            Key: "SET_VAR",
            Value: "set-value");

        var command = new AddVariableToSet(setId, request);
        var handler = new AddVariableToSetHandler(db, NullLogger<AddVariableToSetHandler>.Instance);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Key.Should().Be("SET_VAR");
        result.Value.Should().Be("set-value");
        result.VariableSetId.Should().Be(setId);
        result.ProjectId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithEnvironmentId_CreatesEnvironmentScopedVariable()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var setId = Guid.NewGuid();
        var envId = Guid.NewGuid();
        var variableSet = new VariableSet { Id = setId, Name = "Test Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync();

        var request = new CreateVariableRequest(
            Key: "ENV_VAR",
            Value: "env-value",
            EnvironmentId: envId);

        var command = new AddVariableToSet(setId, request);
        var handler = new AddVariableToSetHandler(db, NullLogger<AddVariableToSetHandler>.Instance);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.EnvironmentId.Should().Be(envId);
        result.VariableSetId.Should().Be(setId);
    }

    [Fact]
    public async Task Handle_PersistsToDatabase_CanBeRetrieved()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var setId = Guid.NewGuid();
        var variableSet = new VariableSet { Id = setId, Name = "Test Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync();

        var request = new CreateVariableRequest(Key: "PERSIST_TEST", Value: "value");
        var command = new AddVariableToSet(setId, request);
        var handler = new AddVariableToSetHandler(db, NullLogger<AddVariableToSetHandler>.Instance);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        var savedVariable = await db.Variables.FirstOrDefaultAsync(v => v.Id == result.Id);
        savedVariable.Should().NotBeNull();
        savedVariable!.Key.Should().Be("PERSIST_TEST");
        savedVariable.VariableSetId.Should().Be(setId);
        savedVariable.ProjectId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_MultipleVariables_AddsAllToSet()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var setId = Guid.NewGuid();
        var variableSet = new VariableSet { Id = setId, Name = "Test Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync();

        var handler = new AddVariableToSetHandler(db, NullLogger<AddVariableToSetHandler>.Instance);

        var request1 = new CreateVariableRequest(Key: "VAR1", Value: "value1");
        var request2 = new CreateVariableRequest(Key: "VAR2", Value: "value2");
        var request3 = new CreateVariableRequest(Key: "VAR3", Value: "value3");

        // Act
        await handler.Handle(new AddVariableToSet(setId, request1), CancellationToken.None);
        await handler.Handle(new AddVariableToSet(setId, request2), CancellationToken.None);
        await handler.Handle(new AddVariableToSet(setId, request3), CancellationToken.None);

        // Assert
        var allVariables = await db.Variables.Where(v => v.VariableSetId == setId).ToListAsync();
        allVariables.Should().HaveCount(3);
        allVariables.Should().OnlyContain(v => v.VariableSetId == setId);
    }
}
