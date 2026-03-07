using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Onlyspans.Variables.Api.Abstractions.Services;
using Onlyspans.Variables.Api.Data.Entities;
using Onlyspans.Variables.Api.Features.VariableSets;
using Onlyspans.Variables.Api.Tests.Helpers;

namespace Onlyspans.Variables.Api.Tests.Features;

public class LinkVariableSetToProjectHandlerTests
{
    private readonly IProjectsClient _projectsClientMock = Substitute.For<IProjectsClient>();

    [Fact]
    public async Task Handle_ValidRequest_LinksSuccessfully()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var setId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variableSet = new VariableSet { Id = setId, Name = "Test Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync();

        _projectsClientMock.ProjectExistsAsync(projectId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        var command = new LinkVariableSetToProject(projectId, setId);
        var handler = new LinkVariableSetToProjectHandler(db, _projectsClientMock, NullLogger<LinkVariableSetToProjectHandler>.Instance);

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

        var variableSet = new VariableSet { Id = setId, Name = "Test Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync();

        _projectsClientMock.ProjectExistsAsync(projectId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));

        var command = new LinkVariableSetToProject(projectId, setId);
        var handler = new LinkVariableSetToProjectHandler(db, _projectsClientMock, NullLogger<LinkVariableSetToProjectHandler>.Instance);

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

        _projectsClientMock.ProjectExistsAsync(projectId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        var command = new LinkVariableSetToProject(projectId, nonExistentSetId);
        var handler = new LinkVariableSetToProjectHandler(db, _projectsClientMock, NullLogger<LinkVariableSetToProjectHandler>.Instance);

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

        var variableSet = new VariableSet { Id = setId, Name = "Test Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.VariableSets.Add(variableSet);

        var existingLink = new ProjectVariableSetLink { ProjectId = projectId, VariableSetId = setId, LinkedAt = DateTime.UtcNow };
        db.ProjectVariableSetLinks.Add(existingLink);
        await db.SaveChangesAsync();

        _projectsClientMock.ProjectExistsAsync(projectId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        var command = new LinkVariableSetToProject(projectId, setId);
        var handler = new LinkVariableSetToProjectHandler(db, _projectsClientMock, NullLogger<LinkVariableSetToProjectHandler>.Instance);

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

        var variableSet = new VariableSet { Id = setId, Name = "Shared Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync();

        _projectsClientMock.ProjectExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        var handler = new LinkVariableSetToProjectHandler(db, _projectsClientMock, NullLogger<LinkVariableSetToProjectHandler>.Instance);

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

        var set1 = new VariableSet { Id = set1Id, Name = "Set 1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var set2 = new VariableSet { Id = set2Id, Name = "Set 2", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.VariableSets.AddRange(set1, set2);
        await db.SaveChangesAsync();

        _projectsClientMock.ProjectExistsAsync(projectId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        var handler = new LinkVariableSetToProjectHandler(db, _projectsClientMock, NullLogger<LinkVariableSetToProjectHandler>.Instance);

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
    public async Task Handle_SetsLinkedAtTimestamp()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var setId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variableSet = new VariableSet { Id = setId, Name = "Test Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync();

        _projectsClientMock.ProjectExistsAsync(projectId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        var command = new LinkVariableSetToProject(projectId, setId);
        var handler = new LinkVariableSetToProjectHandler(db, _projectsClientMock, NullLogger<LinkVariableSetToProjectHandler>.Instance);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var link = await db.ProjectVariableSetLinks.FirstAsync(l => l.ProjectId == projectId && l.VariableSetId == setId);
        link.LinkedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }
}
