﻿using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Helpers;

public static class ControllerHelper
{
    public static Task<ActionResult> ResultState(this HttpStatusCode httpStatusCode, string message = "", object? value = null)
    {
        return ResultState((int)httpStatusCode, message, value);
    }

    public static Task<ActionResult> ResultState(this int code, string message = "", object? value = null)
    {
        if (code == StatusCodes.Status204NoContent)
            return Task.FromResult<ActionResult>(new NoContentResult());

        return Task.FromResult<ActionResult>(new ObjectResult(value ?? message)
        {
            StatusCode = code
        });
    }
}