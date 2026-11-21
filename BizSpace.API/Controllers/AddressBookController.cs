using BizSpace.Dtos;
using BizSpace.Models;
using BizSpace.Repositories.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BizSpace.Controllers;

[ApiController]
[Route("api/addresses")]
[Authorize(Roles = UserRoles.Customer)]
public sealed class AddressBookController(IAddressBookService addresses) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SavedAddressDto>>>> GetMine() =>
        Ok(ApiResponse<List<SavedAddressDto>>.Ok(await addresses.GetMineAsync(CurrentUserId)));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SavedAddressDto>>> Create(SavedAddressWriteRequest request)
    {
        var address = await addresses.CreateAsync(CurrentUserId, request);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<SavedAddressDto>.Ok(address, "Address saved."));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<SavedAddressDto>>> Update(int id, SavedAddressWriteRequest request) =>
        Ok(ApiResponse<SavedAddressDto>.Ok(await addresses.UpdateAsync(CurrentUserId, id, request), "Address updated."));

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await addresses.DeleteAsync(CurrentUserId, id);
        return Ok(ApiResponse<object>.Ok(null, "Address deleted."));
    }
}
