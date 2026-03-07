using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Onlyspans.Variables.Api.Data.Entities;

public class VariableSetConfiguration : IEntityTypeConfiguration<VariableSet>
{
    public void Configure(EntityTypeBuilder<VariableSet> builder)
    {
        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.HasIndex(x => x.Name);
    }
}
