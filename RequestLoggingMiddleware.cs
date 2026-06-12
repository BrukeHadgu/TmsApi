using System.Diagnostics;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "REQUEST  [{CorrelationId}] {Method} {Path}",
            correlationId,
            context.Request.Method,
            context.Request.Path);

        await _next(context);

        stopwatch.Stop();

        _logger.LogInformation(
            "RESPONSE [{CorrelationId}] {StatusCode} in {ElapsedMs}ms",
            correlationId,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds);
    }
}