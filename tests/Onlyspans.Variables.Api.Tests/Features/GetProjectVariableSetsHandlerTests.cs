using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Onlyspans.Variables.Api.Features.VariableSets;
using Onlyspans.Variables.Api.Tests.Helpers;

namespace Onlyspans.Variables.Api.Tests.Features;

public class GetProjectVariableSetsHandlerTests
{
    private readonly Mock<ILogger<GetProjectVariableSetsHandler>> _loggerMock;

    public GetProjectVariableSetsHandlerTests()
    {
        _loggerMock = new Mock<ILogger<GetProjectVariableSetsHandler>>();
    }

    [Fact]
    public async Task Handle_ProjectWithLinkedSets_ReturnsAllLinkedSets()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var set1Id = Guid.NewGuid();
        var set2Id = Guid.NewGuid();
        var set3Id = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var set1 = TestDataBuilder.CreateVariableSet(id: set1Id, name: "Set 1", description: "First");
        var set2 = TestDataBuilder.CreateVariableSet(id: set2Id, name: "Set 2", description: "Second");
        var set3 = TestDataBuilder.CreateVariableSet(id: set3Id, name: "Set 3", description: "Third");
        db.VariableSets.AddRange(set1, set2, set3);

        var link1 = TestDataBuilder.CreateLink(projectId, set1Id);
        var link2 = TestDataBuilder.CreateLink(projectId, set2Id);
        var link3 = TestDataBuilder.CreateLink(projectId, set3Id);
        db.ProjectVariableSetLinks.AddRange(link1, link2, link3);
        await db.SaveChangesAsync();

        var query = new GetProjectVariableSets(projectId);
        var handler = new GetProjectVariableSetsHandler(db, _loggerMock.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(s => s.Name == "Set 1");
        result.Should().Contain(s => s.Name == "Set 2");
        result.Should().Contain(s => s.Name == "Set 3");
    }

    [Fact]
    public async Task Handle_ProjectWithNoLinkedSets_ReturnsEmptyList()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var query = new GetProjectVariableSets(projectId);
        var handler = new GetProjectVariableSetsHandler(db, _loggerMock.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_OnlyReturnsLinkedSets_ExcludesUnlinkedSets()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var linkedSetId = Guid.NewGuid();
        var unlinkedSetId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var linkedSet = TestDataBuilder.CreateVariableSet(id: linkedSetId, name: "Linked Set");
        var unlinkedSet = TestDataBuilder.CreateVariableSet(id: unlinkedSetId, name: "Unlinked Set");
        db.VariableSets.AddRange(linkedSet, unlinkedSet);

        var link = TestDataBuilder.CreateLink(projectId, linkedSetId);
        db.ProjectVariableSetLinks.Add(link);
        await db.SaveChangesAsync();

        var query = new GetProjectVariableSets(projectId);
        var handler = new GetProjectVariableSetsHandler(db, _loggerMock.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Linked Set");
        result.Should().NotContain(s => s.Name == "Unlinked Set");
    }

    [Fact]
    public async Task Handle_MultipleProjects_ReturnsOnlySpecifiedProjectSets()
    {
        // Arrange
        var project1Id = Guid.NewGuid();
        var project2Id = Guid.NewGuid();
        var set1Id = Guid.NewGuid();
        var set2Id = Guid.NewGuid();
        var set3Id = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var set1 = TestDataBuilder.CreateVariableSet(id: set1Id, name: "P1 Set 1");
        var set2 = TestDataBuilder.CreateVariableSet(id: set2Id, name: "P1 Set 2");
        var set3 = TestDataBuilder.CreateVariableSet(id: set3Id, name: "P2 Set");
        db.VariableSets.AddRange(set1, set2, set3);

        // Link sets 1 and 2 to project1, set 3 to project2
        db.ProjectVariableSetLinks.AddRange(
            TestDataBuilder.CreateLink(project1Id, set1Id),
            TestDataBuilder.CreateLink(project1Id, set2Id),
            TestDataBuilder.CreateLink(project2Id, set3Id));
        await db.SaveChangesAsync();

        var query = new GetProjectVariableSets(project1Id);
        var handler = new GetProjectVariableSetsHandler(db, _loggerMock.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(s => s.Name == "P1 Set 1");
        result.Should().Contain(s => s.Name == "P1 Set 2");
        result.Should().NotContain(s => s.Name == "P2 Set");
    }

    [Fact]
    public async Task Handle_ReturnsCompleteSetInformation()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var setId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();
        var now = DateTime.UtcNow;

        var variableSet = TestDataBuilder.CreateVariableSet(
            id: setId,
            name: "Complete Set",
            description: "Full description",
            createdAt: now,
            updatedAt: now);

        db.VariableSets.Add(variableSet);

        var link = TestDataBuilder.CreateLink(projectId, setId);
        db.ProjectVariableSetLinks.Add(link);
        await db.SaveChangesAsync();

        var query = new GetProjectVariableSets(projectId);
        var handler = new GetProjectVariableSetsHandler(db, _loggerMock.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        var response = result[0];
        response.Id.Should().Be(setId);
        response.Name.Should().Be("Complete Set");
        response.Description.Should().Be("Full description");
        response.CreatedAt.Should().Be(now);
        response.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public async Task Handle_LogsInformation_WhenCalled()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var query = new GetProjectVariableSets(projectId);
        var handler = new GetProjectVariableSetsHandler(db, _loggerMock.Object);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Getting variable sets linked to project")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
