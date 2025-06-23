using Hestia.LocationsMDM.WebApi.Common;
using Hestia.LocationsMDM.WebApi.Exceptions;
using Hestia.LocationsMDM.WebApi.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;

namespace Hestia.LocationsMDM.WebApi.Extensions
{
    /// <summary>
    /// The exception handler in the middleware.
    /// </summary>
    internal static class ExceptionMiddlewareExtensions
    {
        /// <summary>
        /// Configures the exception handler.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="logger">The logger.</param>
        internal static void ConfigureExceptionHandler(this IApplicationBuilder app, ILogger logger)
        {
            app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    string message;
                    HttpStatusCode statusCode;
                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if (contextFeature != null)
                    {
                        string actionName = context.Request.Method + " " + context.Request.Path;
                        bool isError = true;
                        if (contextFeature.Error is ArgumentException)
                        {
                            isError = false;
                            statusCode = HttpStatusCode.BadRequest;
                        }
                        else if (contextFeature.Error is AuthorizationException)
                        {
                            isError = false;
                            statusCode = HttpStatusCode.Forbidden;
                        }
                        else if (contextFeature.Error is AuthenticationException)
                        {
                            isError = false;
                            statusCode = HttpStatusCode.Unauthorized;
                        }
                        else if (contextFeature.Error is EntityNotFoundException)
                        {
                            statusCode = HttpStatusCode.NotFound;
                        }
                        else if (contextFeature.Error is DataConflictException)
                        {
                            statusCode = HttpStatusCode.Conflict;
                        }
                        else
                        {
                            statusCode = HttpStatusCode.InternalServerError;
                        }

                        message = contextFeature.Error.Message;
                        string logMessage = $"Error when requesting '{actionName}'. Details: {contextFeature.Error}";
                        if (isError)
                        {
                            logger.LogError(logMessage);
                        }
                        else
                        {
                            logger.LogWarning(logMessage);
                        }
                    }
                    else
                    {
                        message = "Error on server.";
                        statusCode = HttpStatusCode.InternalServerError;
                    }

                    var error = new ApiErrorModel
                    {
                        Message = contextFeature.Error.Message
                    };

                    context.Response.StatusCode = (int)statusCode;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(Util.Serialize(error));
                });
            });
        }
    }
}
