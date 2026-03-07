using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Onlyspans.Variables.Api.Data.Entities;
using Onlyspans.Variables.Api.Data.Records;
using Onlyspans.Variables.Api.Features.Variables;
using Onlyspans.Variables.Api.Tests.Helpers;

namespace Onlyspans.Variables.Api.Tests.Features;

public class UpdateVariableHandlerTests
{
    [Fact]
    public async Task Handle_UpdateKey_UpdatesSuccessfully()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variable = new Variable { Id = Guid.NewGuid(), Key = "OLD_KEY", Value = "value", ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        db.Variables.Add(variable);
        await db.SaveChangesAsync();

        var request = new UpdateVariableRequest(Key: "NEW_KEY");
        var command = new UpdateVariable(variable.Id, request);
        var handler = new UpdateVariableHandler(db, NullLogger<UpdateVariableHandler>.Instance);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Key.Should().Be("NEW_KEY");
        result.Value.Should().Be("value");
    }

    [Fact]
    public async Task Handle_UpdateValue_UpdatesSuccessfully()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variable = new Variable { Id = Guid.NewGuid(), Key = "API_KEY", Value = "old-secret", ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        db.Variables.Add(variable);
        await db.SaveChangesAsync();

        var request = new UpdateVariableRequest(Value: "new-secret");
        var command = new UpdateVariable(variable.Id, request);
        var handler = new UpdateVariableHandler(db, NullLogger<UpdateVariableHandler>.Instance);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Key.Should().Be("API_KEY");
        result.Value.Should().Be("new-secret");
    }

    [Fact]
    public async Task Handle_UpdateEnvironmentId_UpdatesSuccessfully()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var newEnvId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variable = new Variable { Id = Guid.NewGuid(), Key = "DATABASE_URL", Value = "postgres://localhost", ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        db.Variables.Add(variable);
        await db.SaveChangesAsync();

        var request = new UpdateVariableRequest(EnvironmentId: newEnvId);
        var command = new UpdateVariable(variable.Id, request);
        var handler = new UpdateVariableHandler(db, NullLogger<UpdateVariableHandler>.Instance);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.EnvironmentId.Should().Be(newEnvId);
    }

    [Fact]
    public async Task Handle_UpdateMultipleFields_UpdatesAllFields()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var newEnvId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variable = new Variable { Id = Guid.NewGuid(), Key = "OLD_KEY", Value = "old-value", ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        db.Variables.Add(variable);
        await db.SaveChangesAsync();

        var request = new UpdateVariableRequest(
            Key: "NEW_KEY",
            Value: "new-value",
            EnvironmentId: newEnvId);

        var command = new UpdateVariable(variable.Id, request);
        var handler = new UpdateVariableHandler(db, NullLogger<UpdateVariableHandler>.Instance);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Key.Should().Be("NEW_KEY");
        result.Value.Should().Be("new-value");
        result.EnvironmentId.Should().Be(newEnvId);
    }

    [Fact]
    public async Task Handle_VariableNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var request = new UpdateVariableRequest(Key: "NEW_KEY");
        var command = new UpdateVariable(nonExistentId, request);
        var handler = new UpdateVariableHandler(db, NullLogger<UpdateVariableHandler>.Instance);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Variable {nonExistentId} not found");
    }

    [Fact]
    public async Task Handle_UpdatesTimestamp_UpdatedAtIsModified()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var originalCreatedAt = DateTime.UtcNow.AddHours(-1);
        var variable = new Variable { Id = Guid.NewGuid(), Key = "TEST_KEY", Value = "value", ProjectId = projectId, CreatedAt = originalCreatedAt, UpdatedAt = originalCreatedAt };

        db.Variables.Add(variable);
        await db.SaveChangesAsync();

        // Small delay to ensure timestamp difference
        await Task.Delay(100);

        var request = new UpdateVariableRequest(Value: "new-value");
        var command = new UpdateVariable(variable.Id, request);
        var handler = new UpdateVariableHandler(db, NullLogger<UpdateVariableHandler>.Instance);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.UpdatedAt.Should().BeAfter(result.CreatedAt);
        result.CreatedAt.Should().Be(originalCreatedAt);
    }

    [Fact]
    public async Task Handle_PersistsChanges_CanBeRetrievedFromDatabase()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variable = new Variable { Id = Guid.NewGuid(), Key = "MUTABLE_KEY", Value = "initial-value", ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        db.Variables.Add(variable);
        await db.SaveChangesAsync();

        var request = new UpdateVariableRequest(
            Key: "UPDATED_KEY",
            Value: "updated-value");

        var command = new UpdateVariable(variable.Id, request);
        var handler = new UpdateVariableHandler(db, NullLogger<UpdateVariableHandler>.Instance);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var updatedVariable = await db.Variables.FirstOrDefaultAsync(v => v.Id == variable.Id);
        updatedVariable.Should().NotBeNull();
        updatedVariable!.Key.Should().Be("UPDATED_KEY");
        updatedVariable.Value.Should().Be("updated-value");
    }

    [Fact]
    public async Task Handle_NullFields_DoesNotUpdateThoseFields()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var originalEnvId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variable = new Variable { Id = Guid.NewGuid(), Key = "ORIGINAL_KEY", Value = "original-value", EnvironmentId = originalEnvId, ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        db.Variables.Add(variable);
        await db.SaveChangesAsync();

        // Update only the value, leaving key and environmentId as null in request
        var request = new UpdateVariableRequest(
            Key: null,
            Value: "updated-value",
            EnvironmentId: null);

        var command = new UpdateVariable(variable.Id, request);
        var handler = new UpdateVariableHandler(db, NullLogger<UpdateVariableHandler>.Instance);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Key.Should().Be("ORIGINAL_KEY", "null key should not update the key field");
        result.Value.Should().Be("updated-value");
        result.EnvironmentId.Should().Be(originalEnvId, "null environmentId should not clear the field");
    }
}
