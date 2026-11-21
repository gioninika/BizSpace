using BizSpace.Dtos;
using BizSpace.Models;
using BizSpace.Repositories.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BizSpace.Controllers;

[ApiController]
[Route("api/store-manager/campaigns")]
[Authorize(Roles = UserRoles.StoreManager)]
public sealed class StoreManagerCampaignsController(IMarketingCampaignService campaigns) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<MarketingCampaignDto>>>> GetAll() =>
        Ok(ApiResponse<List<MarketingCampaignDto>>.Ok(await campaigns.GetAllAsync(CurrentUserId)));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<MarketingCampaignDto>>> Create(MarketingCampaignWriteRequest request)
    {
        var campaign = await campaigns.CreateAsync(request, CurrentUserId);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<MarketingCampaignDto>.Ok(campaign, "Marketing campaign created."));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<MarketingCampaignDto>>> Update(int id, MarketingCampaignWriteRequest request) =>
        Ok(ApiResponse<MarketingCampaignDto>.Ok(await campaigns.UpdateAsync(id, request, CurrentUserId), "Marketing campaign updated."));

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id)
    {
        await campaigns.DeleteAsync(id, CurrentUserId);
        return Ok(ApiResponse<object>.Ok(null, "Marketing campaign deleted."));
    }
}
