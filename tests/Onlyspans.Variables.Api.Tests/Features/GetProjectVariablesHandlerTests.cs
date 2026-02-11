using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Onlyspans.Variables.Api.Features.Variables;
using Onlyspans.Variables.Api.Tests.Helpers;

namespace Onlyspans.Variables.Api.Tests.Features;

public class GetProjectVariablesHandlerTests
{
    private readonly Mock<ILogger<GetProjectVariablesHandler>> _loggerMock;

    public GetProjectVariablesHandlerTests()
    {
        _loggerMock = new Mock<ILogger<GetProjectVariablesHandler>>();
    }

    [Fact]
    public async Task Handle_ProjectWithVariables_ReturnsAllProjectVariables()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var var1 = TestDataBuilder.CreateVariable(
            key: "VAR1",
            value: "value1",
            projectId: projectId);

        var var2 = TestDataBuilder.CreateVariable(
            key: "VAR2",
            value: "value2",
            projectId: projectId);

        var var3 = TestDataBuilder.CreateVariable(
            key: "VAR3",
            value: "value3",
            projectId: projectId);

        db.Variables.AddRange(var1, var2, var3);
        await db.SaveChangesAsync();

        var query = new GetProjectVariables(projectId);
        var handler = new GetProjectVariablesHandler(db, _loggerMock.Object);

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
        var handler = new GetProjectVariablesHandler(db, _loggerMock.Object);

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
        var variableSet = TestDataBuilder.CreateVariableSet(id: variableSetId, name: "Test Set");
        db.VariableSets.Add(variableSet);

        // Project variable
        var projectVar = TestDataBuilder.CreateVariable(
            key: "PROJECT_VAR",
            value: "project-value",
            projectId: projectId);

        // Variable set variable (should be excluded)
        var setVar = TestDataBuilder.CreateVariable(
            key: "SET_VAR",
            value: "set-value",
            variableSetId: variableSetId);

        db.Variables.AddRange(projectVar, setVar);
        await db.SaveChangesAsync();

        var query = new GetProjectVariables(projectId);
        var handler = new GetProjectVariablesHandler(db, _loggerMock.Object);

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

        var project1Var1 = TestDataBuilder.CreateVariable(
            key: "P1_VAR1",
            value: "value1",
            projectId: project1Id);

        var project1Var2 = TestDataBuilder.CreateVariable(
            key: "P1_VAR2",
            value: "value2",
            projectId: project1Id);

        var project2Var = TestDataBuilder.CreateVariable(
            key: "P2_VAR",
            value: "value",
            projectId: project2Id);

        db.Variables.AddRange(project1Var1, project1Var2, project2Var);
        await db.SaveChangesAsync();

        var query = new GetProjectVariables(project1Id);
        var handler = new GetProjectVariablesHandler(db, _loggerMock.Object);

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

        var unscopedVar = TestDataBuilder.CreateVariable(
            key: "UNSCOPED",
            value: "value",
            environmentId: null,
            projectId: projectId);

        var env1Var = TestDataBuilder.CreateVariable(
            key: "ENV1_VAR",
            value: "env1-value",
            environmentId: env1Id,
            projectId: projectId);

        var env2Var = TestDataBuilder.CreateVariable(
            key: "ENV2_VAR",
            value: "env2-value",
            environmentId: env2Id,
            projectId: projectId);

        db.Variables.AddRange(unscopedVar, env1Var, env2Var);
        await db.SaveChangesAsync();

        var query = new GetProjectVariables(projectId);
        var handler = new GetProjectVariablesHandler(db, _loggerMock.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(v => v.EnvironmentId == null);
        result.Should().Contain(v => v.EnvironmentId == env1Id);
        result.Should().Contain(v => v.EnvironmentId == env2Id);
    }

    [Fact]
    public async Task Handle_LogsInformation_WhenCalled()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var query = new GetProjectVariables(projectId);
        var handler = new GetProjectVariablesHandler(db, _loggerMock.Object);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Getting direct project variables")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsCompleteVariableResponse()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();
        var now = DateTime.UtcNow;

        var variable = TestDataBuilder.CreateVariable(
            key: "COMPLETE_VAR",
            value: "complete-value",
            environmentId: environmentId,
            projectId: projectId,
            createdAt: now,
            updatedAt: now);

        db.Variables.Add(variable);
        await db.SaveChangesAsync();

        var query = new GetProjectVariables(projectId);
        var handler = new GetProjectVariablesHandler(db, _loggerMock.Object);

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
