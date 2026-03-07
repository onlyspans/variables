using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Onlyspans.Variables.Api.Data.Entities;
using Onlyspans.Variables.Api.Features.VariableSets;
using Onlyspans.Variables.Api.Tests.Helpers;

namespace Onlyspans.Variables.Api.Tests.Features;

public class GetProjectVariableSetsHandlerTests
{
    [Fact]
    public async Task Handle_ProjectWithLinkedSets_ReturnsAllLinkedSets()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var set1Id = Guid.NewGuid();
        var set2Id = Guid.NewGuid();
        var set3Id = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var set1 = new VariableSet { Id = set1Id, Name = "Set 1", Description = "First", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var set2 = new VariableSet { Id = set2Id, Name = "Set 2", Description = "Second", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var set3 = new VariableSet { Id = set3Id, Name = "Set 3", Description = "Third", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.VariableSets.AddRange(set1, set2, set3);

        var link1 = new ProjectVariableSetLink { ProjectId = projectId, VariableSetId = set1Id, LinkedAt = DateTime.UtcNow };
        var link2 = new ProjectVariableSetLink { ProjectId = projectId, VariableSetId = set2Id, LinkedAt = DateTime.UtcNow };
        var link3 = new ProjectVariableSetLink { ProjectId = projectId, VariableSetId = set3Id, LinkedAt = DateTime.UtcNow };
        db.ProjectVariableSetLinks.AddRange(link1, link2, link3);
        await db.SaveChangesAsync();

        var query = new GetProjectVariableSets(projectId);
        var handler = new GetProjectVariableSetsHandler(db, NullLogger<GetProjectVariableSetsHandler>.Instance);

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
        var handler = new GetProjectVariableSetsHandler(db, NullLogger<GetProjectVariableSetsHandler>.Instance);

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

        var linkedSet = new VariableSet { Id = linkedSetId, Name = "Linked Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var unlinkedSet = new VariableSet { Id = unlinkedSetId, Name = "Unlinked Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.VariableSets.AddRange(linkedSet, unlinkedSet);

        var link = new ProjectVariableSetLink { ProjectId = projectId, VariableSetId = linkedSetId, LinkedAt = DateTime.UtcNow };
        db.ProjectVariableSetLinks.Add(link);
        await db.SaveChangesAsync();

        var query = new GetProjectVariableSets(projectId);
        var handler = new GetProjectVariableSetsHandler(db, NullLogger<GetProjectVariableSetsHandler>.Instance);

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

        var set1 = new VariableSet { Id = set1Id, Name = "P1 Set 1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var set2 = new VariableSet { Id = set2Id, Name = "P1 Set 2", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var set3 = new VariableSet { Id = set3Id, Name = "P2 Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.VariableSets.AddRange(set1, set2, set3);

        // Link sets 1 and 2 to project1, set 3 to project2
        db.ProjectVariableSetLinks.AddRange(
            new ProjectVariableSetLink { ProjectId = project1Id, VariableSetId = set1Id, LinkedAt = DateTime.UtcNow },
            new ProjectVariableSetLink { ProjectId = project1Id, VariableSetId = set2Id, LinkedAt = DateTime.UtcNow },
            new ProjectVariableSetLink { ProjectId = project2Id, VariableSetId = set3Id, LinkedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var query = new GetProjectVariableSets(project1Id);
        var handler = new GetProjectVariableSetsHandler(db, NullLogger<GetProjectVariableSetsHandler>.Instance);

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

        var variableSet = new VariableSet { Id = setId, Name = "Complete Set", Description = "Full description", CreatedAt = now, UpdatedAt = now };

        db.VariableSets.Add(variableSet);

        var link = new ProjectVariableSetLink { ProjectId = projectId, VariableSetId = setId, LinkedAt = DateTime.UtcNow };
        db.ProjectVariableSetLinks.Add(link);
        await db.SaveChangesAsync();

        var query = new GetProjectVariableSets(projectId);
        var handler = new GetProjectVariableSetsHandler(db, NullLogger<GetProjectVariableSetsHandler>.Instance);

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
}
