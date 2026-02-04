using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Onlyspans.Variables.Api.Data.Entities;

public class VariableSetConfiguration : IEntityTypeConfiguration<VariableSet>
{
    public void Configure(EntityTypeBuilder<VariableSet> builder)
    {
        builder.HasKey(vs => vs.Id);

        builder.Property(vs => vs.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(vs => vs.Description)
            .HasMaxLength(1000);

        builder.HasIndex(vs => vs.Name);
    }
}
