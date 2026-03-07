using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Onlyspans.Variables.Api.Data.Entities;
using Onlyspans.Variables.Api.Data.Records;

namespace Onlyspans.Variables.Api.Tests.Endpoints;

public class VariableSetsEndpointTests : IntegrationTestBase
{
    [Fact]
    public async Task GetVariableSets_ReturnsEmptyList_WhenNoSetsExist()
    {
        // Act
        var response = await Client.GetAsync("/api/variable-sets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var sets = await response.Content.ReadFromJsonAsync<List<VariableSetResponse>>();
        sets.Should().NotBeNull();
        sets.Should().BeEmpty();
    }

    [Fact]
    public async Task GetVariableSets_ReturnsSets_WhenSetsExist()
    {
        // Arrange
        await SeedDatabaseAsync(db =>
        {
            db.VariableSets.Add(new VariableSet { Id = Guid.NewGuid(), Name = "Development Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            db.VariableSets.Add(new VariableSet { Id = Guid.NewGuid(), Name = "Production Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            db.VariableSets.Add(new VariableSet { Id = Guid.NewGuid(), Name = "Staging Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        });

        // Act
        var response = await Client.GetAsync("/api/variable-sets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var sets = await response.Content.ReadFromJsonAsync<List<VariableSetResponse>>();
        sets.Should().NotBeNull();
        sets.Should().HaveCount(3);
        sets.Should().Contain(s => s.Name == "Development Set");
        sets.Should().Contain(s => s.Name == "Production Set");
        sets.Should().Contain(s => s.Name == "Staging Set");
    }

    [Fact]
    public async Task GetVariableSet_WithValidId_ReturnsSet()
    {
        // Arrange
        var setId = Guid.NewGuid();
        var variableId1 = Guid.NewGuid();
        var variableId2 = Guid.NewGuid();

        await SeedDatabaseAsync(db =>
        {
            db.VariableSets.Add(new VariableSet { Id = setId, Name = "Test Set", Description = "A test variable set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            db.Variables.Add(new Variable { Id = variableId1, Key = "VAR1", Value = "value1", VariableSetId = setId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            db.Variables.Add(new Variable { Id = variableId2, Key = "VAR2", Value = "value2", VariableSetId = setId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        });

        // Act
        var response = await Client.GetAsync($"/api/variable-sets/{setId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var set = await response.Content.ReadFromJsonAsync<VariableSetDetailResponse>();
        set.Should().NotBeNull();
        set!.Id.Should().Be(setId);
        set.Name.Should().Be("Test Set");
        set.Description.Should().Be("A test variable set");
        set.Variables.Should().HaveCount(2);
        set.Variables.Should().Contain(v => v.Key == "VAR1");
        set.Variables.Should().Contain(v => v.Key == "VAR2");
    }

    [Fact]
    public async Task GetVariableSet_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/variable-sets/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetVariableSet_WithInvalidGuid_ReturnsNotFound()
    {
        // Act - Invalid GUID in route results in 404 (route not matched)
        var response = await Client.GetAsync("/api/variable-sets/invalid-guid");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateVariableSet_WithValidData_ReturnsCreated()
    {
        // Arrange
        var request = new CreateVariableSetRequest(
            Name: "New Set",
            Description: "A new variable set"
        );

        // Act
        var response = await Client.PostAsJsonAsync("/api/variable-sets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var set = await response.Content.ReadFromJsonAsync<VariableSetResponse>();
        set.Should().NotBeNull();
        set!.Name.Should().Be("New Set");
        set.Description.Should().Be("A new variable set");
        set.Id.Should().NotBeEmpty();

        // Verify location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/api/variable-sets/{set.Id}");

        // Verify set was saved to database
        var savedSet = await DbContext.VariableSets.FindAsync(set.Id);
        savedSet.Should().NotBeNull();
        savedSet!.Name.Should().Be("New Set");
    }

    [Fact]
    public async Task CreateVariableSet_WithoutDescription_ReturnsCreated()
    {
        // Arrange
        var request = new CreateVariableSetRequest(
            Name: "Minimal Set"
        );

        // Act
        var response = await Client.PostAsJsonAsync("/api/variable-sets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var set = await response.Content.ReadFromJsonAsync<VariableSetResponse>();
        set.Should().NotBeNull();
        set!.Name.Should().Be("Minimal Set");
        set.Description.Should().BeNull();
    }

    [Fact]
    public async Task CreateVariableSet_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateVariableSetRequest(
            Name: ""
        );

        // Act
        var response = await Client.PostAsJsonAsync("/api/variable-sets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problemDetails = await response.Content.ReadAsStringAsync();
        problemDetails.Should().Contain("Name");
    }

    [Fact]
    public async Task UpdateVariableSet_WithValidData_ReturnsOk()
    {
        // Arrange
        var setId = Guid.NewGuid();
        await SeedDatabaseAsync(db =>
        {
            db.VariableSets.Add(new VariableSet { Id = setId, Name = "Old Name", Description = "Old Description", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        });

        var request = new UpdateVariableSetRequest(
            Name: "New Name",
            Description: "New Description"
        );

        // Act
        var response = await Client.PutAsJsonAsync($"/api/variable-sets/{setId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<VariableSetResponse>();
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("New Name");
        updated.Description.Should().Be("New Description");

        // Verify update in database
        var dbSet = await DbContext.VariableSets.FindAsync(setId);
        dbSet.Should().NotBeNull();
        dbSet!.Name.Should().Be("New Name");
        dbSet.Description.Should().Be("New Description");
    }

    [Fact]
    public async Task UpdateVariableSet_PartialUpdate_UpdatesOnlyProvidedFields()
    {
        // Arrange
        var setId = Guid.NewGuid();
        await SeedDatabaseAsync(db =>
        {
            db.VariableSets.Add(new VariableSet { Id = setId, Name = "Original Name", Description = "Original Description", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        });

        var request = new UpdateVariableSetRequest(
            Name: "Updated Name"
        );

        // Act
        var response = await Client.PutAsJsonAsync($"/api/variable-sets/{setId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<VariableSetResponse>();
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Name");
        updated.Description.Should().Be("Original Description"); // Should remain unchanged
    }

    [Fact]
    public async Task UpdateVariableSet_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var request = new UpdateVariableSetRequest(
            Name: "New Name"
        );

        // Act
        var response = await Client.PutAsJsonAsync($"/api/variable-sets/{nonExistentId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateVariableSet_WithInvalidGuid_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateVariableSetRequest(
            Name: "New Name"
        );

        // Act - Invalid GUID in route results in 404 (route not matched)
        var response = await Client.PutAsJsonAsync("/api/variable-sets/invalid-guid", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateVariableSet_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var setId = Guid.NewGuid();
        await SeedDatabaseAsync(db =>
        {
            db.VariableSets.Add(new VariableSet { Id = setId, Name = "Test Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        });

        var request = new UpdateVariableSetRequest(
            Name: ""
        );

        // Act
        var response = await Client.PutAsJsonAsync($"/api/variable-sets/{setId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteVariableSet_WithExistingId_ReturnsNoContent()
    {
        // Arrange
        var setId = Guid.NewGuid();
        await SeedDatabaseAsync(db =>
        {
            db.VariableSets.Add(new VariableSet { Id = setId, Name = "Test Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        });

        // Act
        var response = await Client.DeleteAsync($"/api/variable-sets/{setId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion in database
        var dbSet = await DbContext.VariableSets.FindAsync(setId);
        dbSet.Should().BeNull();
    }

    [Fact]
    public async Task DeleteVariableSet_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/variable-sets/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteVariableSet_WithInvalidGuid_ReturnsNotFound()
    {
        // Act - Invalid GUID in route results in 404 (route not matched)
        var response = await Client.DeleteAsync("/api/variable-sets/invalid-guid");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteVariableSet_WithLinkedProjects_CascadesCorrectly()
    {
        // Arrange
        var setId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        await SeedDatabaseAsync(db =>
        {
            db.VariableSets.Add(new VariableSet { Id = setId, Name = "Test Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            db.ProjectVariableSetLinks.Add(new ProjectVariableSetLink { ProjectId = projectId, VariableSetId = setId, LinkedAt = DateTime.UtcNow });
        });

        // Act
        var response = await Client.DeleteAsync($"/api/variable-sets/{setId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify cascade deletion of links
        var link = await DbContext.ProjectVariableSetLinks.FindAsync(projectId, setId);
        link.Should().BeNull();
    }

    [Fact]
    public async Task AddVariableToSet_WithValidData_ReturnsCreated()
    {
        // Arrange
        var setId = Guid.NewGuid();
        await SeedDatabaseAsync(db =>
        {
            db.VariableSets.Add(new VariableSet { Id = setId, Name = "Test Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        });

        var request = new CreateVariableRequest(
            Key: "SET_VAR",
            Value: "set_value"
        );

        // Act
        var response = await Client.PostAsJsonAsync($"/api/variable-sets/{setId}/variables", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var variable = await response.Content.ReadFromJsonAsync<VariableResponse>();
        variable.Should().NotBeNull();
        variable!.Key.Should().Be("SET_VAR");
        variable.Value.Should().Be("set_value");
        variable.VariableSetId.Should().Be(setId);
        variable.ProjectId.Should().BeNull(); // Variables in sets don't have project IDs

        // Verify location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/api/variables/{variable.Id}");
    }

    [Fact]
    public async Task AddVariableToSet_WithNonExistentSetId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentSetId = Guid.NewGuid();
        var request = new CreateVariableRequest(
            Key: "VAR",
            Value: "value"
        );

        // Act
        var response = await Client.PostAsJsonAsync($"/api/variable-sets/{nonExistentSetId}/variables", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddVariableToSet_WithInvalidSetGuid_ReturnsNotFound()
    {
        // Arrange
        var request = new CreateVariableRequest(
            Key: "VAR",
            Value: "value"
        );

        // Act - Invalid GUID in route results in 404 (route not matched)
        var response = await Client.PostAsJsonAsync("/api/variable-sets/invalid-guid/variables", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddVariableToSet_WithEmptyKey_ReturnsBadRequest()
    {
        // Arrange
        var setId = Guid.NewGuid();
        await SeedDatabaseAsync(db =>
        {
            db.VariableSets.Add(new VariableSet { Id = setId, Name = "Test Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        });

        var request = new CreateVariableRequest(
            Key: "",
            Value: "value"
        );

        // Act
        var response = await Client.PostAsJsonAsync($"/api/variable-sets/{setId}/variables", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
