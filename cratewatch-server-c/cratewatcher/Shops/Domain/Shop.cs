namespace CrateWatch.Shops.Domain;

public class Shop
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public required string Name { get; set; }

    public required string SearchUrlTemplate { get; set; }

    // Learned from the example when the shop answers with JSON; both null for a shop that answers with HTML.
    public string? JsonItemsPath { get; set; }

    public string? JsonUrlField { get; set; }
}
