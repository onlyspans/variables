using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Onlyspans.Variables.Api.Data.Exceptions;
using Onlyspans.Variables.Api.Features.Variables;
using Onlyspans.Variables.Api.Tests.Helpers;

namespace Onlyspans.Variables.Api.Tests.Features;

public class GetResolvedVariablesHandlerTests
{
    private readonly Mock<ILogger<GetResolvedVariablesHandler>> _loggerMock;

    public GetResolvedVariablesHandlerTests()
    {
        _loggerMock = new Mock<ILogger<GetResolvedVariablesHandler>>();
    }

    [Fact]
    public async Task Handle_EnvironmentSpecificVariableBeatsUnscoped_ReturnsEnvironmentSpecificVariable()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();

        var db = MockDbContextFactory.CreateInMemoryDbContext();

        // Add unscoped project variable
        var unscopedVar = TestDataBuilder.CreateVariable(
            key: "API_KEY",
            value: "unscoped-value",
            environmentId: null,
            projectId: projectId);

        // Add environment-specific project variable
        var scopedVar = TestDataBuilder.CreateVariable(
            key: "API_KEY",
            value: "env-specific-value",
            environmentId: environmentId,
            projectId: projectId);

        db.Variables.AddRange(unscopedVar, scopedVar);
        await db.SaveChangesAsync();

        var handler = new GetResolvedVariablesHandler(db, _loggerMock.Object);
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
        var variableSet = TestDataBuilder.CreateVariableSet(id: variableSetId, name: "Test Set");
        db.VariableSets.Add(variableSet);

        // Add variable set variable
        var setVar = TestDataBuilder.CreateVariable(
            key: "DATABASE_URL",
            value: "set-value",
            environmentId: environmentId,
            variableSetId: variableSetId);

        // Add project variable with same key and scope
        var projectVar = TestDataBuilder.CreateVariable(
            key: "DATABASE_URL",
            value: "project-value",
            environmentId: environmentId,
            projectId: projectId);

        db.Variables.AddRange(setVar, projectVar);

        // Link variable set to project
        var link = TestDataBuilder.CreateLink(projectId, variableSetId);
        db.ProjectVariableSetLinks.Add(link);

        await db.SaveChangesAsync();

        var handler = new GetResolvedVariablesHandler(db, _loggerMock.Object);
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
        var set1 = TestDataBuilder.CreateVariableSet(id: set1Id, name: "Set One");
        var set2 = TestDataBuilder.CreateVariableSet(id: set2Id, name: "Set Two");
        db.VariableSets.AddRange(set1, set2);

        // Add same variable key to both sets
        var var1 = TestDataBuilder.CreateVariable(
            key: "CONFLICT_KEY",
            value: "value1",
            environmentId: environmentId,
            variableSetId: set1Id);

        var var2 = TestDataBuilder.CreateVariable(
            key: "CONFLICT_KEY",
            value: "value2",
            environmentId: environmentId,
            variableSetId: set2Id);

        db.Variables.AddRange(var1, var2);

        // Link both sets to project
        var link1 = TestDataBuilder.CreateLink(projectId, set1Id);
        var link2 = TestDataBuilder.CreateLink(projectId, set2Id);
        db.ProjectVariableSetLinks.AddRange(link1, link2);

        await db.SaveChangesAsync();

        var handler = new GetResolvedVariablesHandler(db, _loggerMock.Object);
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
        var set1 = TestDataBuilder.CreateVariableSet(id: set1Id, name: "Production Set");
        var set2 = TestDataBuilder.CreateVariableSet(id: set2Id, name: "Staging Set");
        db.VariableSets.AddRange(set1, set2);

        // Add conflicting variables
        var var1 = TestDataBuilder.CreateVariable(
            key: "API_ENDPOINT",
            value: "prod-endpoint",
            variableSetId: set1Id);

        var var2 = TestDataBuilder.CreateVariable(
            key: "API_ENDPOINT",
            value: "staging-endpoint",
            variableSetId: set2Id);

        db.Variables.AddRange(var1, var2);

        // Link both sets
        db.ProjectVariableSetLinks.AddRange(
            TestDataBuilder.CreateLink(projectId, set1Id),
            TestDataBuilder.CreateLink(projectId, set2Id));

        await db.SaveChangesAsync();

        var handler = new GetResolvedVariablesHandler(db, _loggerMock.Object);
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

        var handler = new GetResolvedVariablesHandler(db, _loggerMock.Object);
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
        var unscopedVar1 = TestDataBuilder.CreateVariable(
            key: "KEY1",
            value: "value1",
            environmentId: null,
            projectId: projectId);

        var unscopedVar2 = TestDataBuilder.CreateVariable(
            key: "KEY2",
            value: "value2",
            environmentId: null,
            projectId: projectId);

        // Add environment-scoped variables (should not be included)
        var scopedVar1 = TestDataBuilder.CreateVariable(
            key: "ENV_KEY1",
            value: "env-value1",
            environmentId: env1Id,
            projectId: projectId);

        var scopedVar2 = TestDataBuilder.CreateVariable(
            key: "ENV_KEY2",
            value: "env-value2",
            environmentId: env2Id,
            projectId: projectId);

        db.Variables.AddRange(unscopedVar1, unscopedVar2, scopedVar1, scopedVar2);
        await db.SaveChangesAsync();

        var handler = new GetResolvedVariablesHandler(db, _loggerMock.Object);
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
        var unscopedVar = TestDataBuilder.CreateVariable(
            key: "SHARED_KEY",
            value: "unscoped-value",
            environmentId: null,
            projectId: projectId);

        var targetEnvVar = TestDataBuilder.CreateVariable(
            key: "SHARED_KEY",
            value: "target-env-value",
            environmentId: targetEnvId,
            projectId: projectId);

        // Variable only in other environment (should be excluded)
        var otherEnvVar = TestDataBuilder.CreateVariable(
            key: "OTHER_ENV_KEY",
            value: "other-value",
            environmentId: otherEnvId,
            projectId: projectId);

        // Unscoped variable with no environment-specific version
        var plainUnscopedVar = TestDataBuilder.CreateVariable(
            key: "PLAIN_KEY",
            value: "plain-value",
            environmentId: null,
            projectId: projectId);

        db.Variables.AddRange(unscopedVar, targetEnvVar, otherEnvVar, plainUnscopedVar);
        await db.SaveChangesAsync();

        var handler = new GetResolvedVariablesHandler(db, _loggerMock.Object);
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
        var set1 = TestDataBuilder.CreateVariableSet(id: set1Id, name: "Common Set");
        var set2 = TestDataBuilder.CreateVariableSet(id: set2Id, name: "Database Set");
        db.VariableSets.AddRange(set1, set2);

        // Set 1: Unscoped variable
        var set1Var = TestDataBuilder.CreateVariable(
            key: "COMMON_VAR",
            value: "common-value",
            environmentId: null,
            variableSetId: set1Id);

        // Set 2: Environment-specific variable
        var set2Var = TestDataBuilder.CreateVariable(
            key: "DB_HOST",
            value: "set2-db-host",
            environmentId: prodEnvId,
            variableSetId: set2Id);

        // Project: Override one set variable
        var projectOverride = TestDataBuilder.CreateVariable(
            key: "DB_HOST",
            value: "project-db-host",
            environmentId: prodEnvId,
            projectId: projectId);

        // Project: Additional unscoped variable
        var projectVar = TestDataBuilder.CreateVariable(
            key: "PROJECT_SPECIFIC",
            value: "project-value",
            environmentId: null,
            projectId: projectId);

        db.Variables.AddRange(set1Var, set2Var, projectOverride, projectVar);

        // Link both sets
        db.ProjectVariableSetLinks.AddRange(
            TestDataBuilder.CreateLink(projectId, set1Id),
            TestDataBuilder.CreateLink(projectId, set2Id));

        await db.SaveChangesAsync();

        var handler = new GetResolvedVariablesHandler(db, _loggerMock.Object);
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

    [Fact]
    public async Task Handle_LogsInformation_WhenCalled()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();

        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var handler = new GetResolvedVariablesHandler(db, _loggerMock.Object);
        var query = new GetResolvedVariables(projectId, environmentId);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Resolving variables")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
