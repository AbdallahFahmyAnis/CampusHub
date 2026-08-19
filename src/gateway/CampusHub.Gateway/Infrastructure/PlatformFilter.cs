using CampusHub.BuildingBlocks.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CampusHub.Gateway.Infrastructure;

/// <summary>
/// Restricts a Razor page to the platform (default) tenant.
/// Campus admins are redirected to /campus instead.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class PlatformOnlyAttribute : Attribute, IPageFilter
{
    public void OnPageHandlerSelected(PageHandlerSelectedContext context) { }

    public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var tenantId = Tenancy.TenantId(context.HttpContext.User);
        if (tenantId == Tenancy.DefaultTenantId)
        {
            return;
        }

        context.Result = new RedirectResult("/campus");
    }

    public void OnPageHandlerExecuted(PageHandlerExecutedContext context) { }
}
