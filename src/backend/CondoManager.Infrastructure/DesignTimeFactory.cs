using CondoManager.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CondoManager.Infrastructure;
public sealed class DesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__CondoManager")
            ?? throw new InvalidOperationException("Impostare ConnectionStrings__CondoManager.");
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options, new UnauthenticatedTenantContext());
    }
}