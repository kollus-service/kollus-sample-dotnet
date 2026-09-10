using System.Web.Mvc;

namespace KollusSampleDotnet.Controllers
{
    /// <summary>
    /// 샘플 애플리케이션 홈 화면
    /// GET /  — 메인 페이지
    /// </summary>
    public class HomeController : Controller
    {
        /// <summary>메인 페이지를 반환합니다.</summary>
        public ActionResult Index()
        {
            return View();
        }
    }
}
