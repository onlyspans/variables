using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Onlyspans.Variables.Api.Features.VariableSets;
using Onlyspans.Variables.Api.Tests.Helpers;

namespace Onlyspans.Variables.Api.Tests.Features;

public class GetVariableSetsHandlerTests
{
    private readonly Mock<ILogger<GetVariableSetsHandler>> _loggerMock;

    public GetVariableSetsHandlerTests()
    {
        _loggerMock = new Mock<ILogger<GetVariableSetsHandler>>();
    }

    [Fact]
    public async Task Handle_MultipleVariableSets_ReturnsAll()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var set1 = TestDataBuilder.CreateVariableSet(name: "Set 1", description: "First");
        var set2 = TestDataBuilder.CreateVariableSet(name: "Set 2", description: "Second");
        var set3 = TestDataBuilder.CreateVariableSet(name: "Set 3");

        db.VariableSets.AddRange(set1, set2, set3);
        await db.SaveChangesAsync();

        var query = new GetVariableSets();
        var handler = new GetVariableSetsHandler(db, _loggerMock.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(s => s.Name == "Set 1");
        result.Should().Contain(s => s.Name == "Set 2");
        result.Should().Contain(s => s.Name == "Set 3");
    }

    [Fact]
    public async Task Handle_NoVariableSets_ReturnsEmptyList()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var query = new GetVariableSets();
        var handler = new GetVariableSetsHandler(db, _loggerMock.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsCompleteSetInformation()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();
        var now = DateTime.UtcNow;

        var variableSet = TestDataBuilder.CreateVariableSet(
            name: "Complete Set",
            description: "Full description",
            createdAt: now,
            updatedAt: now);

        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync();

        var query = new GetVariableSets();
        var handler = new GetVariableSetsHandler(db, _loggerMock.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        var response = result[0];
        response.Id.Should().Be(variableSet.Id);
        response.Name.Should().Be("Complete Set");
        response.Description.Should().Be("Full description");
        response.CreatedAt.Should().Be(now);
        response.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public async Task Handle_LogsInformation_WhenCalled()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var query = new GetVariableSets();
        var handler = new GetVariableSetsHandler(db, _loggerMock.Object);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Getting all variable sets")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
