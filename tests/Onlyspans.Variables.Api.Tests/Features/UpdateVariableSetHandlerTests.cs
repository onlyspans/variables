using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Onlyspans.Variables.Api.Data.Records;
using Onlyspans.Variables.Api.Features.VariableSets;
using Onlyspans.Variables.Api.Tests.Helpers;

namespace Onlyspans.Variables.Api.Tests.Features;

public class UpdateVariableSetHandlerTests
{
    private readonly Mock<ILogger<UpdateVariableSetHandler>> _loggerMock;

    public UpdateVariableSetHandlerTests()
    {
        _loggerMock = new Mock<ILogger<UpdateVariableSetHandler>>();
    }

    [Fact]
    public async Task Handle_UpdateName_UpdatesSuccessfully()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variableSet = TestDataBuilder.CreateVariableSet(
            name: "Old Name",
            description: "Description");

        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync();

        var request = new UpdateVariableSetRequest(Name: "New Name");
        var command = new UpdateVariableSet(variableSet.Id, request);
        var handler = new UpdateVariableSetHandler(db, _loggerMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Name.Should().Be("New Name");
        result.Description.Should().Be("Description");
    }

    [Fact]
    public async Task Handle_UpdateDescription_UpdatesSuccessfully()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variableSet = TestDataBuilder.CreateVariableSet(
            name: "Test Set",
            description: "Old Description");

        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync();

        var request = new UpdateVariableSetRequest(Description: "New Description");
        var command = new UpdateVariableSet(variableSet.Id, request);
        var handler = new UpdateVariableSetHandler(db, _loggerMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Name.Should().Be("Test Set");
        result.Description.Should().Be("New Description");
    }

    [Fact]
    public async Task Handle_UpdateBothFields_UpdatesSuccessfully()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variableSet = TestDataBuilder.CreateVariableSet(
            name: "Old Name",
            description: "Old Description");

        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync();

        var request = new UpdateVariableSetRequest(
            Name: "New Name",
            Description: "New Description");

        var command = new UpdateVariableSet(variableSet.Id, request);
        var handler = new UpdateVariableSetHandler(db, _loggerMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Name.Should().Be("New Name");
        result.Description.Should().Be("New Description");
    }

    [Fact]
    public async Task Handle_VariableSetNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var request = new UpdateVariableSetRequest(Name: "New Name");
        var command = new UpdateVariableSet(nonExistentId, request);
        var handler = new UpdateVariableSetHandler(db, _loggerMock.Object);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Variable set {nonExistentId} not found");
    }

    [Fact]
    public async Task Handle_UpdatesTimestamp_UpdatedAtIsModified()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var originalCreatedAt = DateTime.UtcNow.AddHours(-1);
        var variableSet = TestDataBuilder.CreateVariableSet(
            name: "Test Set",
            createdAt: originalCreatedAt,
            updatedAt: originalCreatedAt);

        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync();

        await Task.Delay(100);

        var request = new UpdateVariableSetRequest(Name: "Updated Name");
        var command = new UpdateVariableSet(variableSet.Id, request);
        var handler = new UpdateVariableSetHandler(db, _loggerMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.UpdatedAt.Should().BeAfter(result.CreatedAt);
        result.CreatedAt.Should().Be(originalCreatedAt);
    }

    [Fact]
    public async Task Handle_PersistsChanges_CanBeRetrievedFromDatabase()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variableSet = TestDataBuilder.CreateVariableSet(
            name: "Initial Name",
            description: "Initial Description");

        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync();

        var request = new UpdateVariableSetRequest(
            Name: "Updated Name",
            Description: "Updated Description");

        var command = new UpdateVariableSet(variableSet.Id, request);
        var handler = new UpdateVariableSetHandler(db, _loggerMock.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var updated = await db.VariableSets.FirstOrDefaultAsync(vs => vs.Id == variableSet.Id);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Name");
        updated.Description.Should().Be("Updated Description");
    }

    [Fact]
    public async Task Handle_LogsInformation_OnUpdate()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variableSet = TestDataBuilder.CreateVariableSet(name: "Test Set");
        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync();

        var request = new UpdateVariableSetRequest(Name: "New Name");
        var command = new UpdateVariableSet(variableSet.Id, request);
        var handler = new UpdateVariableSetHandler(db, _loggerMock.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Updating variable set")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
