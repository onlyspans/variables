using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Onlyspans.Variables.Api.Abstractions.Services;
using Onlyspans.Variables.Api.Features.VariableSets;
using Onlyspans.Variables.Api.Tests.Helpers;

namespace Onlyspans.Variables.Api.Tests.Features;

public class LinkVariableSetToProjectHandlerTests
{
    private readonly Mock<ILogger<LinkVariableSetToProjectHandler>> _loggerMock;
    private readonly Mock<IProjectsClient> _projectsClientMock;

    public LinkVariableSetToProjectHandlerTests()
    {
        _loggerMock = new Mock<ILogger<LinkVariableSetToProjectHandler>>();
        _projectsClientMock = new Mock<IProjectsClient>();
    }

    [Fact]
    public async Task Handle_ValidRequest_LinksSuccessfully()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var setId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variableSet = TestDataBuilder.CreateVariableSet(id: setId, name: "Test Set");
        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync();

        _projectsClientMock
            .Setup(x => x.ProjectExistsAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new LinkVariableSetToProject(projectId, setId);
        var handler = new LinkVariableSetToProjectHandler(db, _projectsClientMock.Object, _loggerMock.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var link = await db.ProjectVariableSetLinks
            .FirstOrDefaultAsync(l => l.ProjectId == projectId && l.VariableSetId == setId);
        link.Should().NotBeNull();
        link!.ProjectId.Should().Be(projectId);
        link.VariableSetId.Should().Be(setId);
    }

    [Fact]
    public async Task Handle_ProjectDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var setId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variableSet = TestDataBuilder.CreateVariableSet(id: setId, name: "Test Set");
        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync();

        _projectsClientMock
            .Setup(x => x.ProjectExistsAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new LinkVariableSetToProject(projectId, setId);
        var handler = new LinkVariableSetToProjectHandler(db, _projectsClientMock.Object, _loggerMock.Object);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Project {projectId} does not exist");
    }

    [Fact]
    public async Task Handle_VariableSetDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var nonExistentSetId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        _projectsClientMock
            .Setup(x => x.ProjectExistsAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new LinkVariableSetToProject(projectId, nonExistentSetId);
        var handler = new LinkVariableSetToProjectHandler(db, _projectsClientMock.Object, _loggerMock.Object);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Variable set {nonExistentSetId} does not exist");
    }

    [Fact]
    public async Task Handle_LinkAlreadyExists_ReturnsSuccessfully()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var setId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variableSet = TestDataBuilder.CreateVariableSet(id: setId, name: "Test Set");
        db.VariableSets.Add(variableSet);

        var existingLink = TestDataBuilder.CreateLink(projectId, setId);
        db.ProjectVariableSetLinks.Add(existingLink);
        await db.SaveChangesAsync();

        _projectsClientMock
            .Setup(x => x.ProjectExistsAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new LinkVariableSetToProject(projectId, setId);
        var handler = new LinkVariableSetToProjectHandler(db, _projectsClientMock.Object, _loggerMock.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert - should not create duplicate
        var linkCount = await db.ProjectVariableSetLinks
            .CountAsync(l => l.ProjectId == projectId && l.VariableSetId == setId);
        linkCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_MultipleProjectsLinkedToSameSet_AllLinksExist()
    {
        // Arrange
        var project1Id = Guid.NewGuid();
        var project2Id = Guid.NewGuid();
        var setId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variableSet = TestDataBuilder.CreateVariableSet(id: setId, name: "Shared Set");
        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync();

        _projectsClientMock
            .Setup(x => x.ProjectExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new LinkVariableSetToProjectHandler(db, _projectsClientMock.Object, _loggerMock.Object);

        // Act
        await handler.Handle(new LinkVariableSetToProject(project1Id, setId), CancellationToken.None);
        await handler.Handle(new LinkVariableSetToProject(project2Id, setId), CancellationToken.None);

        // Assert
        var links = await db.ProjectVariableSetLinks.Where(l => l.VariableSetId == setId).ToListAsync();
        links.Should().HaveCount(2);
        links.Should().Contain(l => l.ProjectId == project1Id);
        links.Should().Contain(l => l.ProjectId == project2Id);
    }

    [Fact]
    public async Task Handle_MultipleVariableSetsLinkedToSameProject_AllLinksExist()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var set1Id = Guid.NewGuid();
        var set2Id = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var set1 = TestDataBuilder.CreateVariableSet(id: set1Id, name: "Set 1");
        var set2 = TestDataBuilder.CreateVariableSet(id: set2Id, name: "Set 2");
        db.VariableSets.AddRange(set1, set2);
        await db.SaveChangesAsync();

        _projectsClientMock
            .Setup(x => x.ProjectExistsAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new LinkVariableSetToProjectHandler(db, _projectsClientMock.Object, _loggerMock.Object);

        // Act
        await handler.Handle(new LinkVariableSetToProject(projectId, set1Id), CancellationToken.None);
        await handler.Handle(new LinkVariableSetToProject(projectId, set2Id), CancellationToken.None);

        // Assert
        var links = await db.ProjectVariableSetLinks.Where(l => l.ProjectId == projectId).ToListAsync();
        links.Should().HaveCount(2);
        links.Should().Contain(l => l.VariableSetId == set1Id);
        links.Should().Contain(l => l.VariableSetId == set2Id);
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
        await db.SaveChangesAsync();

        _projectsClientMock
            .Setup(x => x.ProjectExistsAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new LinkVariableSetToProject(projectId, setId);
        var handler = new LinkVariableSetToProjectHandler(db, _projectsClientMock.Object, _loggerMock.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Linking variable set")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Linked variable set")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SetsLinkedAtTimestamp()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var setId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variableSet = TestDataBuilder.CreateVariableSet(id: setId, name: "Test Set");
        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync();

        _projectsClientMock
            .Setup(x => x.ProjectExistsAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new LinkVariableSetToProject(projectId, setId);
        var handler = new LinkVariableSetToProjectHandler(db, _projectsClientMock.Object, _loggerMock.Object);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var link = await db.ProjectVariableSetLinks.FirstAsync(l => l.ProjectId == projectId && l.VariableSetId == setId);
        link.LinkedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }
}
