using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CampusHub.Identity.Api.Data;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.UseOpenIddict();
        builder.Entity<Tenant>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasConversion(GuidAsText);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Slug).HasMaxLength(80);
            entity.Property(x => x.Plan).HasMaxLength(32);
            entity.HasIndex(x => x.Slug).IsUnique();
        });
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.TenantId).HasConversion(GuidAsText);
        });
    }

    private static readonly ValueConverter<Guid, string> GuidAsText = new(
        id => id.ToString("D"),
        value => ParseGuid(value));

    private static Guid ParseGuid(string? value) =>
        Guid.TryParse(value, out var parsed) ? parsed : Guid.Empty;
}
