// 멀티 DRM 데모 컨트롤러: 멀티 DRM 재생 URL 생성과 DoveRunner 라이선스 토큰 발급을 처리한다.
using System;
using System.Configuration;
using System.Web.Mvc;
using KollusSampleDotnet.Models;
using KollusSampleDotnet.Services;

namespace KollusSampleDotnet.Controllers
{
    /// <summary>
    /// Kollus 멀티 DRM 재생/라이선스 데모
    /// GET  /MultiDrm            — 데모 페이지
    /// POST /MultiDrm/GeneratePlayUrl      — 멀티 DRM 재생 URL 생성
    /// POST /MultiDrm/GenerateLicenseToken — 라이선스 토큰 생성
    /// </summary>
    public class MultiDrmController : Controller
    {
        private readonly KollusMultiDrmService _multiDrmService;

        public MultiDrmController()
        {
            _multiDrmService = new KollusMultiDrmService(
                securityKey:   ConfigurationManager.AppSettings["Kollus.MultiDrm.SecurityKey"],
                customKey:     ConfigurationManager.AppSettings["Kollus.MultiDrm.CustomKey"],
                doveRunnerSiteKey:   ConfigurationManager.AppSettings["Kollus.DoveRunner.SiteKey"],
                doveRunnerAccessKey: ConfigurationManager.AppSettings["Kollus.DoveRunner.AccessKey"],
                doveRunnerSiteId: ConfigurationManager.AppSettings["Kollus.DoveRunner.SiteId"]
            );
        }

        /// <summary>멀티 DRM 데모 페이지를 반환합니다.</summary>
        [HttpGet]
        public ActionResult Index()
        {
            ViewBag.DefaultMck          = ConfigurationManager.AppSettings["Kollus.MultiDrm.MediaContentKey"] ?? "";
            ViewBag.DefaultSiteId       = ConfigurationManager.AppSettings["Kollus.DoveRunner.SiteId"] ?? "";
            ViewBag.DefaultUploadFileKey = ConfigurationManager.AppSettings["Kollus.MultiDrm.UploadFileKey"] ?? "";
            return View();
        }

        /// <summary>멀티 DRM 재생 URL을 생성합니다 (POST).</summary>
        /// <param name="options">재생 옵션(콘텐츠 키, 사용자 ID, 스트리밍 타입 등)</param>
        [HttpPost]
        public ActionResult GeneratePlayUrl(MultiDrmJwtOptions options)
        {
            try
            {
                var playUrl = _multiDrmService.GenerateMultiDrmPlayUrl(options);
                return Json(new { success = true, play_url = playUrl });
            }
            catch (Exception ex)
            {
                // 멀티 DRM 재생 URL 생성 실패: DoveRunner 설정값 누락이나 옵션 오류 가능성
                System.Diagnostics.Debug.WriteLine($"[MultiDrm/GeneratePlayUrl] {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>라이선스 토큰을 생성합니다 (POST).</summary>
        /// <param name="uploadFileKey">업로드 파일 키(콘텐츠 식별자)</param>
        /// <param name="streamingType">스트리밍 타입 ("hls"면 FairPlay, 그 외 Widevine)</param>
        [HttpPost]
        public ActionResult GenerateLicenseToken(string uploadFileKey, string streamingType = "dash")
        {
            try
            {
                var cuid  = "sample_user";
                var token = _multiDrmService.CreateLicenseToken(cuid, uploadFileKey, streamingType);
                return Json(new { success = true, license_token = token });
            }
            catch (Exception ex)
            {
                // 라이선스 토큰 생성 실패: uploadFileKey가 유효하지 않거나 DoveRunner 설정 오류 가능성
                System.Diagnostics.Debug.WriteLine($"[MultiDrm/GenerateLicenseToken] {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
