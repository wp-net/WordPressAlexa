using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace WordPressAlexa.Utility
{
    public class AlexaRequestValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public AlexaRequestValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            Debug.WriteLine("alexa request middleware");
            context.Request.EnableRewind();
            
            // Verify SignatureCertChainUrl is present
            context.Request.Headers.TryGetValue("SignatureCertChainUrl", out var signatureChainUrl);
            if (String.IsNullOrWhiteSpace(signatureChainUrl))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
            var certUrl = new Uri(signatureChainUrl);

            // Verify SignatureCertChainUrl is Signature
            context.Request.Headers.TryGetValue("Signature", out var signature);
            if (String.IsNullOrWhiteSpace(signature))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            string body = new StreamReader(context.Request.Body).ReadToEnd();
            context.Request.Body.Position = 0;

            if (String.IsNullOrWhiteSpace(body))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var valid = await RequestVerification.Verify(signature, certUrl, body);
            if (!valid)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            await _next(context);
        }
    }
}
