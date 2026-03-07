using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Onlyspans.Variables.Api.Data.Entities;

public class ProjectVariableSetLinkConfiguration : IEntityTypeConfiguration<ProjectVariableSetLink>
{
    public void Configure(EntityTypeBuilder<ProjectVariableSetLink> builder)
    {
        builder.HasKey(x => new { x.ProjectId, x.VariableSetId });

        builder.HasIndex(x => x.ProjectId);
        builder.HasIndex(x => x.VariableSetId);

        builder.HasOne(x => x.VariableSet)
            .WithMany()
            .HasForeignKey(x => x.VariableSetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
