using System.Configuration;
using System.Web.Mvc;

namespace KollusSampleDotnet.Controllers
{
    /// <summary>
    /// GET /upload — 파일 업로드 UI 페이지
    /// 업로드 API 엔드포인트는 ApiController (/api/upload/*)에 있습니다.
    /// </summary>
    public class UploadController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.DefaultApiToken    = ConfigurationManager.AppSettings["Kollus.ApiAccessToken"];
            ViewBag.DefaultCategoryKey = ConfigurationManager.AppSettings["Kollus.CategoryKey"];
            return View();
        }
    }
}
