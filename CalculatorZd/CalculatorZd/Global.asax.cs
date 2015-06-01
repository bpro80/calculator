using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.SessionState;
using BO;
using BO.Implementation;
using BO.Implementation.Caching;
using BO.Implementation.Caching.Provider;
using BO.Implementation.Calculator;
using Common.Api;
using log4net;
using Microsoft.AspNet.Identity;

namespace CalculatorZd
{
    public class MvcApplication : HttpApplication
    {
        private const string _WebApiPrefix = "api";
        private static string _WebApiExecutionPath = String.Format("~/{0}", _WebApiPrefix);

        readonly ILog _logger = LogManager.GetLogger(typeof(MvcApplication));

        protected void Application_Start()
        {
            log4net.Config.XmlConfigurator.Configure();
            _logger.Info("Application start");
           
            AreaRegistration.RegisterAllAreas();
            RegisterRoutes(RouteTable.Routes);
            IdentityConfig.ConfigureIdentity();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            BundleConfig.RegisterBundles(BundleTable.Bundles);
            _logger.Info("Init server properties");

            ServerProperties.Instance.Init();
            DictionariesLoader.Start();
            
        }

        private void Session_Start(object sender, EventArgs e)
        {
            SessionManager.CurrentIP = HttpContext.Current.Request.UserHostAddress;
            
            HttpContext.Current.Session.Add("__calcAppSession", string.Empty);
            if (User.Identity.IsAuthenticated)
            {
                if (ServerProperties.Instance.EnableBlockUnKnowIP == 1)
                {
                    if ((User.IsInRole("admin") || User.IsInRole("god")))
                    {
                        SessionManager.AccessDenied = false;
                    }
                    else
                    {
                        SessionManager.AccessDenied = AutorizeHelper.CheckIPAccess(SessionManager.CurrentIP);
                    }
                }
                else
                {
                    SessionManager.AccessDenied = false;
                }
               

                string userId = User.Identity.GetUserId();
                Firm firm = FirmsManager.GetFirmByID(new Guid(userId));
                SessionManager.FirmInfo = firm;
            }
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            Route route = routes.MapHttpRoute("DefaultApi", "api/{controller}/{action}/{id}",
                new {id = RouteParameter.Optional}
                );
            route.RouteHandler = new MyHttpControllerRouteHandler();
        }

        protected void Application_PostAuthorizeRequest()
        {
            if (IsWebApiRequest())
            {
                HttpContext.Current.SetSessionStateBehavior(SessionStateBehavior.Required);
            }
        }

        private static bool IsWebApiRequest()
        {
            return HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath.StartsWith(_WebApiExecutionPath);
        }

    }
}