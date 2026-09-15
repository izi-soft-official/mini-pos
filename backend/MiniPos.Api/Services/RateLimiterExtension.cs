using System.Threading.RateLimiting;

namespace MiniPos.Api.Services;

public static class RateLimiterExtensions
{
    public static IServiceCollection AddCustomRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/json";

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                }

                var jsonResponse = "{\"error\": \"Too many requests. Please try again later.\"}";
                await context.HttpContext.Response.WriteAsync(jsonResponse, cancellationToken);
            };

            options.AddPolicy("strict-user-limit", httpContext =>
            {
                string partitionKey = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                                      ?? httpContext.Connection.RemoteIpAddress?.ToString()
                                      ?? "unknown";

                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: partitionKey,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        //dis just for testing, in prod it would be way lower
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 4,
                        QueueLimit = 5
                    });
            });
        });

        return services;
    }
}
