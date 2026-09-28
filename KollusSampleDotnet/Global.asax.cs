// ASP.NET 애플리케이션 진입점.
// 라우트 등록과, Kollus 플레이어 iframe 임베드에 필요한 CORS/Private Network Access 응답 헤더를 처리한다.
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace KollusSampleDotnet
{
    /// <summary>애플리케이션 수명 주기 이벤트(시작, 요청 시작)를 처리하는 HttpApplication</summary>
    public class MvcApplication : HttpApplication
    {
        /// <summary>애플리케이션 시작 시 Area와 라우트를 등록합니다.</summary>
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }

        // Kollus 플레이어 iframe 권한 및 Private Network Access 헤더
        // Chrome PNA 정책: OPTIONS preflight에 Access-Control-Allow-Private-Network 응답 필요

        /// <summary>
        /// 모든 요청에 플레이어 iframe 권한/CORS/PNA 헤더를 추가하고,
        /// OPTIONS preflight 요청은 본문 없이 204(No Content)로 즉시 종료합니다.
        /// </summary>
        protected void Application_BeginRequest()
        {
            var response = Context.Response;
            response.AddHeader("Permissions-Policy", "local-network-access=*, encrypted-media=*, autoplay=*, fullscreen=*");
            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.AddHeader("Access-Control-Allow-Private-Network", "true");
            response.AddHeader("Access-Control-Allow-Headers", "Access-Control-Request-Private-Network");

            if (Context.Request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = (int)HttpStatusCode.NoContent;
                response.End();
            }
        }
    }
}
