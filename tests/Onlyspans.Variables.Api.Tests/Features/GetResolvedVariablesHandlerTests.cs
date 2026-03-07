using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Onlyspans.Variables.Api.Data.Entities;
using Onlyspans.Variables.Api.Data.Exceptions;
using Onlyspans.Variables.Api.Features.Variables;
using Onlyspans.Variables.Api.Tests.Helpers;

namespace Onlyspans.Variables.Api.Tests.Features;

public class GetResolvedVariablesHandlerTests
{
    [Fact]
    public async Task Handle_EnvironmentSpecificVariableBeatsUnscoped_ReturnsEnvironmentSpecificVariable()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();

        var db = MockDbContextFactory.CreateInMemoryDbContext();

        // Add unscoped project variable
        var unscopedVar = new Variable { Id = Guid.NewGuid(), Key = "API_KEY", Value = "unscoped-value", ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        // Add environment-specific project variable
        var scopedVar = new Variable { Id = Guid.NewGuid(), Key = "API_KEY", Value = "env-specific-value", EnvironmentId = environmentId, ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        db.Variables.AddRange(unscopedVar, scopedVar);
        await db.SaveChangesAsync();

        var handler = new GetResolvedVariablesHandler(db, NullLogger<GetResolvedVariablesHandler>.Instance);
        var query = new GetResolvedVariables(projectId, environmentId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Key.Should().Be("API_KEY");
        result[0].Value.Should().Be("env-specific-value");
        result[0].EnvironmentId.Should().Be(environmentId);
    }

    [Fact]
    public async Task Handle_ProjectVariableBeatsVariableSetVariable_ReturnsProjectVariable()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var variableSetId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();

        var db = MockDbContextFactory.CreateInMemoryDbContext();

        // Create variable set
        var variableSet = new VariableSet { Id = variableSetId, Name = "Test Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.VariableSets.Add(variableSet);

        // Add variable set variable
        var setVar = new Variable { Id = Guid.NewGuid(), Key = "DATABASE_URL", Value = "set-value", EnvironmentId = environmentId, VariableSetId = variableSetId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        // Add project variable with same key and scope
        var projectVar = new Variable { Id = Guid.NewGuid(), Key = "DATABASE_URL", Value = "project-value", EnvironmentId = environmentId, ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        db.Variables.AddRange(setVar, projectVar);

        // Link variable set to project
        var link = new ProjectVariableSetLink { ProjectId = projectId, VariableSetId = variableSetId, LinkedAt = DateTime.UtcNow };
        db.ProjectVariableSetLinks.Add(link);

        await db.SaveChangesAsync();

        var handler = new GetResolvedVariablesHandler(db, NullLogger<GetResolvedVariablesHandler>.Instance);
        var query = new GetResolvedVariables(projectId, environmentId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Key.Should().Be("DATABASE_URL");
        result[0].Value.Should().Be("project-value", "project variables should override variable set variables");
        result[0].ProjectId.Should().Be(projectId);
    }

    [Fact]
    public async Task Handle_MultipleVariableSetsWithSameKey_ThrowsVariableConflictException()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var set1Id = Guid.NewGuid();
        var set2Id = Guid.NewGuid();
        var environmentId = Guid.NewGuid();

        var db = MockDbContextFactory.CreateInMemoryDbContext();

        // Create two variable sets
        var set1 = new VariableSet { Id = set1Id, Name = "Set One", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var set2 = new VariableSet { Id = set2Id, Name = "Set Two", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.VariableSets.AddRange(set1, set2);

        // Add same variable key to both sets
        var var1 = new Variable { Id = Guid.NewGuid(), Key = "CONFLICT_KEY", Value = "value1", EnvironmentId = environmentId, VariableSetId = set1Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var var2 = new Variable { Id = Guid.NewGuid(), Key = "CONFLICT_KEY", Value = "value2", EnvironmentId = environmentId, VariableSetId = set2Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        db.Variables.AddRange(var1, var2);

        // Link both sets to project
        var link1 = new ProjectVariableSetLink { ProjectId = projectId, VariableSetId = set1Id, LinkedAt = DateTime.UtcNow };
        var link2 = new ProjectVariableSetLink { ProjectId = projectId, VariableSetId = set2Id, LinkedAt = DateTime.UtcNow };
        db.ProjectVariableSetLinks.AddRange(link1, link2);

        await db.SaveChangesAsync();

        var handler = new GetResolvedVariablesHandler(db, NullLogger<GetResolvedVariablesHandler>.Instance);
        var query = new GetResolvedVariables(projectId, environmentId);

        // Act
        Func<Task> act = async () => await handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<VariableConflictException>()
            .WithMessage("*CONFLICT_KEY*");
    }

    [Fact]
    public async Task Handle_ConflictException_IncludesSourceInformation()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var set1Id = Guid.NewGuid();
        var set2Id = Guid.NewGuid();

        var db = MockDbContextFactory.CreateInMemoryDbContext();

        // Create two variable sets with descriptive names
        var set1 = new VariableSet { Id = set1Id, Name = "Production Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var set2 = new VariableSet { Id = set2Id, Name = "Staging Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.VariableSets.AddRange(set1, set2);

        // Add conflicting variables
        var var1 = new Variable { Id = Guid.NewGuid(), Key = "API_ENDPOINT", Value = "prod-endpoint", VariableSetId = set1Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var var2 = new Variable { Id = Guid.NewGuid(), Key = "API_ENDPOINT", Value = "staging-endpoint", VariableSetId = set2Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        db.Variables.AddRange(var1, var2);

        // Link both sets
        db.ProjectVariableSetLinks.AddRange(
            new ProjectVariableSetLink { ProjectId = projectId, VariableSetId = set1Id, LinkedAt = DateTime.UtcNow },
            new ProjectVariableSetLink { ProjectId = projectId, VariableSetId = set2Id, LinkedAt = DateTime.UtcNow });

        await db.SaveChangesAsync();

        var handler = new GetResolvedVariablesHandler(db, NullLogger<GetResolvedVariablesHandler>.Instance);
        var query = new GetResolvedVariables(projectId, null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<VariableConflictException>(
            async () => await handler.Handle(query, CancellationToken.None));

        exception.Key.Should().Be("API_ENDPOINT");
        exception.Sources.Should().HaveCount(2);
        exception.Sources.Should().Contain("Production Set");
        exception.Sources.Should().Contain("Staging Set");
        exception.Message.Should().Contain("Production Set");
        exception.Message.Should().Contain("Staging Set");
    }

    [Fact]
    public async Task Handle_NoVariablesExist_ReturnsEmptyList()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();

        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var handler = new GetResolvedVariablesHandler(db, NullLogger<GetResolvedVariablesHandler>.Instance);
        var query = new GetResolvedVariables(projectId, environmentId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NoEnvironmentSpecified_ReturnsAllUnscopedVariables()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var env1Id = Guid.NewGuid();
        var env2Id = Guid.NewGuid();

        var db = MockDbContextFactory.CreateInMemoryDbContext();

        // Add unscoped variables
        var unscopedVar1 = new Variable { Id = Guid.NewGuid(), Key = "KEY1", Value = "value1", ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var unscopedVar2 = new Variable { Id = Guid.NewGuid(), Key = "KEY2", Value = "value2", ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        // Add environment-scoped variables (should not be included)
        var scopedVar1 = new Variable { Id = Guid.NewGuid(), Key = "ENV_KEY1", Value = "env-value1", EnvironmentId = env1Id, ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var scopedVar2 = new Variable { Id = Guid.NewGuid(), Key = "ENV_KEY2", Value = "env-value2", EnvironmentId = env2Id, ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        db.Variables.AddRange(unscopedVar1, unscopedVar2, scopedVar1, scopedVar2);
        await db.SaveChangesAsync();

        var handler = new GetResolvedVariablesHandler(db, NullLogger<GetResolvedVariablesHandler>.Instance);
        var query = new GetResolvedVariables(projectId, null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(4);
        result.Should().Contain(v => v.Key == "KEY1" && v.EnvironmentId == null);
        result.Should().Contain(v => v.Key == "KEY2" && v.EnvironmentId == null);
        result.Should().Contain(v => v.Key == "ENV_KEY1" && v.EnvironmentId == env1Id);
        result.Should().Contain(v => v.Key == "ENV_KEY2" && v.EnvironmentId == env2Id);
    }

    [Fact]
    public async Task Handle_WithEnvironmentSpecified_FiltersAndPrefersEnvironmentSpecific()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var targetEnvId = Guid.NewGuid();
        var otherEnvId = Guid.NewGuid();

        var db = MockDbContextFactory.CreateInMemoryDbContext();

        // Variable with both unscoped and target environment version
        var unscopedVar = new Variable { Id = Guid.NewGuid(), Key = "SHARED_KEY", Value = "unscoped-value", ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var targetEnvVar = new Variable { Id = Guid.NewGuid(), Key = "SHARED_KEY", Value = "target-env-value", EnvironmentId = targetEnvId, ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        // Variable only in other environment (should be excluded)
        var otherEnvVar = new Variable { Id = Guid.NewGuid(), Key = "OTHER_ENV_KEY", Value = "other-value", EnvironmentId = otherEnvId, ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        // Unscoped variable with no environment-specific version
        var plainUnscopedVar = new Variable { Id = Guid.NewGuid(), Key = "PLAIN_KEY", Value = "plain-value", ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        db.Variables.AddRange(unscopedVar, targetEnvVar, otherEnvVar, plainUnscopedVar);
        await db.SaveChangesAsync();

        var handler = new GetResolvedVariablesHandler(db, NullLogger<GetResolvedVariablesHandler>.Instance);
        var query = new GetResolvedVariables(projectId, targetEnvId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(v => v.Key == "SHARED_KEY" && v.Value == "target-env-value");
        result.Should().Contain(v => v.Key == "PLAIN_KEY" && v.Value == "plain-value");
        result.Should().NotContain(v => v.Key == "OTHER_ENV_KEY");
    }

    [Fact]
    public async Task Handle_ComplexScenario_ResolvesCorrectlyWithMultipleSetsAndScopes()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var prodEnvId = Guid.NewGuid();
        var set1Id = Guid.NewGuid();
        var set2Id = Guid.NewGuid();

        var db = MockDbContextFactory.CreateInMemoryDbContext();

        // Create two variable sets
        var set1 = new VariableSet { Id = set1Id, Name = "Common Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var set2 = new VariableSet { Id = set2Id, Name = "Database Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.VariableSets.AddRange(set1, set2);

        // Set 1: Unscoped variable
        var set1Var = new Variable { Id = Guid.NewGuid(), Key = "COMMON_VAR", Value = "common-value", VariableSetId = set1Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        // Set 2: Environment-specific variable
        var set2Var = new Variable { Id = Guid.NewGuid(), Key = "DB_HOST", Value = "set2-db-host", EnvironmentId = prodEnvId, VariableSetId = set2Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        // Project: Override one set variable
        var projectOverride = new Variable { Id = Guid.NewGuid(), Key = "DB_HOST", Value = "project-db-host", EnvironmentId = prodEnvId, ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        // Project: Additional unscoped variable
        var projectVar = new Variable { Id = Guid.NewGuid(), Key = "PROJECT_SPECIFIC", Value = "project-value", ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        db.Variables.AddRange(set1Var, set2Var, projectOverride, projectVar);

        // Link both sets
        db.ProjectVariableSetLinks.AddRange(
            new ProjectVariableSetLink { ProjectId = projectId, VariableSetId = set1Id, LinkedAt = DateTime.UtcNow },
            new ProjectVariableSetLink { ProjectId = projectId, VariableSetId = set2Id, LinkedAt = DateTime.UtcNow });

        await db.SaveChangesAsync();

        var handler = new GetResolvedVariablesHandler(db, NullLogger<GetResolvedVariablesHandler>.Instance);
        var query = new GetResolvedVariables(projectId, prodEnvId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);

        // Verify common variable from set
        result.Should().Contain(v => v.Key == "COMMON_VAR" && v.Value == "common-value");

        // Verify project override beats set variable
        var dbHost = result.First(v => v.Key == "DB_HOST");
        dbHost.Value.Should().Be("project-db-host");
        dbHost.ProjectId.Should().Be(projectId);

        // Verify project-specific variable
        result.Should().Contain(v => v.Key == "PROJECT_SPECIFIC" && v.Value == "project-value");
    }
}
