namespace CampusHub.Identity.Api.Data;

public sealed class Tenant
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public required string Plan { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
