// 플레이어 재생 데모 컨트롤러: 설정된 기본 키로 JWT를 발급해 데모 페이지를 렌더링한다.
using System;
using System.Configuration;
using System.Web.Mvc;
using KollusSampleDotnet.Models;
using KollusSampleDotnet.Services;

namespace KollusSampleDotnet.Controllers
{
    /// <summary>
    /// Kollus 플레이어 재생 데모
    /// GET /Demo  — 설정된 기본 키로 JWT를 발급해 재생 페이지를 보여줌
    /// </summary>
    public class DemoController : Controller
    {
        /// <summary>기본 설정값으로 JWT를 발급하고 재생 링크를 뷰에 전달합니다.</summary>
        public ActionResult Index()
        {
            var securityKey = ConfigurationManager.AppSettings["Kollus.SecurityKey"] ?? "";
            var customKey   = ConfigurationManager.AppSettings["Kollus.CustomKey"] ?? "";
            var mck         = ConfigurationManager.AppSettings["Kollus.MediaContentKey"] ?? "";

            var result = new KollusJwtService(securityKey, customKey).GenerateToken(new KollusJwtOptions
            {
                MediaContentKey = mck,
                ClientUserId    = "sample_user",
                ExpireSeconds   = KollusDefaults.DemoJwtExpireSeconds,
            });

            ViewBag.PlayerUrl = result.Link;
            return View();
        }
    }
}
