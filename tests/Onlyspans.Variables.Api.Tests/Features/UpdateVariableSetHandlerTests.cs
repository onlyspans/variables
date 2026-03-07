using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Onlyspans.Variables.Api.Data.Entities;
using Onlyspans.Variables.Api.Data.Records;
using Onlyspans.Variables.Api.Features.VariableSets;
using Onlyspans.Variables.Api.Tests.Helpers;

namespace Onlyspans.Variables.Api.Tests.Features;

public class UpdateVariableSetHandlerTests
{
    [Fact]
    public async Task Handle_UpdateName_UpdatesSuccessfully()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variableSet = new VariableSet { Id = Guid.NewGuid(), Name = "Old Name", Description = "Description", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync();

        var request = new UpdateVariableSetRequest(Name: "New Name");
        var command = new UpdateVariableSet(variableSet.Id, request);
        var handler = new UpdateVariableSetHandler(db, NullLogger<UpdateVariableSetHandler>.Instance);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Name.Should().Be("New Name");
        result.Description.Should().Be("Description");
    }

    [Fact]
    public async Task Handle_UpdateDescription_UpdatesSuccessfully()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variableSet = new VariableSet { Id = Guid.NewGuid(), Name = "Test Set", Description = "Old Description", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync();

        var request = new UpdateVariableSetRequest(Description: "New Description");
        var command = new UpdateVariableSet(variableSet.Id, request);
        var handler = new UpdateVariableSetHandler(db, NullLogger<UpdateVariableSetHandler>.Instance);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Name.Should().Be("Test Set");
        result.Description.Should().Be("New Description");
    }

    [Fact]
    public async Task Handle_UpdateBothFields_UpdatesSuccessfully()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variableSet = new VariableSet { Id = Guid.NewGuid(), Name = "Old Name", Description = "Old Description", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync();

        var request = new UpdateVariableSetRequest(
            Name: "New Name",
            Description: "New Description");

        var command = new UpdateVariableSet(variableSet.Id, request);
        var handler = new UpdateVariableSetHandler(db, NullLogger<UpdateVariableSetHandler>.Instance);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Name.Should().Be("New Name");
        result.Description.Should().Be("New Description");
    }

    [Fact]
    public async Task Handle_VariableSetNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var request = new UpdateVariableSetRequest(Name: "New Name");
        var command = new UpdateVariableSet(nonExistentId, request);
        var handler = new UpdateVariableSetHandler(db, NullLogger<UpdateVariableSetHandler>.Instance);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Variable set {nonExistentId} not found");
    }

    [Fact]
    public async Task Handle_UpdatesTimestamp_UpdatedAtIsModified()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var originalCreatedAt = DateTime.UtcNow.AddHours(-1);
        var variableSet = new VariableSet { Id = Guid.NewGuid(), Name = "Test Set", CreatedAt = originalCreatedAt, UpdatedAt = originalCreatedAt };

        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync();

        await Task.Delay(100);

        var request = new UpdateVariableSetRequest(Name: "Updated Name");
        var command = new UpdateVariableSet(variableSet.Id, request);
        var handler = new UpdateVariableSetHandler(db, NullLogger<UpdateVariableSetHandler>.Instance);

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
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variableSet = new VariableSet { Id = Guid.NewGuid(), Name = "Initial Name", Description = "Initial Description", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync();

        var request = new UpdateVariableSetRequest(
            Name: "Updated Name",
            Description: "Updated Description");

        var command = new UpdateVariableSet(variableSet.Id, request);
        var handler = new UpdateVariableSetHandler(db, NullLogger<UpdateVariableSetHandler>.Instance);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var updated = await db.VariableSets.FirstOrDefaultAsync(vs => vs.Id == variableSet.Id);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Name");
        updated.Description.Should().Be("Updated Description");
    }
}
