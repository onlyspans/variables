using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Onlyspans.Variables.Api.Data.Entities;
using Onlyspans.Variables.Api.Features.VariableSets;
using Onlyspans.Variables.Api.Tests.Helpers;

namespace Onlyspans.Variables.Api.Tests.Features;

public class UnlinkVariableSetFromProjectHandlerTests
{
    [Fact]
    public async Task Handle_ExistingLink_UnlinksSuccessfully()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var setId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variableSet = new VariableSet { Id = setId, Name = "Test Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.VariableSets.Add(variableSet);

        var link = new ProjectVariableSetLink { ProjectId = projectId, VariableSetId = setId, LinkedAt = DateTime.UtcNow };
        db.ProjectVariableSetLinks.Add(link);
        await db.SaveChangesAsync();

        var command = new UnlinkVariableSetFromProject(projectId, setId);
        var handler = new UnlinkVariableSetFromProjectHandler(db, NullLogger<UnlinkVariableSetFromProjectHandler>.Instance);

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
        var handler = new UnlinkVariableSetFromProjectHandler(db, NullLogger<UnlinkVariableSetFromProjectHandler>.Instance);

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

        var set1 = new VariableSet { Id = set1Id, Name = "Set 1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var set2 = new VariableSet { Id = set2Id, Name = "Set 2", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var set3 = new VariableSet { Id = set3Id, Name = "Set 3", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.VariableSets.AddRange(set1, set2, set3);

        var link1 = new ProjectVariableSetLink { ProjectId = projectId, VariableSetId = set1Id, LinkedAt = DateTime.UtcNow };
        var link2 = new ProjectVariableSetLink { ProjectId = projectId, VariableSetId = set2Id, LinkedAt = DateTime.UtcNow };
        var link3 = new ProjectVariableSetLink { ProjectId = projectId, VariableSetId = set3Id, LinkedAt = DateTime.UtcNow };
        db.ProjectVariableSetLinks.AddRange(link1, link2, link3);
        await db.SaveChangesAsync();

        var command = new UnlinkVariableSetFromProject(projectId, set2Id);
        var handler = new UnlinkVariableSetFromProjectHandler(db, NullLogger<UnlinkVariableSetFromProjectHandler>.Instance);

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

        var variableSet = new VariableSet { Id = setId, Name = "Shared Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.VariableSets.Add(variableSet);

        var link1 = new ProjectVariableSetLink { ProjectId = project1Id, VariableSetId = setId, LinkedAt = DateTime.UtcNow };
        var link2 = new ProjectVariableSetLink { ProjectId = project2Id, VariableSetId = setId, LinkedAt = DateTime.UtcNow };
        db.ProjectVariableSetLinks.AddRange(link1, link2);
        await db.SaveChangesAsync();

        var command = new UnlinkVariableSetFromProject(project1Id, setId);
        var handler = new UnlinkVariableSetFromProjectHandler(db, NullLogger<UnlinkVariableSetFromProjectHandler>.Instance);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var allLinks = await db.ProjectVariableSetLinks.ToListAsync();
        allLinks.Should().HaveCount(1);
        allLinks[0].ProjectId.Should().Be(project2Id);
        allLinks[0].VariableSetId.Should().Be(setId);
    }
}
