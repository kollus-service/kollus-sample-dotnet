// ASP.NET MVC 라우팅 설정.
// 특성(Attribute) 라우팅을 활성화하고 기본 {controller}/{action}/{id} 라우트를 등록한다.
using System.Web.Mvc;
using System.Web.Routing;

namespace KollusSampleDotnet
{
    /// <summary>애플리케이션 시작 시 호출되는 라우트 등록 클래스</summary>
    public class RouteConfig
    {
        /// <summary>
        /// 라우트 테이블을 구성합니다. 특성 라우팅을 먼저 등록해야
        /// /callback/next-episode 같은 하이픈 포함 URL이 기본 라우트보다 우선 매칭됩니다.
        /// </summary>
        /// <param name="routes">Global.asax에서 전달되는 전역 라우트 컬렉션(RouteTable.Routes)</param>
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Attribute routing 활성화 (하이픈 포함 URL: /callback/next-episode 등)
            routes.MapMvcAttributeRoutes();

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
