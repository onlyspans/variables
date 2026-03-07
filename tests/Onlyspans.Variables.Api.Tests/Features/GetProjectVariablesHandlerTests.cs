using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Onlyspans.Variables.Api.Data.Entities;
using Onlyspans.Variables.Api.Features.Variables;
using Onlyspans.Variables.Api.Tests.Helpers;

namespace Onlyspans.Variables.Api.Tests.Features;

public class GetProjectVariablesHandlerTests
{
    [Fact]
    public async Task Handle_ProjectWithVariables_ReturnsAllProjectVariables()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var var1 = new Variable { Id = Guid.NewGuid(), Key = "VAR1", Value = "value1", ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var var2 = new Variable { Id = Guid.NewGuid(), Key = "VAR2", Value = "value2", ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var var3 = new Variable { Id = Guid.NewGuid(), Key = "VAR3", Value = "value3", ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        db.Variables.AddRange(var1, var2, var3);
        await db.SaveChangesAsync();

        var query = new GetProjectVariables(projectId);
        var handler = new GetProjectVariablesHandler(db, NullLogger<GetProjectVariablesHandler>.Instance);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(v => v.Key == "VAR1" && v.Value == "value1");
        result.Should().Contain(v => v.Key == "VAR2" && v.Value == "value2");
        result.Should().Contain(v => v.Key == "VAR3" && v.Value == "value3");
    }

    [Fact]
    public async Task Handle_ProjectWithNoVariables_ReturnsEmptyList()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var query = new GetProjectVariables(projectId);
        var handler = new GetProjectVariablesHandler(db, NullLogger<GetProjectVariablesHandler>.Instance);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_OnlyReturnsProjectVariables_ExcludesVariableSetVariables()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var variableSetId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        // Create variable set
        var variableSet = new VariableSet { Id = variableSetId, Name = "Test Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.VariableSets.Add(variableSet);

        // Project variable
        var projectVar = new Variable { Id = Guid.NewGuid(), Key = "PROJECT_VAR", Value = "project-value", ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        // Variable set variable (should be excluded)
        var setVar = new Variable { Id = Guid.NewGuid(), Key = "SET_VAR", Value = "set-value", VariableSetId = variableSetId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        db.Variables.AddRange(projectVar, setVar);
        await db.SaveChangesAsync();

        var query = new GetProjectVariables(projectId);
        var handler = new GetProjectVariablesHandler(db, NullLogger<GetProjectVariablesHandler>.Instance);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Key.Should().Be("PROJECT_VAR");
        result[0].ProjectId.Should().Be(projectId);
        result[0].VariableSetId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_MultipleProjects_ReturnsOnlySpecifiedProjectVariables()
    {
        // Arrange
        var project1Id = Guid.NewGuid();
        var project2Id = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var project1Var1 = new Variable { Id = Guid.NewGuid(), Key = "P1_VAR1", Value = "value1", ProjectId = project1Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var project1Var2 = new Variable { Id = Guid.NewGuid(), Key = "P1_VAR2", Value = "value2", ProjectId = project1Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var project2Var = new Variable { Id = Guid.NewGuid(), Key = "P2_VAR", Value = "value", ProjectId = project2Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        db.Variables.AddRange(project1Var1, project1Var2, project2Var);
        await db.SaveChangesAsync();

        var query = new GetProjectVariables(project1Id);
        var handler = new GetProjectVariablesHandler(db, NullLogger<GetProjectVariablesHandler>.Instance);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(v => v.ProjectId == project1Id);
        result.Should().NotContain(v => v.Key == "P2_VAR");
    }

    [Fact]
    public async Task Handle_ReturnsVariablesWithAllEnvironmentScopes()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var env1Id = Guid.NewGuid();
        var env2Id = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var unscopedVar = new Variable { Id = Guid.NewGuid(), Key = "UNSCOPED", Value = "value", ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var env1Var = new Variable { Id = Guid.NewGuid(), Key = "ENV1_VAR", Value = "env1-value", EnvironmentId = env1Id, ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var env2Var = new Variable { Id = Guid.NewGuid(), Key = "ENV2_VAR", Value = "env2-value", EnvironmentId = env2Id, ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        db.Variables.AddRange(unscopedVar, env1Var, env2Var);
        await db.SaveChangesAsync();

        var query = new GetProjectVariables(projectId);
        var handler = new GetProjectVariablesHandler(db, NullLogger<GetProjectVariablesHandler>.Instance);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(v => v.EnvironmentId == null);
        result.Should().Contain(v => v.EnvironmentId == env1Id);
        result.Should().Contain(v => v.EnvironmentId == env2Id);
    }

    [Fact]
    public async Task Handle_ReturnsCompleteVariableResponse()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();
        var now = DateTime.UtcNow;

        var variable = new Variable { Id = Guid.NewGuid(), Key = "COMPLETE_VAR", Value = "complete-value", EnvironmentId = environmentId, ProjectId = projectId, CreatedAt = now, UpdatedAt = now };

        db.Variables.Add(variable);
        await db.SaveChangesAsync();

        var query = new GetProjectVariables(projectId);
        var handler = new GetProjectVariablesHandler(db, NullLogger<GetProjectVariablesHandler>.Instance);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        var response = result[0];
        response.Id.Should().Be(variable.Id);
        response.Key.Should().Be("COMPLETE_VAR");
        response.Value.Should().Be("complete-value");
        response.EnvironmentId.Should().Be(environmentId);
        response.ProjectId.Should().Be(projectId);
        response.VariableSetId.Should().BeNull();
        response.CreatedAt.Should().Be(now);
        response.UpdatedAt.Should().Be(now);
    }
}
