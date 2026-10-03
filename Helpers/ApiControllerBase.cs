using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace api.Helpers
{
    public abstract class ApiControllerBase : ControllerBase
    {
        protected Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        protected IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.Succeeded) return Ok(result.Data);
            return result.ErrorType switch
            {
                ResultError.NotFound => NotFound(result.Errors),
                ResultError.Forbidden => StatusCode(StatusCodes.Status403Forbidden, result.Errors),
                ResultError.Conflict => Conflict(result.Errors),
                _ => BadRequest(result.Errors) // this will be the default
            };
        }
    }
}