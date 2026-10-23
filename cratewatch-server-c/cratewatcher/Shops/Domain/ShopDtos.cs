using System.ComponentModel.DataAnnotations;
using CrateWatch.Releases.Domain;

namespace CrateWatch.Shops.Domain;

public record ShopResponse(Guid Id, string Name, string SearchUrlTemplate)
{
    public static ShopResponse From(Shop shop) => new(shop.Id, shop.Name, shop.SearchUrlTemplate);
}

public record CreateShopRequest(
    [Required(AllowEmptyStrings = false), StringLength(100)] string Name,
    [Required(AllowEmptyStrings = false), StringLength(1000)] string SearchUrlTemplate,
    [Required(AllowEmptyStrings = false), StringLength(200)] string ExampleArtist,
    [Required(AllowEmptyStrings = false), StringLength(200)] string ExampleTitle
);

public record ScanResult(int Checked, int Found, int Failed);

public record ShopOffer(Guid ShopId, string ShopName, string Url, DateTimeOffset CheckedAt);

public record AvailableRelease(ReleaseResponse Release, List<ShopOffer> Offers);
