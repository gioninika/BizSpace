using BizSpace.Dtos;
using BizSpace.Models;
using BizSpace.Repositories.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BizSpace.Controllers;

[ApiController]
[Route("api/store-manager/analytics")]
[Authorize(Roles = UserRoles.StoreManager)]
public sealed class StoreManagerAnalyticsController(ISalesAnalyticsService analytics) : BaseApiController
{
    [HttpGet("sales")]
    public async Task<ActionResult<ApiResponse<SalesAnalyticsDto>>> GetSales(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc) =>
        Ok(ApiResponse<SalesAnalyticsDto>.Ok(await analytics.GetSalesAsync(fromUtc, toUtc, CurrentUserId)));
}
