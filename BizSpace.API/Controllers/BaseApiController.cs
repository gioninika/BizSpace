using System.Security.Claims;
using BizSpace.Exceptions;
using BizSpace.Models;
using Microsoft.AspNetCore.Mvc;

namespace BizSpace.Controllers;

public abstract class BaseApiController : ControllerBase
{
    protected bool IsAdmin() => User.IsInRole(UserRoles.Admin);
    protected bool IsStoreManager() => User.IsInRole(UserRoles.StoreManager);

    protected int CurrentUserId
    {
        get
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(value, out var id)) return id;
            throw new UnauthorizedException("User identity was not found in the token.");
        }
    }
}
