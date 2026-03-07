using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Onlyspans.Variables.Api.Data.Entities;

public class VariableConfiguration : IEntityTypeConfiguration<Variable>
{
    public void Configure(EntityTypeBuilder<Variable> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Key)
            .IsRequired()
            .HasMaxLength(256);

        builder
            .Property(v => v.Value)
            .IsRequired();

        // Indexes for common queries
        builder.HasIndex(x => x.ProjectId);
        builder.HasIndex(x => x.VariableSetId);
        builder.HasIndex(x => x.EnvironmentId);
        builder.HasIndex(x => new { x.ProjectId, x.Key, x.EnvironmentId });
        builder.HasIndex(x => new { x.VariableSetId, x.Key, x.EnvironmentId });

        // Relationship to VariableSet
        builder
            .HasOne(x => x.VariableSet)
            .WithMany(x => x.Variables)
            .HasForeignKey(x => x.VariableSetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
