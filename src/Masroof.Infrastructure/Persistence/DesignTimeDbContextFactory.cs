using Masroof.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Masroof.Infrastructure.Persistence;

/// <summary>Lets the EF tools (migrations) construct the context without the full app host.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MasroofDbContext>
{
    public MasroofDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Sql")
            ?? "Server=localhost;Database=Masroof;User Id=sa;Password=Your_password123;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<MasroofDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new MasroofDbContext(options, new DesignTimeCurrentUser());
    }

    private sealed class DesignTimeCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated => false;
        public Guid UserId => Guid.Empty;
        public string? DisplayName => null;
        public string Locale => "en";
        public string Currency => "SAR";
    }
}
