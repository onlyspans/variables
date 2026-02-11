using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Onlyspans.Variables.Api.Data.Records;
using Onlyspans.Variables.Api.Features.Variables;
using Onlyspans.Variables.Api.Tests.Helpers;

namespace Onlyspans.Variables.Api.Tests.Features;

public class CreateVariableHandlerTests
{
    private readonly Mock<ILogger<CreateVariableHandler>> _loggerMock;

    public CreateVariableHandlerTests()
    {
        _loggerMock = new Mock<ILogger<CreateVariableHandler>>();
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesVariableSuccessfully()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var request = new CreateVariableRequest(
            Key: "API_KEY",
            Value: "secret-value",
            EnvironmentId: null,
            VariableSetId: null);

        var command = new CreateVariable(projectId, request);
        var handler = new CreateVariableHandler(db, _loggerMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Key.Should().Be("API_KEY");
        result.Value.Should().Be("secret-value");
        result.ProjectId.Should().Be(projectId);
        result.EnvironmentId.Should().BeNull();
        result.VariableSetId.Should().BeNull();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Handle_WithEnvironmentId_CreatesEnvironmentScopedVariable()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var request = new CreateVariableRequest(
            Key: "DATABASE_URL",
            Value: "postgres://localhost:5432",
            EnvironmentId: environmentId);

        var command = new CreateVariable(projectId, request);
        var handler = new CreateVariableHandler(db, _loggerMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.EnvironmentId.Should().Be(environmentId);
        result.ProjectId.Should().Be(projectId);
    }

    [Fact]
    public async Task Handle_PersistsToDatabase_CanBeRetrieved()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var request = new CreateVariableRequest(
            Key: "FEATURE_FLAG",
            Value: "true");

        var command = new CreateVariable(projectId, request);
        var handler = new CreateVariableHandler(db, _loggerMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        var savedVariable = await db.Variables.FirstOrDefaultAsync(v => v.Id == result.Id);
        savedVariable.Should().NotBeNull();
        savedVariable!.Key.Should().Be("FEATURE_FLAG");
        savedVariable.Value.Should().Be("true");
        savedVariable.ProjectId.Should().Be(projectId);
    }

    [Fact]
    public async Task Handle_LogsInformation_OnSuccess()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var request = new CreateVariableRequest(Key: "TEST", Value: "value");
        var command = new CreateVariable(projectId, request);
        var handler = new CreateVariableHandler(db, _loggerMock.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Creating variable")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Created variable")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SetsTimestamps_CreatedAtAndUpdatedAtAreEqual()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var request = new CreateVariableRequest(Key: "TIMESTAMP_TEST", Value: "value");
        var command = new CreateVariable(projectId, request);
        var handler = new CreateVariableHandler(db, _loggerMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.CreatedAt.Should().Be(result.UpdatedAt);
    }

    [Fact]
    public async Task Handle_MultipleVariables_CreatesAllSuccessfully()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();
        var handler = new CreateVariableHandler(db, _loggerMock.Object);

        var request1 = new CreateVariableRequest(Key: "VAR1", Value: "value1");
        var request2 = new CreateVariableRequest(Key: "VAR2", Value: "value2");
        var request3 = new CreateVariableRequest(Key: "VAR3", Value: "value3");

        // Act
        var result1 = await handler.Handle(new CreateVariable(projectId, request1), CancellationToken.None);
        var result2 = await handler.Handle(new CreateVariable(projectId, request2), CancellationToken.None);
        var result3 = await handler.Handle(new CreateVariable(projectId, request3), CancellationToken.None);

        // Assert
        var allVariables = await db.Variables.Where(v => v.ProjectId == projectId).ToListAsync();
        allVariables.Should().HaveCount(3);
        allVariables.Should().OnlyContain(v => v.ProjectId == projectId);
    }
}
