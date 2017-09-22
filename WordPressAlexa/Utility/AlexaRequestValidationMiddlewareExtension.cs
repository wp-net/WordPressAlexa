using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WordPressAlexa.Utility
{
    public static class AlexaRequestValidationMiddlewareExtension
    {
        public static IApplicationBuilder UseAlexaRequestValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AlexaRequestValidationMiddleware>();
        }
    }
}
