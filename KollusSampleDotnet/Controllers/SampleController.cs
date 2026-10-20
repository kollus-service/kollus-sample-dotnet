// 샘플 플레이어 페이지 컨트롤러: 기본 키와 옵션 기본값을 ViewBag으로 넘겨 /sample 페이지를 렌더링한다.
using System.Configuration;
using System.Web.Mvc;
using KollusSampleDotnet.Models;

namespace KollusSampleDotnet.Controllers
{
    /// <summary>
    /// GET /sample — JWT 발급 + 웹/모바일 플레이어 샘플 페이지
    /// </summary>
    public class SampleController : Controller
    {
        /// <summary>
        /// Web.config의 채널 키와 KollusDefaults의 기본 옵션값을 ViewBag에 담아 샘플 페이지를 반환합니다.
        /// 뷰의 입력 초기값과 JS fallback이 서버 상수와 어긋나지 않도록 값을 한 곳(KollusDefaults)에서만 가져옵니다.
        /// </summary>
        /// <returns>Views/Sample/Index.cshtml</returns>
        public ActionResult Index()
        {
            ViewBag.DefaultSecurityKey = ConfigurationManager.AppSettings["Kollus.SecurityKey"];
            ViewBag.DefaultCustomKey = ConfigurationManager.AppSettings["Kollus.CustomKey"];
            ViewBag.DefaultMck       = ConfigurationManager.AppSettings["Kollus.MediaContentKey"];

            ViewBag.DefaultExpireMinutes    = KollusDefaults.DefaultExpireMinutes;
            ViewBag.DefaultWatermarkFontSize = KollusDefaults.WatermarkFontSize;
            ViewBag.DefaultWatermarkColor    = KollusDefaults.WatermarkFontColor;
            ViewBag.DefaultWatermarkAlpha    = KollusDefaults.WatermarkAlpha;
            ViewBag.DefaultWatermarkShowTime = KollusDefaults.WatermarkShowTime;
            ViewBag.DefaultWatermarkHideTime = KollusDefaults.WatermarkHideTime;
            return View();
        }
    }
}
