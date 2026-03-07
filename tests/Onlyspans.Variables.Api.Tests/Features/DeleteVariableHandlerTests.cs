using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Onlyspans.Variables.Api.Data.Entities;
using Onlyspans.Variables.Api.Features.Variables;
using Onlyspans.Variables.Api.Tests.Helpers;

namespace Onlyspans.Variables.Api.Tests.Features;

public class DeleteVariableHandlerTests
{
    [Fact]
    public async Task Handle_ExistingVariable_DeletesSuccessfully()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variable = new Variable { Id = Guid.NewGuid(), Key = "TO_DELETE", Value = "value", ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        db.Variables.Add(variable);
        await db.SaveChangesAsync();

        var command = new DeleteVariable(variable.Id);
        var handler = new DeleteVariableHandler(db, NullLogger<DeleteVariableHandler>.Instance);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var deletedVariable = await db.Variables.FirstOrDefaultAsync(v => v.Id == variable.Id);
        deletedVariable.Should().BeNull();
    }

    [Fact]
    public async Task Handle_VariableNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var command = new DeleteVariable(nonExistentId);
        var handler = new DeleteVariableHandler(db, NullLogger<DeleteVariableHandler>.Instance);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Variable {nonExistentId} not found");
    }

    [Fact]
    public async Task Handle_DeletesVariable_DoesNotAffectOtherVariables()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variable1 = new Variable { Id = Guid.NewGuid(), Key = "VAR1", Value = "value1", ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var variable2 = new Variable { Id = Guid.NewGuid(), Key = "VAR2", Value = "value2", ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var variable3 = new Variable { Id = Guid.NewGuid(), Key = "VAR3", Value = "value3", ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        db.Variables.AddRange(variable1, variable2, variable3);
        await db.SaveChangesAsync();

        var command = new DeleteVariable(variable2.Id);
        var handler = new DeleteVariableHandler(db, NullLogger<DeleteVariableHandler>.Instance);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var remainingVariables = await db.Variables.ToListAsync();
        remainingVariables.Should().HaveCount(2);
        remainingVariables.Should().Contain(v => v.Id == variable1.Id);
        remainingVariables.Should().Contain(v => v.Id == variable3.Id);
        remainingVariables.Should().NotContain(v => v.Id == variable2.Id);
    }

    [Fact]
    public async Task Handle_DeleteAllVariablesInProject_LeavesProjectIntact()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variable1 = new Variable { Id = Guid.NewGuid(), Key = "VAR1", Value = "value1", ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var variable2 = new Variable { Id = Guid.NewGuid(), Key = "VAR2", Value = "value2", ProjectId = projectId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        db.Variables.AddRange(variable1, variable2);
        await db.SaveChangesAsync();

        var handler = new DeleteVariableHandler(db, NullLogger<DeleteVariableHandler>.Instance);

        // Act
        await handler.Handle(new DeleteVariable(variable1.Id), CancellationToken.None);
        await handler.Handle(new DeleteVariable(variable2.Id), CancellationToken.None);

        // Assert
        var remainingVariables = await db.Variables.Where(v => v.ProjectId == projectId).ToListAsync();
        remainingVariables.Should().BeEmpty();
    }
}
