using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace KollusSampleDotnet
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }

        // Kollus 플레이어 iframe 권한 및 Private Network Access 헤더
        // Chrome PNA 정책: OPTIONS preflight에 Access-Control-Allow-Private-Network 응답 필요
        protected void Application_BeginRequest()
        {
            var response = Context.Response;
            response.AddHeader("Permissions-Policy", "local-network-access=*, encrypted-media=*, autoplay=*, fullscreen=*");
            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.AddHeader("Access-Control-Allow-Private-Network", "true");
            response.AddHeader("Access-Control-Allow-Headers", "Access-Control-Request-Private-Network");

            if (Context.Request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 204;
                response.End();
            }
        }
    }
}
