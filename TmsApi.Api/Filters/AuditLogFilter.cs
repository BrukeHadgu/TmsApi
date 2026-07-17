using Microsoft.AspNetCore.Mvc.Filters;
namespace TmsApi.Api.Filters;
public class AuditLogFilter(ILogger<AuditLogFilter> logger) : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var method = context.HttpContext.Request.Method;
        var route  = context.HttpContext.Request.Path;
        logger.LogInformation("TMS API call: {Method} {Route}", method, route);
    }
    public void OnActionExecuted(ActionExecutedContext context)
    {
        var status = context.HttpContext.Response.StatusCode;
        logger.LogInformation("TMS API response: {StatusCode}", status);
    }
}