using System;
using System.Configuration;
using System.Web.Mvc;
using KollusSampleDotnet.Services;

namespace KollusSampleDotnet.Controllers
{
    /// <summary>
    /// Kollus 플레이어 JWT 발급 API
    /// GET  /jwt/create  — 쿼리 파라미터로 토큰 발급
    /// POST /jwt/create  — JSON 바디로 토큰 발급
    /// </summary>
    [RoutePrefix("jwt")]
    public class JwtController : Controller
    {
        private readonly string _defaultSecretKey;
        private readonly string _defaultCustomKey;

        public JwtController()
        {
            _defaultSecretKey = ConfigurationManager.AppSettings["Kollus.SecurityKey"];
            _defaultCustomKey = ConfigurationManager.AppSettings["Kollus.CustomKey"];
        }

        [Route("create")]
        [HttpGet]
        public ActionResult Create(
            string mck, string cuid = "sample_user", int expt = 3,
            string secretKey = null, string customKey = null,
            bool? seek = null, string intro = null, string outro = null,
            bool nextEpisode = false)
        {
            return GenerateToken(new JwtCreateRequest
            {
                Mck = mck, Cuid = cuid, Expt = expt,
                SecretKey = secretKey, CustomKey = customKey,
                Seek = seek, Intro = intro, Outro = outro,
                NextEpisode = nextEpisode
            });
        }

        [Route("create")]
        [HttpPost]
        public ActionResult Create(JwtCreateRequest req)
        {
            return GenerateToken(req);
        }

        private ActionResult GenerateToken(JwtCreateRequest req)
        {
            if (string.IsNullOrEmpty(req.Mck))
                return Json(new { error = "Missing required parameter: mck" }, JsonRequestBehavior.AllowGet);

            try
            {
                var sKey = string.IsNullOrEmpty(req.SecretKey) ? _defaultSecretKey : req.SecretKey;
                var cKey = string.IsNullOrEmpty(req.CustomKey) ? _defaultCustomKey : req.CustomKey;

                var options = new Models.KollusJwtOptions
                {
                    MediaContentKey = req.Mck,
                    ClientUserId    = req.Cuid ?? "sample_user",
                    ExpireSeconds   = req.Expt * 60,
                    IntroMck        = req.Intro,
                    OutroMck        = req.Outro,
                    NextEpisode     = req.NextEpisode,
                    SeekEnabled     = req.Seek ?? true,
                    SeekableEnd     = req.SeekableEnd,
                    DisablePlayrate = req.DisablePlayrate,
                };

                if (req.PlaySectionEnd > 0)
                    options.PlaySection = new Models.PlaySection
                    {
                        StartTime = req.PlaySectionStart,
                        EndTime   = req.PlaySectionEnd
                    };

                if (req.Watermark)
                    options.Watermarking = new Models.WatermarkingPolicy
                    {
                        CodeKind         = "client_user_id",
                        FontSize         = req.WatermarkFontSize,
                        FontColor        = req.WatermarkColor,
                        Alpha            = req.WatermarkAlpha,
                        ShowTime         = req.WatermarkShowTime,
                        HideTime         = req.WatermarkHideTime,
                        EnableHtml5Player = true
                    };

                var svc    = new KollusJwtService(sKey, cKey);
                var result = svc.GenerateToken(options);
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // JWT 생성 실패: 잘못된 옵션 조합이나 키 누락 가능성 — 클라이언트가 원인 파악할 수 있게 메시지 전달
                System.Diagnostics.Debug.WriteLine($"[Jwt/create] {ex.Message}");
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }

    public class JwtCreateRequest
    {
        public string Mck             { get; set; }
        public string Cuid            { get; set; } = "sample_user";
        public int    Expt            { get; set; } = 3;
        public string SecretKey       { get; set; }
        public string CustomKey       { get; set; }
        public bool?  Seek            { get; set; }
        public int    SeekableEnd     { get; set; } = 0;
        public string Intro           { get; set; }
        public string Outro           { get; set; }
        public bool   NextEpisode     { get; set; } = false;
        public bool   DisablePlayrate { get; set; } = false;
        // Play section
        public int    PlaySectionStart { get; set; } = 0;
        public int    PlaySectionEnd   { get; set; } = 0;
        // Watermark
        public bool   Watermark       { get; set; } = false;
        public int    WatermarkFontSize { get; set; } = 10;
        public string WatermarkColor  { get; set; } = "FFFFFF";
        public int    WatermarkAlpha  { get; set; } = 128;
        public int    WatermarkShowTime { get; set; } = 3;
        public int    WatermarkHideTime { get; set; } = 300;
    }
}
