using Microsoft.EntityFrameworkCore;
using Onlyspans.Variables.Api.Data.Entities;

namespace Onlyspans.Variables.Api.Data.Contexts;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Variable> Variables => Set<Variable>();
    public DbSet<VariableSet> VariableSets => Set<VariableSet>();
    public DbSet<ProjectVariableSetLink> ProjectVariableSetLinks => Set<ProjectVariableSetLink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
