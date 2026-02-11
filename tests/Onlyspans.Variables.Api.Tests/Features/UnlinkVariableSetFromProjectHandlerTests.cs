using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Onlyspans.Variables.Api.Features.VariableSets;
using Onlyspans.Variables.Api.Tests.Helpers;

namespace Onlyspans.Variables.Api.Tests.Features;

public class UnlinkVariableSetFromProjectHandlerTests
{
    private readonly Mock<ILogger<UnlinkVariableSetFromProjectHandler>> _loggerMock;

    public UnlinkVariableSetFromProjectHandlerTests()
    {
        _loggerMock = new Mock<ILogger<UnlinkVariableSetFromProjectHandler>>();
    }

    [Fact]
    public async Task Handle_ExistingLink_UnlinksSuccessfully()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var setId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variableSet = TestDataBuilder.CreateVariableSet(id: setId, name: "Test Set");
        db.VariableSets.Add(variableSet);

        var link = TestDataBuilder.CreateLink(projectId, setId);
        db.ProjectVariableSetLinks.Add(link);
        await db.SaveChangesAsync();

        var command = new UnlinkVariableSetFromProject(projectId, setId);
        var handler = new UnlinkVariableSetFromProjectHandler(db, _loggerMock.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var removedLink = await db.ProjectVariableSetLinks
            .FirstOrDefaultAsync(l => l.ProjectId == projectId && l.VariableSetId == setId);
        removedLink.Should().BeNull();
    }

    [Fact]
    public async Task Handle_LinkDoesNotExist_ReturnsSuccessfully()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var setId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var command = new UnlinkVariableSetFromProject(projectId, setId);
        var handler = new UnlinkVariableSetFromProjectHandler(db, _loggerMock.Object);

        // Act & Assert - should not throw
        await handler.Handle(command, CancellationToken.None);
    }

    [Fact]
    public async Task Handle_UnlinkOneOfMultipleLinks_OthersRemainIntact()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var set1Id = Guid.NewGuid();
        var set2Id = Guid.NewGuid();
        var set3Id = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var set1 = TestDataBuilder.CreateVariableSet(id: set1Id, name: "Set 1");
        var set2 = TestDataBuilder.CreateVariableSet(id: set2Id, name: "Set 2");
        var set3 = TestDataBuilder.CreateVariableSet(id: set3Id, name: "Set 3");
        db.VariableSets.AddRange(set1, set2, set3);

        var link1 = TestDataBuilder.CreateLink(projectId, set1Id);
        var link2 = TestDataBuilder.CreateLink(projectId, set2Id);
        var link3 = TestDataBuilder.CreateLink(projectId, set3Id);
        db.ProjectVariableSetLinks.AddRange(link1, link2, link3);
        await db.SaveChangesAsync();

        var command = new UnlinkVariableSetFromProject(projectId, set2Id);
        var handler = new UnlinkVariableSetFromProjectHandler(db, _loggerMock.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var remainingLinks = await db.ProjectVariableSetLinks
            .Where(l => l.ProjectId == projectId)
            .ToListAsync();

        remainingLinks.Should().HaveCount(2);
        remainingLinks.Should().Contain(l => l.VariableSetId == set1Id);
        remainingLinks.Should().Contain(l => l.VariableSetId == set3Id);
        remainingLinks.Should().NotContain(l => l.VariableSetId == set2Id);
    }

    [Fact]
    public async Task Handle_UnlinkFromOneProject_OtherProjectLinksRemain()
    {
        // Arrange
        var project1Id = Guid.NewGuid();
        var project2Id = Guid.NewGuid();
        var setId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variableSet = TestDataBuilder.CreateVariableSet(id: setId, name: "Shared Set");
        db.VariableSets.Add(variableSet);

        var link1 = TestDataBuilder.CreateLink(project1Id, setId);
        var link2 = TestDataBuilder.CreateLink(project2Id, setId);
        db.ProjectVariableSetLinks.AddRange(link1, link2);
        await db.SaveChangesAsync();

        var command = new UnlinkVariableSetFromProject(project1Id, setId);
        var handler = new UnlinkVariableSetFromProjectHandler(db, _loggerMock.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var allLinks = await db.ProjectVariableSetLinks.ToListAsync();
        allLinks.Should().HaveCount(1);
        allLinks[0].ProjectId.Should().Be(project2Id);
        allLinks[0].VariableSetId.Should().Be(setId);
    }

    [Fact]
    public async Task Handle_LogsInformation_OnSuccess()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var setId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variableSet = TestDataBuilder.CreateVariableSet(id: setId, name: "Test Set");
        db.VariableSets.Add(variableSet);

        var link = TestDataBuilder.CreateLink(projectId, setId);
        db.ProjectVariableSetLinks.Add(link);
        await db.SaveChangesAsync();

        var command = new UnlinkVariableSetFromProject(projectId, setId);
        var handler = new UnlinkVariableSetFromProjectHandler(db, _loggerMock.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unlinking variable set")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unlinked variable set")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_LogsInformation_WhenLinkNotFound()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var setId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var command = new UnlinkVariableSetFromProject(projectId, setId);
        var handler = new UnlinkVariableSetFromProjectHandler(db, _loggerMock.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Link between project") && v.ToString()!.Contains("not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
