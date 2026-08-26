namespace TmsApi.Api.Middlewares;

public class SecurityHeadersMiddleware(RequestDelegate next)
{
  public async Task InvokeAsync(HttpContext context)
  {
    // Prevents MIME type sniffing attacks
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

    // Prevents clickjacking — page cannot be embedded in an iframe
    context.Response.Headers.Append("X-Frame-Options", "DENY");

    // Controls how much referrer info is sent with requests
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

    // ✅ Content Security Policy — allows Scalar to work
    context.Response.Headers.Append(
        "Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://unpkg.com; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' data: https://fonts.gstatic.com; " +
        "connect-src 'self' ws://localhost:5084 http://localhost:5084;"
    );

    await next(context);
  }
}