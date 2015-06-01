using System;
using System.Web.Mvc;
using Common.Api;

namespace Utils
{
    public class AccessDeniedAuthorizeAttribute : AuthorizeAttribute
    {
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            base.OnAuthorization(filterContext);

            if (filterContext.Result is HttpUnauthorizedResult)
            {
                filterContext.Result = new RedirectResult("~/Home");
            }
        }
    }

    public class UserIpAccessDenied : AuthorizeAttribute
    {
         public override void OnAuthorization(AuthorizationContext filterContext)
        {
            base.OnAuthorization(filterContext);

            if (SessionManager.AccessDenied.HasValue && SessionManager.AccessDenied == true)
                    filterContext.Result = new RedirectResult("~/AccessDenied");
          //  else
           // {
             //   filterContext.Result = new RedirectResult("~/Calculator");
                    
            //}
        }
    }
}
