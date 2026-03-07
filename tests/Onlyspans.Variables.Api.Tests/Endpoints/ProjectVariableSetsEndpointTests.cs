using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Onlyspans.Variables.Api.Data.Entities;
using Onlyspans.Variables.Api.Data.Records;

namespace Onlyspans.Variables.Api.Tests.Endpoints;

public class ProjectVariableSetsEndpointTests : IntegrationTestBase
{
    [Fact]
    public async Task GetProjectVariableSets_ReturnsEmptyList_WhenNoLinksExist()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/projects/{projectId}/variable-sets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var sets = await response.Content.ReadFromJsonAsync<List<VariableSetResponse>>();
        sets.Should().NotBeNull();
        sets.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProjectVariableSets_ReturnsLinkedSets_WhenLinksExist()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var set1Id = Guid.NewGuid();
        var set2Id = Guid.NewGuid();
        var set3Id = Guid.NewGuid();

        await SeedDatabaseAsync(db =>
        {
            // Create variable sets
            db.VariableSets.Add(new VariableSet { Id = set1Id, Name = "Development Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            db.VariableSets.Add(new VariableSet { Id = set2Id, Name = "Production Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            db.VariableSets.Add(new VariableSet { Id = set3Id, Name = "Unlinked Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });

            // Link only set1 and set2 to the project
            db.ProjectVariableSetLinks.Add(new ProjectVariableSetLink { ProjectId = projectId, VariableSetId = set1Id, LinkedAt = DateTime.UtcNow });
            db.ProjectVariableSetLinks.Add(new ProjectVariableSetLink { ProjectId = projectId, VariableSetId = set2Id, LinkedAt = DateTime.UtcNow });
        });

        // Act
        var response = await Client.GetAsync($"/api/projects/{projectId}/variable-sets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var sets = await response.Content.ReadFromJsonAsync<List<VariableSetResponse>>();
        sets.Should().NotBeNull();
        sets.Should().HaveCount(2);
        sets.Should().Contain(s => s.Name == "Development Set");
        sets.Should().Contain(s => s.Name == "Production Set");
        sets.Should().NotContain(s => s.Name == "Unlinked Set");
    }

    [Fact]
    public async Task GetProjectVariableSets_WithInvalidProjectGuid_ReturnsNotFound()
    {
        // Act - Invalid GUID in route results in 404 (route not matched)
        var response = await Client.GetAsync("/api/projects/invalid-guid/variable-sets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LinkVariableSetToProject_WithValidIds_ReturnsNoContent()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var setId = Guid.NewGuid();

        await SeedDatabaseAsync(db =>
        {
            db.VariableSets.Add(new VariableSet { Id = setId, Name = "Test Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        });

        // Act
        var response = await Client.PostAsync($"/api/projects/{projectId}/variable-sets/{setId}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify link was created in database
        var link = await DbContext.ProjectVariableSetLinks.FindAsync(projectId, setId);
        link.Should().NotBeNull();
        link!.ProjectId.Should().Be(projectId);
        link.VariableSetId.Should().Be(setId);
    }

    [Fact]
    public async Task LinkVariableSetToProject_WithNonExistentSet_ReturnsNotFound()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var nonExistentSetId = Guid.NewGuid();

        // Act
        var response = await Client.PostAsync($"/api/projects/{projectId}/variable-sets/{nonExistentSetId}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LinkVariableSetToProject_WhenAlreadyLinked_ReturnsSuccessfully()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var setId = Guid.NewGuid();

        await SeedDatabaseAsync(db =>
        {
            db.VariableSets.Add(new VariableSet { Id = setId, Name = "Test Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            db.ProjectVariableSetLinks.Add(new ProjectVariableSetLink { ProjectId = projectId, VariableSetId = setId, LinkedAt = DateTime.UtcNow });
        });

        // Act
        var response = await Client.PostAsync($"/api/projects/{projectId}/variable-sets/{setId}", null);

        // Assert - idempotent operation should succeed
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify no duplicate created
        var linkCount = await DbContext.ProjectVariableSetLinks
            .CountAsync(l => l.ProjectId == projectId && l.VariableSetId == setId);
        linkCount.Should().Be(1);
    }

    [Fact]
    public async Task LinkVariableSetToProject_WithInvalidProjectGuid_ReturnsNotFound()
    {
        // Arrange
        var setId = Guid.NewGuid();

        // Act - Invalid GUID in route results in 404 (route not matched)
        var response = await Client.PostAsync($"/api/projects/invalid-guid/variable-sets/{setId}", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LinkVariableSetToProject_WithInvalidSetGuid_ReturnsNotFound()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        // Act - Invalid GUID in route results in 404 (route not matched)
        var response = await Client.PostAsync($"/api/projects/{projectId}/variable-sets/invalid-guid", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnlinkVariableSetFromProject_WithExistingLink_ReturnsNoContent()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var setId = Guid.NewGuid();

        await SeedDatabaseAsync(db =>
        {
            db.VariableSets.Add(new VariableSet { Id = setId, Name = "Test Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            db.ProjectVariableSetLinks.Add(new ProjectVariableSetLink { ProjectId = projectId, VariableSetId = setId, LinkedAt = DateTime.UtcNow });
        });

        // Act
        var response = await Client.DeleteAsync($"/api/projects/{projectId}/variable-sets/{setId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify link was deleted from database
        var link = await DbContext.ProjectVariableSetLinks.FindAsync(projectId, setId);
        link.Should().BeNull();
    }

    [Fact]
    public async Task UnlinkVariableSetFromProject_WithNonExistentLink_ReturnsNoContent()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var setId = Guid.NewGuid();

        await SeedDatabaseAsync(db =>
        {
            db.VariableSets.Add(new VariableSet { Id = setId, Name = "Test Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        });

        // Act
        var response = await Client.DeleteAsync($"/api/projects/{projectId}/variable-sets/{setId}");

        // Assert - Idempotent behavior: returns success even if link doesn't exist
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UnlinkVariableSetFromProject_WithInvalidProjectGuid_ReturnsNotFound()
    {
        // Arrange
        var setId = Guid.NewGuid();

        // Act - Invalid GUID in route results in 404 (route not matched)
        var response = await Client.DeleteAsync($"/api/projects/invalid-guid/variable-sets/{setId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnlinkVariableSetFromProject_WithInvalidSetGuid_ReturnsNotFound()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        // Act - Invalid GUID in route results in 404 (route not matched)
        var response = await Client.DeleteAsync($"/api/projects/{projectId}/variable-sets/invalid-guid");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LinkVariableSetToProject_MultipleSets_CreatesMultipleLinks()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var set1Id = Guid.NewGuid();
        var set2Id = Guid.NewGuid();
        var set3Id = Guid.NewGuid();

        await SeedDatabaseAsync(db =>
        {
            db.VariableSets.Add(new VariableSet { Id = set1Id, Name = "Set 1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            db.VariableSets.Add(new VariableSet { Id = set2Id, Name = "Set 2", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            db.VariableSets.Add(new VariableSet { Id = set3Id, Name = "Set 3", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        });

        // Act - Link multiple sets
        var response1 = await Client.PostAsync($"/api/projects/{projectId}/variable-sets/{set1Id}", null);
        var response2 = await Client.PostAsync($"/api/projects/{projectId}/variable-sets/{set2Id}", null);
        var response3 = await Client.PostAsync($"/api/projects/{projectId}/variable-sets/{set3Id}", null);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response2.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response3.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify all links exist
        var getResponse = await Client.GetAsync($"/api/projects/{projectId}/variable-sets");
        var sets = await getResponse.Content.ReadFromJsonAsync<List<VariableSetResponse>>();
        sets.Should().HaveCount(3);
    }

    [Fact]
    public async Task UnlinkVariableSetFromProject_DoesNotAffectOtherProjects()
    {
        // Arrange
        var project1Id = Guid.NewGuid();
        var project2Id = Guid.NewGuid();
        var setId = Guid.NewGuid();

        await SeedDatabaseAsync(db =>
        {
            db.VariableSets.Add(new VariableSet { Id = setId, Name = "Test Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            db.ProjectVariableSetLinks.Add(new ProjectVariableSetLink { ProjectId = project1Id, VariableSetId = setId, LinkedAt = DateTime.UtcNow });
            db.ProjectVariableSetLinks.Add(new ProjectVariableSetLink { ProjectId = project2Id, VariableSetId = setId, LinkedAt = DateTime.UtcNow });
        });

        // Act - Unlink from project1
        var response = await Client.DeleteAsync($"/api/projects/{project1Id}/variable-sets/{setId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify project1 link is deleted but project2 link remains
        var link1 = await DbContext.ProjectVariableSetLinks.FindAsync(project1Id, setId);
        link1.Should().BeNull();

        var link2 = await DbContext.ProjectVariableSetLinks.FindAsync(project2Id, setId);
        link2.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProjectVariableSets_ReturnsOnlySetsForSpecificProject()
    {
        // Arrange
        var project1Id = Guid.NewGuid();
        var project2Id = Guid.NewGuid();
        var set1Id = Guid.NewGuid();
        var set2Id = Guid.NewGuid();

        await SeedDatabaseAsync(db =>
        {
            db.VariableSets.Add(new VariableSet { Id = set1Id, Name = "Project 1 Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            db.VariableSets.Add(new VariableSet { Id = set2Id, Name = "Project 2 Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            db.ProjectVariableSetLinks.Add(new ProjectVariableSetLink { ProjectId = project1Id, VariableSetId = set1Id, LinkedAt = DateTime.UtcNow });
            db.ProjectVariableSetLinks.Add(new ProjectVariableSetLink { ProjectId = project2Id, VariableSetId = set2Id, LinkedAt = DateTime.UtcNow });
        });

        // Act
        var response = await Client.GetAsync($"/api/projects/{project1Id}/variable-sets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var sets = await response.Content.ReadFromJsonAsync<List<VariableSetResponse>>();
        sets.Should().NotBeNull();
        sets.Should().HaveCount(1);
        sets![0].Name.Should().Be("Project 1 Set");
    }
}
