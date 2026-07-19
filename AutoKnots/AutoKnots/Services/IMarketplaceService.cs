using AutoKnots.Models;

namespace AutoKnots.Services;

public interface IMarketplaceService
{
    Task<MarketplacePage> SearchAsync(MarketplaceSearch search, CancellationToken cancellationToken = default);
    Task<MarketplacePage> GetSellerListingsAsync(string sellerUserId, MarketplaceSearch search, CancellationToken cancellationToken = default);
    Task<MarketplaceListingDetails?> GetPublicDetailsAsync(int id, CancellationToken cancellationToken = default);
    Task<MarketplaceListingDetails?> GetOwnerDetailsAsync(int id, string sellerUserId, CancellationToken cancellationToken = default);
    Task<MarketplaceResult<MarketplaceListingDetails>> CreateAsync(string sellerUserId, MarketplaceListingInput input, CancellationToken cancellationToken = default);
    Task<MarketplaceResult<MarketplaceListingDetails>> UpdateAsync(int id, string sellerUserId, MarketplaceListingInput input, CancellationToken cancellationToken = default);
    Task<MarketplaceResult<MarketplaceListingDetails>> SetStatusAsync(int id, string sellerUserId, ListingStatus status, CancellationToken cancellationToken = default);
}
