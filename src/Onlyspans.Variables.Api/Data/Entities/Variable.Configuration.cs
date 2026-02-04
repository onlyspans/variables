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

        builder.Property(v => v.Value)
            .IsRequired();

        // Indexes for common queries
        builder.HasIndex(v => v.ProjectId);
        builder.HasIndex(v => v.VariableSetId);
        builder.HasIndex(v => v.EnvironmentId);
        builder.HasIndex(v => new { v.ProjectId, v.Key, v.EnvironmentId });
        builder.HasIndex(v => new { v.VariableSetId, v.Key, v.EnvironmentId });

        // Relationship to VariableSet
        builder.HasOne(v => v.VariableSet)
            .WithMany(vs => vs.Variables)
            .HasForeignKey(v => v.VariableSetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
