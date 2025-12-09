using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace MiddlewareNexi.Filters
{
    public class ApiKeyAuthFilter : IAsyncAuthorizationFilter
    {
        private const string ApiKeyHeaderName = "X-API-KEY";
        private readonly IConfiguration _configuration;

        public ApiKeyAuthFilter(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var http = context.HttpContext;

            // 1) LASCIA PASSARE IL PREFLIGHT CORS
            if (HttpMethods.IsOptions(http.Request.Method))
                return Task.CompletedTask;

            // 2) Leggi la chiave configurata
            var configuredKey = _configuration["Security:ApiKey"];
            if (string.IsNullOrWhiteSpace(configuredKey))
            {
                context.Result = new UnauthorizedObjectResult("API Key non configurata");
                return Task.CompletedTask;
            }

            // 3) Estrai la chiave dall'header
            if (!http.Request.Headers.TryGetValue(ApiKeyHeaderName, out var provided))
            {
                context.Result = new UnauthorizedObjectResult("API Key mancante");
                return Task.CompletedTask;
            }

            // 4) Confronto a tempo costante, evitando NRE e spazi
            var a = Encoding.UTF8.GetBytes(configuredKey.Trim());
            var b = Encoding.UTF8.GetBytes(provided.ToString().Trim());
            if (a.Length != b.Length || !CryptographicOperations.FixedTimeEquals(a, b))
            {
                context.Result = new UnauthorizedObjectResult("API Key non valida");
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
    }
}
