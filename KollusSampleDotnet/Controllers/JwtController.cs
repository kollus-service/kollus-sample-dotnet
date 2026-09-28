// 플레이어 JWT 발급 API 컨트롤러: 쿼리 또는 JSON 파라미터로 Kollus 플레이어 토큰을 만든다.
using System;
using System.Configuration;
using System.Web.Mvc;
using KollusSampleDotnet.Models;
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

        /// <summary>Web.config의 채널 보안키/사용자키를 기본값으로 읽어 둡니다. 요청에 키가 없으면 이 값을 사용합니다.</summary>
        public JwtController()
        {
            _defaultSecretKey = ConfigurationManager.AppSettings["Kollus.SecurityKey"];
            _defaultCustomKey = ConfigurationManager.AppSettings["Kollus.CustomKey"];
        }

        /// <summary>쿼리 파라미터로 플레이어 JWT를 발급합니다 (GET /jwt/create).</summary>
        /// <param name="mck">재생할 미디어 콘텐츠 키 (필수)</param>
        /// <param name="cuid">시청자 식별자(client_user_id)</param>
        /// <param name="expt">토큰 만료 시간(분)</param>
        /// <param name="secretKey">채널 보안키 — 생략 시 Web.config 값 사용</param>
        /// <param name="customKey">채널 사용자키 — 생략 시 Web.config 값 사용</param>
        /// <param name="seek">탐색(스크러빙) 허용 여부 — null이면 허용</param>
        /// <param name="intro">본편 앞에 재생할 인트로 MCK</param>
        /// <param name="outro">본편 뒤에 재생할 아웃트로 MCK</param>
        /// <param name="nextEpisode">본편 종료 시 next-episode 콜백 호출 여부</param>
        /// <returns>JSON { Token, Link } — 실패 시 { error }</returns>
        [Route("create")]
        [HttpGet]
        public ActionResult Create(
            string mck, string cuid = "sample_user", int expt = KollusDefaults.DefaultExpireMinutes,
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

        /// <summary>JSON 바디로 플레이어 JWT를 발급합니다 (POST /jwt/create). 재생 구간·워터마크 등 전체 옵션을 지원합니다.</summary>
        /// <param name="req">발급 옵션 (JSON 바디에서 모델 바인딩)</param>
        /// <returns>JSON { Token, Link } — 실패 시 { error }</returns>
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

                var options = new KollusJwtOptions
                {
                    MediaContentKey = req.Mck,
                    ClientUserId    = req.Cuid ?? "sample_user",
                    // 클라이언트는 분 단위(expt)로 보내고 JWT 옵션은 초 단위이므로 환산
                    ExpireSeconds   = req.Expt * KollusDefaults.SecondsPerMinute,
                    IntroMck        = req.Intro,
                    OutroMck        = req.Outro,
                    NextEpisode     = req.NextEpisode,
                    SeekEnabled     = req.Seek ?? true,
                    SeekableEnd     = req.SeekableEnd,
                    DisablePlayrate = req.DisablePlayrate,
                };

                if (req.PlaySectionEnd > 0)
                    options.PlaySection = new PlaySection
                    {
                        StartTime = req.PlaySectionStart,
                        EndTime   = req.PlaySectionEnd
                    };

                if (req.Watermark)
                    options.Watermarking = new WatermarkingPolicy
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

    /// <summary>
    /// /jwt/create 요청 DTO. GET 쿼리 파라미터와 POST JSON 바디 양쪽에서 모델 바인딩됩니다.
    /// 기본값은 KollusDefaults와 동일하게 유지해 뷰·모델과 어긋나지 않도록 합니다.
    /// </summary>
    public class JwtCreateRequest
    {
        public string Mck             { get; set; }
        public string Cuid            { get; set; } = "sample_user";
        public int    Expt            { get; set; } = KollusDefaults.DefaultExpireMinutes;
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
        public int    WatermarkFontSize { get; set; } = KollusDefaults.WatermarkFontSize;
        public string WatermarkColor  { get; set; } = KollusDefaults.WatermarkFontColor;
        public int    WatermarkAlpha  { get; set; } = KollusDefaults.WatermarkAlpha;
        public int    WatermarkShowTime { get; set; } = KollusDefaults.WatermarkShowTime;
        public int    WatermarkHideTime { get; set; } = KollusDefaults.WatermarkHideTime;
    }
}
