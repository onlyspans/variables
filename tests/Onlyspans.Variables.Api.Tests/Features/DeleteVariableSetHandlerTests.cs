using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Onlyspans.Variables.Api.Data.Entities;
using Onlyspans.Variables.Api.Features.VariableSets;
using Onlyspans.Variables.Api.Tests.Helpers;

namespace Onlyspans.Variables.Api.Tests.Features;

public class DeleteVariableSetHandlerTests
{
    [Fact]
    public async Task Handle_ExistingVariableSet_DeletesSuccessfully()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var variableSet = new VariableSet { Id = Guid.NewGuid(), Name = "To Delete", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.VariableSets.Add(variableSet);
        await db.SaveChangesAsync();

        var command = new DeleteVariableSet(variableSet.Id);
        var handler = new DeleteVariableSetHandler(db, NullLogger<DeleteVariableSetHandler>.Instance);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var deleted = await db.VariableSets.FirstOrDefaultAsync(vs => vs.Id == variableSet.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task Handle_VariableSetNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var command = new DeleteVariableSet(nonExistentId);
        var handler = new DeleteVariableSetHandler(db, NullLogger<DeleteVariableSetHandler>.Instance);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Variable set {nonExistentId} not found");
    }

    [Fact]
    public async Task Handle_DeleteVariableSet_DoesNotAffectOthers()
    {
        // Arrange
        var db = MockDbContextFactory.CreateInMemoryDbContext();

        var set1 = new VariableSet { Id = Guid.NewGuid(), Name = "Set 1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var set2 = new VariableSet { Id = Guid.NewGuid(), Name = "Set 2", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var set3 = new VariableSet { Id = Guid.NewGuid(), Name = "Set 3", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        db.VariableSets.AddRange(set1, set2, set3);
        await db.SaveChangesAsync();

        var command = new DeleteVariableSet(set2.Id);
        var handler = new DeleteVariableSetHandler(db, NullLogger<DeleteVariableSetHandler>.Instance);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var remaining = await db.VariableSets.ToListAsync();
        remaining.Should().HaveCount(2);
        remaining.Should().Contain(s => s.Id == set1.Id);
        remaining.Should().Contain(s => s.Id == set3.Id);
    }
}
