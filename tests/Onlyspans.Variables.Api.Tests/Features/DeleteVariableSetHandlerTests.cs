using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Onlyspans.Variables.Api.Features.VariableSets;
using Onlyspans.Variables.Api.Tests.Helpers;

namespace Onlyspans.Variables.Api.Tests.Features;

public class DeleteVariableSetHandlerTests
{
    private readonly Mock<ILogger<DeleteVariableSetHandler>> _loggerMock;

    public DeleteVariableSetHandlerTests()
    {
        _loggerMock = new Mock<ILogger<DeleteVariableSetHandler>>();
    }

    [Fact]
    public async Task Handle_ExistingVariableSet_DeletesSuccessfully()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variableSet = TestDataBuilder.CreateVariableSet(name: "To Delete");
        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync();

        var command = new DeleteVariableSet(variableSet.Id);
        var handler = new DeleteVariableSetHandler(db, _loggerMock.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var deleted = await db.VariableSets.FirstOrDefaultAsync(vs => vs.Id == variableSet.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task Handle_VariableSetNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var command = new DeleteVariableSet(nonExistentId);
        var handler = new DeleteVariableSetHandler(db, _loggerMock.Object);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Variable set {nonExistentId} not found");
    }

    [Fact]
    public async Task Handle_DeleteVariableSet_DoesNotAffectOthers()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var set1 = TestDataBuilder.CreateVariableSet(name: "Set 1");
        var set2 = TestDataBuilder.CreateVariableSet(name: "Set 2");
        var set3 = TestDataBuilder.CreateVariableSet(name: "Set 3");

        db.VariableSets.AddRange(set1, set2, set3);
        await db.SaveChangesAsync();

        var command = new DeleteVariableSet(set2.Id);
        var handler = new DeleteVariableSetHandler(db, _loggerMock.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var remaining = await db.VariableSets.ToListAsync();
        remaining.Should().HaveCount(2);
        remaining.Should().Contain(s => s.Id == set1.Id);
        remaining.Should().Contain(s => s.Id == set3.Id);
    }

    [Fact]
    public async Task Handle_LogsInformation_OnDelete()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variableSet = TestDataBuilder.CreateVariableSet(name: "Test Set");
        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync();

        var command = new DeleteVariableSet(variableSet.Id);
        var handler = new DeleteVariableSetHandler(db, _loggerMock.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Deleting variable set")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
