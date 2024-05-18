﻿using System.Net.Mime;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
namespace WebApi.Controllers;

[ApiController]
public class ErrorsController : ControllerBase {

   /// <summary>
   /// Handle errors in development environment.
   /// </summary>
   [HttpGet("/banking/error-development")]
   [Produces(MediaTypeNames.Application.Json)]
   [ProducesResponseType(StatusCodes.Status404NotFound)]
   [ProducesDefaultResponseType]
   public IActionResult HandleErrorDevelopment(
      [FromServices] IHostEnvironment hostEnvironment
   ) {
      if (!hostEnvironment.IsDevelopment()) {
         return NotFound();
      }

      var exceptionHandlerFeature = HttpContext.Features.Get<IExceptionHandlerFeature>()!;

      var values = exceptionHandlerFeature?.RouteValues?.Values;
      var text = string.Empty;
      if (values != null) {
         foreach (var r in values)
            if (r != null)
               text += r + " ";
      }
      text += $"{exceptionHandlerFeature?.Path} ... ";
      text += $"{exceptionHandlerFeature?.Error.Message}";

      return Problem(
         title: text,
         instance: exceptionHandlerFeature?.Endpoint?.DisplayName,
         detail: exceptionHandlerFeature?.Error?.StackTrace
      );
   }

   // RFC 7807 Probelm Details
   /// <summary>
   /// Handle errors in production environment.
   /// </summary>   
   [HttpGet("/banking/error")]
   [Produces(MediaTypeNames.Application.Json)]
   [ProducesDefaultResponseType]
   public IActionResult HandleError()
      => Problem();

}