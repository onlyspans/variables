using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Onlyspans.Variables.Api.Data.Entities;

public class ProjectVariableSetLinkConfiguration : IEntityTypeConfiguration<ProjectVariableSetLink>
{
    public void Configure(EntityTypeBuilder<ProjectVariableSetLink> builder)
    {
        builder.HasKey(l => new { l.ProjectId, l.VariableSetId });

        builder.HasIndex(l => l.ProjectId);
        builder.HasIndex(l => l.VariableSetId);

        builder.HasOne(l => l.VariableSet)
            .WithMany()
            .HasForeignKey(l => l.VariableSetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
