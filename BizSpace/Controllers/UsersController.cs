using BizSpace.Dtos;
using BizSpace.Models;
using BizSpace.Repositories.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BizSpace.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController(IUserService users, IOrderService orders) : BaseApiController
{
    [Authorize(Roles = UserRoles.Admin)]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetAll() =>
        Ok(ApiResponse<List<UserDto>>.Ok(await users.GetAllAsync()));

    [AllowAnonymous]
    [HttpPost("store-managers")]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateStoreManager(CreateStoreManagerRequest request)
    {
        var manager = await users.CreateStoreManagerAsync(request);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<UserDto>.Ok(manager, "Store Manager account created. Check the email for a confirmation code."));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(int id) =>
        Ok(ApiResponse<UserDto>.Ok(await users.GetByIdAsync(id, CurrentUserId, IsAdmin())));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Update(int id, UpdateUserRequest request) =>
        Ok(ApiResponse<UserDto>.Ok(await users.UpdateAsync(id, CurrentUserId, IsAdmin(), request), "Profile updated."));

    [HttpGet("{id:int}/orders")]
    public async Task<ActionResult<ApiResponse<List<OrderDto>>>> GetOrders(int id) =>
        Ok(ApiResponse<List<OrderDto>>.Ok(await orders.GetForUserAsync(id, CurrentUserId, IsAdmin())));
}
