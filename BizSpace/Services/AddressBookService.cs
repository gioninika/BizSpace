using BizSpace.Dtos;
using BizSpace.Models;
using BizSpace.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BizSpace.Repositories.Services;

public sealed class AddressBookService(IGenericRepository<SavedAddress> addresses) : IAddressBookService
{
    public async Task<List<SavedAddressDto>> GetMineAsync(int userId) =>
        (await addresses.Query.AsNoTracking().Where(x => x.UserId == userId)
            .OrderBy(x => x.Id).ToListAsync()).Select(ToDto).ToList();

    public async Task<SavedAddressDto> CreateAsync(int userId, SavedAddressWriteRequest request)
    {
        var address = new SavedAddress { UserId = userId };
        Apply(address, request);
        await addresses.AddAsync(address);
        await addresses.SaveAsync();
        return ToDto(address);
    }

    public async Task<SavedAddressDto> UpdateAsync(int userId, int addressId, SavedAddressWriteRequest request)
    {
        var address = await addresses.Query.FirstOrDefaultAsync(x => x.Id == addressId && x.UserId == userId)
            ?? throw new KeyNotFoundException("Saved address was not found.");
        Apply(address, request);
        await addresses.SaveAsync();
        return ToDto(address);
    }

    public async Task DeleteAsync(int userId, int addressId)
    {
        var address = await addresses.Query.FirstOrDefaultAsync(x => x.Id == addressId && x.UserId == userId)
            ?? throw new KeyNotFoundException("Saved address was not found.");
        addresses.Delete(address);
        await addresses.SaveAsync();
    }

    private static void Apply(SavedAddress address, SavedAddressWriteRequest request)
    {
        address.Label = request.Label.Trim();
        address.StreetAddress = request.StreetAddress.Trim();
        address.City = request.City.Trim();
        address.PostalCode = request.PostalCode.Trim();
        address.Country = request.Country.Trim();
    }

    private static SavedAddressDto ToDto(SavedAddress address) => new()
    {
        Id = address.Id,
        Label = address.Label,
        StreetAddress = address.StreetAddress,
        City = address.City,
        PostalCode = address.PostalCode,
        Country = address.Country,
        CreatedAtUtc = address.CreatedAtUtc
    };
}
