using AutoKnots.Data;
using AutoKnots.Models;
using AutoKnots.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AutoKnots.Tests;

public class MarketplaceServiceTests
{
    [Fact]
    public async Task CreateAsync_CreatesActiveListingForOwnedInventory()
    {
        await using var db = CreateDatabase();
        var item = await AddInventoryAsync(db, "seller-1");
        var service = new MarketplaceService(db);

        var result = await service.CreateAsync("seller-1", ValidInput(item.Id, publish: true));

        Assert.True(result.Success);
        Assert.Equal(ListingStatus.Active, result.Value!.Status);
        Assert.NotNull(result.Value.PublishedAt);
        Assert.Equal(1, await db.VehicleListings.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_DoesNotAllowAnotherUsersInventory()
    {
        await using var db = CreateDatabase();
        var item = await AddInventoryAsync(db, "seller-1");
        var service = new MarketplaceService(db);

        var result = await service.CreateAsync("seller-2", ValidInput(item.Id));

        Assert.False(result.Success);
        Assert.Equal("Inventory item not found.", result.Error);
        Assert.Empty(db.VehicleListings);
    }

    [Fact]
    public async Task CreateAsync_RejectsSecondListingForSameVehicle()
    {
        await using var db = CreateDatabase();
        var item = await AddInventoryAsync(db, "seller-1");
        var service = new MarketplaceService(db);
        Assert.True((await service.CreateAsync("seller-1", ValidInput(item.Id))).Success);

        var second = await service.CreateAsync("seller-1", ValidInput(item.Id));

        Assert.False(second.Success);
        Assert.Contains("already has", second.Error);
    }

    [Fact]
    public async Task SearchAsync_ReturnsOnlyActiveListingsMatchingFilters()
    {
        await using var db = CreateDatabase();
        var activeItem = await AddInventoryAsync(db, "seller-1", "Toyota", "Corolla");
        var draftItem = await AddInventoryAsync(db, "seller-1", "Ford", "Focus");
        var service = new MarketplaceService(db);
        Assert.True((await service.CreateAsync("seller-1", ValidInput(activeItem.Id, publish: true))).Success);
        Assert.True((await service.CreateAsync("seller-1", ValidInput(draftItem.Id))).Success);

        var page = await service.SearchAsync(new MarketplaceSearch { Make = "Toyota" });

        var listing = Assert.Single(page.Items);
        Assert.Equal("Toyota", listing.Make);
        Assert.False(string.IsNullOrWhiteSpace(listing.Slug));
    }

    [Fact]
    public async Task SetStatusAsync_EnforcesLifecycleTransitions()
    {
        await using var db = CreateDatabase();
        var item = await AddInventoryAsync(db, "seller-1");
        var service = new MarketplaceService(db);
        var created = await service.CreateAsync("seller-1", ValidInput(item.Id));

        var activated = await service.SetStatusAsync(created.Value!.Id, "seller-1", ListingStatus.Active);
        var sold = await service.SetStatusAsync(created.Value.Id, "seller-1", ListingStatus.Sold);
        var reactivated = await service.SetStatusAsync(created.Value.Id, "seller-1", ListingStatus.Active);

        Assert.True(activated.Success);
        Assert.True(sold.Success);
        Assert.False(reactivated.Success);
    }

    [Fact]
    public async Task PublicResults_UseSlugsAndDoNotExposeInternalIdentifiers()
    {
        await using var db = CreateDatabase();
        var item = await AddInventoryAsync(db, "seller-1");
        var service = new MarketplaceService(db);
        var created = await service.CreateAsync("seller-1", ValidInput(item.Id, publish: true));

        var page = await service.SearchAsync(new MarketplaceSearch());
        var details = await service.GetPublicDetailsAsync(created.Value!.Slug);

        Assert.NotEmpty(Assert.Single(page.Items).Slug);
        Assert.NotNull(details);
        var publicProperties = typeof(PublicListingDetails).GetProperties().Select(property => property.Name).ToList();
        Assert.DoesNotContain("Id", publicProperties);
        Assert.DoesNotContain("InventoryItemId", publicProperties);
        Assert.DoesNotContain("SellerUserId", publicProperties);
        Assert.DoesNotContain("CostPrice", publicProperties);
    }

    [Fact]
    public async Task PublicSearch_HidesExpiredAndNonActiveListings()
    {
        await using var db = CreateDatabase();
        var active = await AddInventoryAsync(db, "seller-1", "Toyota", "Corolla");
        var expired = await AddInventoryAsync(db, "seller-1", "Ford", "Focus");
        var draft = await AddInventoryAsync(db, "seller-1", "BMW", "M3");
        var service = new MarketplaceService(db);
        Assert.True((await service.CreateAsync("seller-1", ValidInput(active.Id, publish: true))).Success);
        var expiredInput = ValidInput(expired.Id, publish: true);
        expiredInput.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        Assert.True((await service.CreateAsync("seller-1", expiredInput)).Success);
        Assert.True((await service.CreateAsync("seller-1", ValidInput(draft.Id))).Success);

        var page = await service.SearchAsync(new MarketplaceSearch());

        var visible = Assert.Single(page.Items);
        Assert.Equal("Toyota", visible.Make);
    }

    [Fact]
    public async Task PublicDetails_RespectSellerNameConsent()
    {
        await using var db = CreateDatabase();
        var item = await AddInventoryAsync(db, "seller-1");
        var input = ValidInput(item.Id, publish: true);
        input.ShowSellerName = false;
        var service = new MarketplaceService(db);
        var created = await service.CreateAsync("seller-1", input);

        var details = await service.GetPublicDetailsAsync(created.Value!.Slug);

        Assert.NotNull(details);
        Assert.Null(details.SellerName);
    }

    [Fact]
    public async Task CreateAsync_GeneratesStableUniqueCleanSlugs()
    {
        await using var db = CreateDatabase();
        var firstItem = await AddInventoryAsync(db, "seller-1", "BMW", "M3");
        var secondItem = await AddInventoryAsync(db, "seller-1", "BMW", "M3");
        var service = new MarketplaceService(db);
        var firstInput = ValidInput(firstItem.Id);
        var secondInput = ValidInput(secondItem.Id);
        firstInput.Title = secondInput.Title = "BMW M3 Competition!";

        var first = await service.CreateAsync("seller-1", firstInput);
        var second = await service.CreateAsync("seller-1", secondInput);

        Assert.Equal("bmw-m3-competition-2022", first.Value!.Slug);
        Assert.Equal("bmw-m3-competition-2022-2", second.Value!.Slug);
    }

    private static ApplicationDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new ApplicationDbContext(options);
        db.Users.Add(new IdentityUser { Id = "seller-1", UserName = "seller@example.test", Email = "seller@example.test" });
        db.Users.Add(new IdentityUser { Id = "seller-2", UserName = "other@example.test", Email = "other@example.test" });
        db.SaveChanges();
        return db;
    }

    private static async Task<InventoryItem> AddInventoryAsync(
        ApplicationDbContext db,
        string ownerId,
        string make = "Toyota",
        string model = "Corolla")
    {
        var item = new InventoryItem
        {
            Name = $"{make} {model}",
            Make = make,
            Model = model,
            EngineNumber = Guid.NewGuid().ToString(),
            ChassisNumber = Guid.NewGuid().ToString(),
            CreatedByUserId = ownerId,
            CreatedAt = DateTime.UtcNow,
            Status = InventoryStatus.Active,
            IsActive = true
        };
        db.InventoryItems.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    private static MarketplaceListingInput ValidInput(int inventoryItemId, bool publish = false) => new()
    {
        InventoryItemId = inventoryItemId,
        Title = "Well maintained family car",
        Description = "Full service history and ready to drive.",
        Price = 12_500,
        Year = 2022,
        Mileage = 18_000,
        FuelType = FuelType.Petrol,
        Transmission = TransmissionType.Automatic,
        BodyStyle = BodyStyle.Saloon,
        Condition = VehicleCondition.Good,
        Location = "London",
        Publish = publish
    };
}
