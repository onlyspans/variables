using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Onlyspans.Variables.Api.Data.Records;
using Onlyspans.Variables.Api.Tests.Helpers;

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
            db.VariableSets.Add(TestDataBuilder.CreateVariableSet(id: set1Id, name: "Development Set"));
            db.VariableSets.Add(TestDataBuilder.CreateVariableSet(id: set2Id, name: "Production Set"));
            db.VariableSets.Add(TestDataBuilder.CreateVariableSet(id: set3Id, name: "Unlinked Set"));

            // Link only set1 and set2 to the project
            db.ProjectVariableSetLinks.Add(TestDataBuilder.CreateLink(projectId, set1Id));
            db.ProjectVariableSetLinks.Add(TestDataBuilder.CreateLink(projectId, set2Id));
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
            db.VariableSets.Add(TestDataBuilder.CreateVariableSet(id: setId, name: "Test Set"));
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
            db.VariableSets.Add(TestDataBuilder.CreateVariableSet(id: setId));
            db.ProjectVariableSetLinks.Add(TestDataBuilder.CreateLink(projectId, setId));
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
            db.VariableSets.Add(TestDataBuilder.CreateVariableSet(id: setId));
            db.ProjectVariableSetLinks.Add(TestDataBuilder.CreateLink(projectId, setId));
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
            db.VariableSets.Add(TestDataBuilder.CreateVariableSet(id: setId));
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
            db.VariableSets.Add(TestDataBuilder.CreateVariableSet(id: set1Id, name: "Set 1"));
            db.VariableSets.Add(TestDataBuilder.CreateVariableSet(id: set2Id, name: "Set 2"));
            db.VariableSets.Add(TestDataBuilder.CreateVariableSet(id: set3Id, name: "Set 3"));
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
            db.VariableSets.Add(TestDataBuilder.CreateVariableSet(id: setId));
            db.ProjectVariableSetLinks.Add(TestDataBuilder.CreateLink(project1Id, setId));
            db.ProjectVariableSetLinks.Add(TestDataBuilder.CreateLink(project2Id, setId));
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
            db.VariableSets.Add(TestDataBuilder.CreateVariableSet(id: set1Id, name: "Project 1 Set"));
            db.VariableSets.Add(TestDataBuilder.CreateVariableSet(id: set2Id, name: "Project 2 Set"));
            db.ProjectVariableSetLinks.Add(TestDataBuilder.CreateLink(project1Id, set1Id));
            db.ProjectVariableSetLinks.Add(TestDataBuilder.CreateLink(project2Id, set2Id));
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
