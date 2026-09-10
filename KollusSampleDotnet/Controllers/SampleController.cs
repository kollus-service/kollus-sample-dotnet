using System.Configuration;
using System.Web.Mvc;

namespace KollusSampleDotnet.Controllers
{
    /// <summary>
    /// GET /sample — JWT 발급 + 웹/모바일 플레이어 샘플 페이지
    /// </summary>
    public class SampleController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.DefaultSecretKey = ConfigurationManager.AppSettings["Kollus.SecurityKey"];
            ViewBag.DefaultCustomKey = ConfigurationManager.AppSettings["Kollus.CustomKey"];
            ViewBag.DefaultMck       = ConfigurationManager.AppSettings["Kollus.MediaContentKey"];
            return View();
        }
    }
}
