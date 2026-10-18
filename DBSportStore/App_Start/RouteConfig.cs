using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace DBSportStore
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default", // tên route
                url: "{controller}/{action}/{id}", // mẫu URL /Controller/Action/Id
                defaults: new { controller = "Store", action = "Index", id = UrlParameter.Optional } // Controller và Action mặc định
            );
        }
    }
}
