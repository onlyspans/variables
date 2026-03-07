using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Onlyspans.Variables.Api.Data.Entities;
using Onlyspans.Variables.Api.Data.Records;

namespace Onlyspans.Variables.Api.Tests.Endpoints;

public class VariablesEndpointTests : IntegrationTestBase
{
    [Fact]
    public async Task GetProjectVariables_ReturnsEmptyList_WhenNoVariablesExist()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/projects/{projectId}/variables");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var variables = await response.Content.ReadFromJsonAsync<List<VariableResponse>>();
        variables.Should().NotBeNull();
        variables.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProjectVariables_ReturnsVariables_WhenVariablesExist()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        await SeedDatabaseAsync(db =>
        {
            db.Variables.Add(new Variable { Id = Guid.NewGuid(), Key = "API_KEY", Value = "secret123", ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            db.Variables.Add(new Variable { Id = Guid.NewGuid(), Key = "DATABASE_URL", Value = "postgres://localhost", ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
            // Variable for different project should not be returned
            db.Variables.Add(new Variable { Id = Guid.NewGuid(), Key = "OTHER_VAR", Value = "other", ProjectId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        });

        // Act
        var response = await Client.GetAsync($"/api/projects/{projectId}/variables");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var variables = await response.Content.ReadFromJsonAsync<List<VariableResponse>>();
        variables.Should().NotBeNull();
        variables.Should().HaveCount(2);
        variables.Should().Contain(v => v.Key == "API_KEY" && v.Value == "secret123");
        variables.Should().Contain(v => v.Key == "DATABASE_URL");
    }

    [Fact]
    public async Task GetProjectVariables_WithInvalidGuid_ReturnsNotFound()
    {
        // Act - Invalid GUID in route results in 404 (route not matched)
        var response = await Client.GetAsync("/api/projects/invalid-guid/variables");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateVariable_WithValidData_ReturnsCreated()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var request = new CreateVariableRequest(
            Key: "NEW_VAR",
            Value: "new_value",
            EnvironmentId: Guid.NewGuid(),
            VariableSetId: null
        );

        // Act
        var response = await Client.PostAsJsonAsync($"/api/projects/{projectId}/variables", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var variable = await response.Content.ReadFromJsonAsync<VariableResponse>();
        variable.Should().NotBeNull();
        variable!.Key.Should().Be("NEW_VAR");
        variable.Value.Should().Be("new_value");
        variable.ProjectId.Should().Be(projectId);
        variable.Id.Should().NotBeEmpty();

        // Verify location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/api/variables/{variable.Id}");

        // Verify variable was saved to database
        var savedVariable = await DbContext.Variables.FindAsync(variable.Id);
        savedVariable.Should().NotBeNull();
        savedVariable!.Key.Should().Be("NEW_VAR");
    }

    [Fact]
    public async Task CreateVariable_WithEmptyKey_ReturnsBadRequest()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var request = new CreateVariableRequest(
            Key: "",
            Value: "value"
        );

        // Act
        var response = await Client.PostAsJsonAsync($"/api/projects/{projectId}/variables", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problemDetails = await response.Content.ReadAsStringAsync();
        problemDetails.Should().Contain("Key");
    }

    [Fact]
    public async Task CreateVariable_WithEmptyValue_ReturnsBadRequest()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var request = new CreateVariableRequest(
            Key: "VALID_KEY",
            Value: ""
        );

        // Act
        var response = await Client.PostAsJsonAsync($"/api/projects/{projectId}/variables", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problemDetails = await response.Content.ReadAsStringAsync();
        problemDetails.Should().Contain("Value");
    }

    [Fact]
    public async Task CreateVariable_WithInvalidProjectGuid_ReturnsNotFound()
    {
        // Arrange
        var request = new CreateVariableRequest(
            Key: "KEY",
            Value: "value"
        );

        // Act - Invalid GUID in route results in 404 (route not matched)
        var response = await Client.PostAsJsonAsync("/api/projects/invalid-guid/variables", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateVariable_WithValidData_ReturnsOk()
    {
        // Arrange
        var variable = new Variable { Id = Guid.NewGuid(), Key = "OLD_KEY", Value = "old_value", ProjectId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        await SeedDatabaseAsync(db => db.Variables.Add(variable));

        var request = new UpdateVariableRequest(
            Key: "NEW_KEY",
            Value: "new_value",
            EnvironmentId: Guid.NewGuid()
        );

        // Act
        var response = await Client.PutAsJsonAsync($"/api/variables/{variable.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<VariableResponse>();
        updated.Should().NotBeNull();
        updated!.Key.Should().Be("NEW_KEY");
        updated.Value.Should().Be("new_value");
        updated.EnvironmentId.Should().NotBeNull();

        // Verify update in database
        var dbVariable = await DbContext.Variables.FindAsync(variable.Id);
        dbVariable.Should().NotBeNull();
        dbVariable!.Key.Should().Be("NEW_KEY");
        dbVariable.Value.Should().Be("new_value");
    }

    [Fact]
    public async Task UpdateVariable_PartialUpdate_UpdatesOnlyProvidedFields()
    {
        // Arrange
        var variable = new Variable { Id = Guid.NewGuid(), Key = "ORIGINAL_KEY", Value = "original_value", ProjectId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        await SeedDatabaseAsync(db => db.Variables.Add(variable));

        var request = new UpdateVariableRequest(
            Value: "new_value_only"
        );

        // Act
        var response = await Client.PutAsJsonAsync($"/api/variables/{variable.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<VariableResponse>();
        updated.Should().NotBeNull();
        updated!.Key.Should().Be("ORIGINAL_KEY"); // Should remain unchanged
        updated.Value.Should().Be("new_value_only");
    }

    [Fact]
    public async Task UpdateVariable_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var request = new UpdateVariableRequest(
            Key: "KEY",
            Value: "value"
        );

        // Act
        var response = await Client.PutAsJsonAsync($"/api/variables/{nonExistentId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateVariable_WithInvalidGuid_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateVariableRequest(
            Key: "KEY",
            Value: "value"
        );

        // Act - Invalid GUID in route results in 404 (route not matched)
        var response = await Client.PutAsJsonAsync("/api/variables/invalid-guid", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateVariable_WithEmptyKey_ReturnsBadRequest()
    {
        // Arrange
        var variable = new Variable { Id = Guid.NewGuid(), Key = "TEST_KEY", Value = "TEST_VALUE", ProjectId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        await SeedDatabaseAsync(db => db.Variables.Add(variable));

        var request = new UpdateVariableRequest(
            Key: ""
        );

        // Act
        var response = await Client.PutAsJsonAsync($"/api/variables/{variable.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteVariable_WithExistingId_ReturnsNoContent()
    {
        // Arrange
        var variable = new Variable { Id = Guid.NewGuid(), Key = "TEST_KEY", Value = "TEST_VALUE", ProjectId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        await SeedDatabaseAsync(db => db.Variables.Add(variable));

        // Act
        var response = await Client.DeleteAsync($"/api/variables/{variable.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion in database
        var dbVariable = await DbContext.Variables.FindAsync(variable.Id);
        dbVariable.Should().BeNull();
    }

    [Fact]
    public async Task DeleteVariable_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/variables/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteVariable_WithInvalidGuid_ReturnsNotFound()
    {
        // Act - Invalid GUID in route results in 404 (route not matched)
        var response = await Client.DeleteAsync("/api/variables/invalid-guid");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateVariable_InVariableSet_AssociatesCorrectly()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var variableSetId = Guid.NewGuid();

        await SeedDatabaseAsync(db =>
        {
            db.VariableSets.Add(new VariableSet { Id = variableSetId, Name = "Test Set", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        });

        var request = new CreateVariableRequest(
            Key: "SET_VAR",
            Value: "set_value",
            VariableSetId: variableSetId
        );

        // Act
        var response = await Client.PostAsJsonAsync($"/api/projects/{projectId}/variables", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var variable = await response.Content.ReadFromJsonAsync<VariableResponse>();
        variable.Should().NotBeNull();
        variable!.VariableSetId.Should().Be(variableSetId);
        variable.ProjectId.Should().Be(projectId);
    }
}
