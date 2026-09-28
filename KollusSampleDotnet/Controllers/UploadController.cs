// 파일 업로드 UI 페이지 컨트롤러: 업로드 API 토큰·카테고리 키·URL 유효 시간 기본값을 ViewBag으로 넘긴다.
using System.Configuration;
using System.Web.Mvc;
using KollusSampleDotnet.Models;

namespace KollusSampleDotnet.Controllers
{
    /// <summary>
    /// GET /upload — 파일 업로드 UI 페이지
    /// 업로드 API 엔드포인트는 ApiController (/api/upload/*)에 있습니다.
    /// </summary>
    public class UploadController : Controller
    {
        /// <summary>
        /// Web.config의 Upload API 토큰·카테고리 키와 업로드 URL 유효 시간 기본값을 ViewBag에 담아 업로드 페이지를 반환합니다.
        /// </summary>
        /// <returns>Views/Upload/Index.cshtml</returns>
        public ActionResult Index()
        {
            ViewBag.DefaultApiToken    = ConfigurationManager.AppSettings["Kollus.ApiAccessToken"];
            ViewBag.DefaultCategoryKey = ConfigurationManager.AppSettings["Kollus.CategoryKey"];
            ViewBag.DefaultUploadExpireTime = KollusDefaults.UploadUrlExpireSeconds;
            return View();
        }
    }
}
