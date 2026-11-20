using BizSpace.Dtos;
using BizSpace.Models;
using BizSpace.Repositories.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BizSpace.Controllers;

[ApiController]
[Route("api/wishlist")]
[Authorize(Roles = UserRoles.Customer)]
public sealed class WishlistController(IWishlistService wishlist) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<WishlistDto>>>> GetMine() =>
        Ok(ApiResponse<List<WishlistDto>>.Ok(await wishlist.GetMineAsync(CurrentUserId)));

    [HttpPost("{productId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Add(int productId)
    {
        await wishlist.AddAsync(CurrentUserId, productId);
        return Ok(ApiResponse<object>.Ok(null, "Product added to wishlist."));
    }

    [HttpDelete("{productId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Remove(int productId)
    {
        await wishlist.RemoveAsync(CurrentUserId, productId);
        return Ok(ApiResponse<object>.Ok(null, "Product removed from wishlist."));
    }
}
