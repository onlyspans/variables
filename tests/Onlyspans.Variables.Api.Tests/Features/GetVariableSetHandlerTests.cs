using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Onlyspans.Variables.Api.Data.Entities;
using Onlyspans.Variables.Api.Features.VariableSets;
using Onlyspans.Variables.Api.Tests.Helpers;

namespace Onlyspans.Variables.Api.Tests.Features;

public class GetVariableSetHandlerTests
{
    [Fact]
    public async Task Handle_ExistingVariableSet_ReturnsDetailWithVariables()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var setId = Guid.NewGuid();
        var variableSet = new VariableSet { Id = setId, Name = "Test Set", Description = "Test description", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        var var1 = new Variable { Id = Guid.NewGuid(), Key = "VAR1", Value = "value1", VariableSetId = setId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var var2 = new Variable { Id = Guid.NewGuid(), Key = "VAR2", Value = "value2", VariableSetId = setId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        db.VariableSets.Add(variableSet);
        db.Variables.AddRange(var1, var2);
        await db.SaveChangesAsync();

        var query = new GetVariableSet(setId);
        var handler = new GetVariableSetHandler(db, NullLogger<GetVariableSetHandler>.Instance);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(setId);
        result.Name.Should().Be("Test Set");
        result.Description.Should().Be("Test description");
        result.Variables.Should().HaveCount(2);
        result.Variables.Should().Contain(v => v.Key == "VAR1");
        result.Variables.Should().Contain(v => v.Key == "VAR2");
    }

    [Fact]
    public async Task Handle_VariableSetWithNoVariables_ReturnsEmptyVariablesList()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variableSet = new VariableSet { Id = Guid.NewGuid(), Name = "Empty Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync();

        var query = new GetVariableSet(variableSet.Id);
        var handler = new GetVariableSetHandler(db, NullLogger<GetVariableSetHandler>.Instance);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Variables.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_VariableSetNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var query = new GetVariableSet(nonExistentId);
        var handler = new GetVariableSetHandler(db, NullLogger<GetVariableSetHandler>.Instance);

        // Act
        Func<Task> act = async () => await handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Variable set {nonExistentId} not found");
    }

    [Fact]
    public async Task Handle_VariablesIncludeEnvironmentScoping()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var setId = Guid.NewGuid();
        var envId = Guid.NewGuid();
        var variableSet = new VariableSet { Id = setId, Name = "Scoped Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        var unscopedVar = new Variable { Id = Guid.NewGuid(), Key = "UNSCOPED", Value = "value", VariableSetId = setId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var scopedVar = new Variable { Id = Guid.NewGuid(), Key = "SCOPED", Value = "env-value", EnvironmentId = envId, VariableSetId = setId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        db.VariableSets.Add(variableSet);
        db.Variables.AddRange(unscopedVar, scopedVar);
        await db.SaveChangesAsync();

        var query = new GetVariableSet(setId);
        var handler = new GetVariableSetHandler(db, NullLogger<GetVariableSetHandler>.Instance);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Variables.Should().HaveCount(2);
        result.Variables.Should().Contain(v => v.Key == "UNSCOPED" && v.EnvironmentId == null);
        result.Variables.Should().Contain(v => v.Key == "SCOPED" && v.EnvironmentId == envId);
    }
}
