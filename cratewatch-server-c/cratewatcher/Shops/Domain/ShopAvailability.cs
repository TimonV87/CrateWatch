namespace CrateWatch.Shops.Domain;

public class ShopAvailability
{
    public Guid Id { get; set; }

    public Guid ShopId { get; set; }

    public Guid CdReleaseId { get; set; }

    public required string Url { get; set; }

    public DateTimeOffset CheckedAt { get; set; }
}
