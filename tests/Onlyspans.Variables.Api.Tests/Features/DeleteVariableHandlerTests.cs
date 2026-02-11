using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Onlyspans.Variables.Api.Features.Variables;
using Onlyspans.Variables.Api.Tests.Helpers;

namespace Onlyspans.Variables.Api.Tests.Features;

public class DeleteVariableHandlerTests
{
    private readonly Mock<ILogger<DeleteVariableHandler>> _loggerMock;

    public DeleteVariableHandlerTests()
    {
        _loggerMock = new Mock<ILogger<DeleteVariableHandler>>();
    }

    [Fact]
    public async Task Handle_ExistingVariable_DeletesSuccessfully()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variable = TestDataBuilder.CreateVariable(
            key: "TO_DELETE",
            value: "value",
            projectId: projectId);

        db.Variables.Add(variable);
        await db.SaveChangesAsync();

        var command = new DeleteVariable(variable.Id);
        var handler = new DeleteVariableHandler(db, _loggerMock.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var deletedVariable = await db.Variables.FirstOrDefaultAsync(v => v.Id == variable.Id);
        deletedVariable.Should().BeNull();
    }

    [Fact]
    public async Task Handle_VariableNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var command = new DeleteVariable(nonExistentId);
        var handler = new DeleteVariableHandler(db, _loggerMock.Object);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Variable {nonExistentId} not found");
    }

    [Fact]
    public async Task Handle_DeletesVariable_DoesNotAffectOtherVariables()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variable1 = TestDataBuilder.CreateVariable(
            key: "VAR1",
            value: "value1",
            projectId: projectId);

        var variable2 = TestDataBuilder.CreateVariable(
            key: "VAR2",
            value: "value2",
            projectId: projectId);

        var variable3 = TestDataBuilder.CreateVariable(
            key: "VAR3",
            value: "value3",
            projectId: projectId);

        db.Variables.AddRange(variable1, variable2, variable3);
        await db.SaveChangesAsync();

        var command = new DeleteVariable(variable2.Id);
        var handler = new DeleteVariableHandler(db, _loggerMock.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var remainingVariables = await db.Variables.ToListAsync();
        remainingVariables.Should().HaveCount(2);
        remainingVariables.Should().Contain(v => v.Id == variable1.Id);
        remainingVariables.Should().Contain(v => v.Id == variable3.Id);
        remainingVariables.Should().NotContain(v => v.Id == variable2.Id);
    }

    [Fact]
    public async Task Handle_LogsInformation_OnDelete()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variable = TestDataBuilder.CreateVariable(
            key: "LOG_TEST",
            value: "value",
            projectId: projectId);

        db.Variables.Add(variable);
        await db.SaveChangesAsync();

        var command = new DeleteVariable(variable.Id);
        var handler = new DeleteVariableHandler(db, _loggerMock.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Deleting variable")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Deleted variable")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DeleteAllVariablesInProject_LeavesProjectIntact()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variable1 = TestDataBuilder.CreateVariable(
            key: "VAR1",
            value: "value1",
            projectId: projectId);

        var variable2 = TestDataBuilder.CreateVariable(
            key: "VAR2",
            value: "value2",
            projectId: projectId);

        db.Variables.AddRange(variable1, variable2);
        await db.SaveChangesAsync();

        var handler = new DeleteVariableHandler(db, _loggerMock.Object);

        // Act
        await handler.Handle(new DeleteVariable(variable1.Id), CancellationToken.None);
        await handler.Handle(new DeleteVariable(variable2.Id), CancellationToken.None);

        // Assert
        var remainingVariables = await db.Variables.Where(v => v.ProjectId == projectId).ToListAsync();
        remainingVariables.Should().BeEmpty();
    }
}
