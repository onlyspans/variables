using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Onlyspans.Variables.Api.Features.VariableSets;
using Onlyspans.Variables.Api.Tests.Helpers;

namespace Onlyspans.Variables.Api.Tests.Features;

public class GetVariableSetHandlerTests
{
    private readonly Mock<ILogger<GetVariableSetHandler>> _loggerMock;

    public GetVariableSetHandlerTests()
    {
        _loggerMock = new Mock<ILogger<GetVariableSetHandler>>();
    }

    [Fact]
    public async Task Handle_ExistingVariableSet_ReturnsDetailWithVariables()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var setId = Guid.NewGuid();
        var variableSet = TestDataBuilder.CreateVariableSet(
            id: setId,
            name: "Test Set",
            description: "Test description");

        var var1 = TestDataBuilder.CreateVariable(
            key: "VAR1",
            value: "value1",
            variableSetId: setId);

        var var2 = TestDataBuilder.CreateVariable(
            key: "VAR2",
            value: "value2",
            variableSetId: setId);

        db.VariableSets.Add(variableSet);
        db.Variables.AddRange(var1, var2);
        await db.SaveChangesAsync();

        var query = new GetVariableSet(setId);
        var handler = new GetVariableSetHandler(db, _loggerMock.Object);

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

        var variableSet = TestDataBuilder.CreateVariableSet(name: "Empty Set");
        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync();

        var query = new GetVariableSet(variableSet.Id);
        var handler = new GetVariableSetHandler(db, _loggerMock.Object);

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
        var handler = new GetVariableSetHandler(db, _loggerMock.Object);

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
        var variableSet = TestDataBuilder.CreateVariableSet(id: setId, name: "Scoped Set");

        var unscopedVar = TestDataBuilder.CreateVariable(
            key: "UNSCOPED",
            value: "value",
            environmentId: null,
            variableSetId: setId);

        var scopedVar = TestDataBuilder.CreateVariable(
            key: "SCOPED",
            value: "env-value",
            environmentId: envId,
            variableSetId: setId);

        db.VariableSets.Add(variableSet);
        db.Variables.AddRange(unscopedVar, scopedVar);
        await db.SaveChangesAsync();

        var query = new GetVariableSet(setId);
        var handler = new GetVariableSetHandler(db, _loggerMock.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Variables.Should().HaveCount(2);
        result.Variables.Should().Contain(v => v.Key == "UNSCOPED" && v.EnvironmentId == null);
        result.Variables.Should().Contain(v => v.Key == "SCOPED" && v.EnvironmentId == envId);
    }

    [Fact]
    public async Task Handle_LogsInformation_WhenCalled()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variableSet = TestDataBuilder.CreateVariableSet(name: "Test Set");
        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync();

        var query = new GetVariableSet(variableSet.Id);
        var handler = new GetVariableSetHandler(db, _loggerMock.Object);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Getting variable set")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
