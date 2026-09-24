using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using KMC.EventClient.Helpers;

namespace KMC.EventClient.Controllers;

public class BaseController : Controller
{
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        ViewBag.Token = GlobalVariables.Token;
        ViewBag.Username = GlobalVariables.UserName;
        ViewBag.Role = GlobalVariables.UserRole;
        ViewBag.IsLoggedIn = !string.IsNullOrEmpty(GlobalVariables.Token);
        base.OnActionExecuting(filterContext);
    }
}