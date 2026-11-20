using BizSpace.Dtos;
using BizSpace.Models;
using BizSpace.Repositories.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BizSpace.Controllers;

[ApiController]
[Route("api/stores")]
public sealed class StoresController(IStoreService stores, INotificationService notifications) : BaseApiController
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<StoreDto>>>> GetAll() =>
        Ok(ApiResponse<List<StoreDto>>.Ok(await stores.GetAllAsync()));

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<StoreDto>>> GetById(int id) =>
        Ok(ApiResponse<StoreDto>.Ok(await stores.GetByIdAsync(id)));

    [Authorize(Roles = UserRoles.StoreManager)]
    [HttpGet("mine")]
    public async Task<ActionResult<ApiResponse<List<StoreDto>>>> GetMine() =>
        Ok(ApiResponse<List<StoreDto>>.Ok(await stores.GetMineAsync(CurrentUserId)));

    [Authorize(Roles = UserRoles.StoreManager)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<StoreDto>>> Create(CreateStoreRequest request)
    {
        var store = await stores.CreateAsync(CurrentUserId, request);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<StoreDto>.Ok(store, "Store created."));
    }

    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.StoreManager)]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await stores.DeleteAsync(id, CurrentUserId, IsAdmin());
        return Ok(ApiResponse<object>.Ok(null, "Store and its catalog were deleted."));
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost("{id:int}/messages")]
    public async Task<ActionResult<ApiResponse<NotificationDto>>> SendManagerMessage(
        int id,
        SendManagerMessageRequest request) =>
        Ok(ApiResponse<NotificationDto>.Ok(
            await notifications.SendAdminMessageAsync(id, CurrentUserId, request.Message),
            "Message sent to the store manager."));
}
