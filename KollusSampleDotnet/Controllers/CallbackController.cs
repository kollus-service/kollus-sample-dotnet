using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using KollusSampleDotnet.Services;

namespace KollusSampleDotnet.Controllers
{
    /// <summary>
    /// Kollus 플레이어 콜백 통합 컨트롤러
    /// POST /callback/play          — 재생 콜백 (kind 1: 만료 설정 / kind 3: 재생 승인)
    /// POST /callback/drm           — DRM 콜백 (kind 1: 다운로드 승인 / 2: 완료 / 3: 오프라인 확인)
    /// POST /callback/next-episode  — 다음 회차 SI링크 반환
    /// POST /callback/lms           — 시청 진도 수신 및 로컬 파일 로그
    /// </summary>
    [RoutePrefix("callback")]
    public class CallbackController : Controller
    {
        private readonly KollusJwtService _jwtService;
        private readonly string _customKey;
        private readonly JavaScriptSerializer _json = new JavaScriptSerializer();

        // 콜백 JWT 만료 시간 (초)
        private const int CALLBACK_JWT_EXPIRE_SEC = 600;
        // 콘텐츠 재생 만료 시간 — 30일
        private const int CONTENT_EXPIRE_SEC = 60 * 60 * 24 * 30;
        // Next Episode JWT 만료 시간 (초)
        private const int NEXT_EPISODE_EXPIRE_SEC = 180;

        // 다음 회차 MCK 목록 — 실제 서비스에서는 DB/API 조회로 교체
        private static readonly string[] NextEpisodeMcks = { "YOUR_MCK_1", "YOUR_MCK_2" };

        public CallbackController()
        {
            var securityKey = ConfigurationManager.AppSettings["Kollus.SecurityKey"];
            _customKey      = ConfigurationManager.AppSettings["Kollus.CustomKey"];
            _jwtService     = new KollusJwtService(securityKey, _customKey);
        }

        // ── Play Callback ─────────────────────────────────────────────
        // POST /callback/play
        // kind 1: 재생 만료 설정  /  kind 3: 최종 재생 승인
        // Request: application/x-www-form-urlencoded
        //   kind, client_user_id, player_id, media_content_key
        //   (optional) hardware_id, device_name, localtime, uservalues

        /// <summary>재생 콜백 처리 — kind 1(만료 설정) / kind 3(재생 승인)</summary>
        [Route("play")]
        [HttpPost]
        public ActionResult Play()
        {
            if (!int.TryParse(Request.Form["kind"], out var kind))
                return new HttpStatusCodeResult(400, "Invalid kind");
            var now  = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            object data;

            if (kind == 1)
            {
                data = new Dictionary<string, object>
                {
                    // [Required] 재생 만료 Unix timestamp (최대: 2145916799)
                    { "expiration_date", now + CONTENT_EXPIRE_SEC },
                    // [Required] 1=허용 / 0=차단
                    { "result", 1 },

                    // [Optional] 차단 시 표시 메시지
                    // { "message", "Your subscription has expired." },

                    // [Optional] VM 환경 차단: 0=허용 / 1=차단
                    // { "vmcheck", 0 },

                    // [Optional] 외부 출력(TV-out) 차단: 0=허용 / 1=차단
                    // { "disable_tvout", 0 },

                    // [Optional] 누적 재생 시간 제한(초), 0=무제한
                    // { "expiration_playtime", 0 },

                    // [Optional] 화면 캡처 감지(PC): 0=off / 1=on
                    // { "cpcheck", 1 },
                };
            }
            else if (kind == 3)
            {
                data = new Dictionary<string, object>
                {
                    // [Required] 1=허용 / 0=차단
                    { "result", 1 },

                    // [Optional] 콘텐츠 만료 여부: 0=재생가능 / 1=만료
                    // { "content_expired", 0 },

                    // [Optional] 차단 시 표시 메시지
                    // { "message", "Playback is not allowed." },
                };
            }
            else
            {
                return new HttpStatusCodeResult(400, "Unknown kind");
            }

            return SendCallbackJwt(new Dictionary<string, object>
            {
                { "data", data },
                { "exp",  now + CALLBACK_JWT_EXPIRE_SEC },
            });
        }

        // ── DRM Callback ──────────────────────────────────────────────
        // POST /callback/drm
        // kind 1: 다운로드 승인  /  kind 2: 다운로드 완료  /  kind 3: 오프라인 재생 확인
        // Request: items(JSON 배열) 필드 포함

        /// <summary>DRM 콜백 처리 — kind 1(다운로드 승인) / 2(완료) / 3(오프라인 재생 확인)</summary>
        [Route("drm")]
        [HttpPost]
        public ActionResult Drm()
        {
            var itemsJson = Request.Form["items"];
            if (string.IsNullOrEmpty(itemsJson))
                return new HttpStatusCodeResult(400, "Missing items");

            List<Dictionary<string, object>> items;
            try
            {
                items = _json.Deserialize<List<Dictionary<string, object>>>(itemsJson);
            }
            catch (Exception ex)
            {
                // items 필드가 유효한 JSON 배열이 아닌 경우 400 반환
                System.Diagnostics.Debug.WriteLine($"[Callback/drm] items 파싱 실패: {ex.Message}");
                return new HttpStatusCodeResult(400, "Invalid items JSON");
            }

            var dataList = new List<object>();

            foreach (var item in items)
            {
                if (!item.ContainsKey("kind")) continue;
                var kind = Convert.ToInt32(item["kind"]);
                var mck  = item.ContainsKey("media_content_key") ? item["media_content_key"]?.ToString() : "";

                if (kind == 1)
                {
                    dataList.Add(new Dictionary<string, object>
                    {
                        // [Required]
                        { "kind",               1   },
                        { "media_content_key",  mck },
                        { "result",             1   },

                        // [Optional] 다운로드 만료 Unix timestamp
                        // { "expiration_date", now + CONTENT_EXPIRE_SEC },

                        // [Optional] 오프라인 재생 횟수 제한: 0=무제한
                        // { "expiration_count", 0 },

                        // [Optional] 오프라인 재생 시간 제한(초): 0=무제한
                        // { "expiration_playtime", 0 },

                        // [Optional] 재생 시간 측정 방식: 1=실재생시간(정지 제외)
                        // { "expiration_playtime_type", 1 },

                        // [Optional] 차단 시 표시 메시지
                        // { "message", "Download is not allowed." },

                        // [Optional] 만료 갱신 팝업: 0=숨김 / 1=표시
                        // { "expiration_refresh_popup", 0 },

                        // [Optional] VM 환경 차단(HTML5 PC): 0=허용 / 1=차단
                        // { "vmcheck", 1 },

                        // [Optional] kind 3 오프라인 재생 중 콜백 여부: 0=off / 1=on
                        // { "check_abuse", 0 },

                        // [Optional] 오프라인 북마크 동기화 설정
                        // { "offline_bookmark", new Dictionary<string, object> {
                        //     { "download", 0 }, { "readonly", 0 }
                        // }},
                    });
                }
                else if (kind == 2)
                {
                    dataList.Add(new Dictionary<string, object>
                    {
                        // [Required]
                        { "kind",               2   },
                        { "media_content_key",  mck },
                        { "result",             1   },

                        // [Optional] 다운로드 후 즉시 삭제: 0=유지 / 1=삭제
                        // { "content_delete", 0 },

                        // [Optional] 삭제 시 표시 메시지
                        // { "message", "This content has been removed." },

                        // [Optional] 검증 기간 만료 Unix timestamp: 0=무제한
                        // { "check_expiration_date", 0 },
                    });
                }
                else if (kind == 3)
                {
                    var startAt = item.ContainsKey("start_at") ? Convert.ToInt64(item["start_at"]) : 0;

                    dataList.Add(new Dictionary<string, object>
                    {
                        // [Required]
                        { "kind",               3       },
                        { "media_content_key",  mck     },
                        { "start_at",           startAt }, // 요청값 그대로 echo
                        { "result",             1       },

                        // [Optional] 콘텐츠 강제 만료: 0=재생가능 / 1=차단
                        // { "content_expired", 0 },

                        // [Optional] 다운로드 파일 삭제: 0=유지 / 1=삭제
                        // { "content_delete", 0 },

                        // [Optional] 만료 정책 초기화: 0=유지 / 1=초기화
                        // { "content_expire_reset", 0 },

                        // [Optional] 재생 만료 Unix timestamp 갱신
                        // { "expiration_date", now + CONTENT_EXPIRE_SEC },

                        // [Optional] 오프라인 재생 횟수 갱신: 0=무제한
                        // { "expiration_count", 0 },

                        // [Optional] 오프라인 재생 시간 갱신(초): 0=무제한
                        // { "expiration_playtime", 0 },

                        // [Optional] kind 3 재호출 여부: 0=off / 1=on
                        // { "check_abuse", 0 },

                        // [Optional] 검증 기간 만료 Unix timestamp: 0=무제한
                        // { "check_expiration_date", 0 },

                        // [Optional] 차단/삭제 시 표시 메시지
                        // { "message", "Playback is not allowed." },
                    });
                }
            }

            return SendCallbackJwt(new Dictionary<string, object> { { "data", dataList } });
        }

        // ── Next Episode Callback ─────────────────────────────────────
        // POST /callback/next-episode
        // 영상 종료 시 플레이어가 다음 회차 SI링크를 요청

        /// <summary>다음 회차 콜백 — 현재 MCK 기준으로 다음 MCK의 SI링크 JWT 반환</summary>
        [Route("next-episode")]
        [HttpPost]
        public ActionResult NextEpisode(string mck, string id)
        {
            try
            {
                var nextMck = GetNextMck(mck);
                if (nextMck == null)
                    return Json(new { result = 0 });

                var result  = _jwtService.GenerateToken(new Models.KollusJwtOptions
                {
                    MediaContentKey = nextMck,
                    ClientUserId    = id ?? "unknown",
                    ExpireSeconds   = NEXT_EPISODE_EXPIRE_SEC
                });
                var nextUrl = result.Link.Replace("/s?", "/si?");
                return Json(new { result = 1, next_url = nextUrl });
            }
            catch (Exception ex)
            {
                // 다음 회차 URL 생성 실패 시 result=0 반환하여 플레이어가 자연 종료되도록 처리
                System.Diagnostics.Debug.WriteLine($"[Callback/next-episode] {ex.Message}");
                return Json(new { result = 0 });
            }
        }

        // ── LMS Callback ──────────────────────────────────────────────
        // POST /callback/lms
        // 플레이어가 재생 진도를 주기적으로 전송 — HTTP 200만 응답
        // 전체 데이터는 json_data 필드에 JSON 문자열로 포함됨
        // 로그: ~/logs/lms.log (JSON Lines 형식)

        /// <summary>LMS 콜백 — 재생 진도를 ~/logs/lms.log에 기록하고 HTTP 200 반환</summary>
        [Route("lms")]
        [HttpPost]
        public ActionResult Lms()
        {
            try
            {
                var jsonData = Request.Form["json_data"];
                Dictionary<string, object> d = null;
                if (!string.IsNullOrEmpty(jsonData))
                {
                    try
                    {
                        d = _json.Deserialize<Dictionary<string, object>>(jsonData);
                    }
                    catch (Exception ex)
                    {
                        // json_data 파싱 실패 시 raw 값 없이 received_at만 기록하고 계속 진행
                        System.Diagnostics.Debug.WriteLine($"[Callback/lms] json_data 파싱 실패: {ex.Message}");
                    }
                }

                var log = new Dictionary<string, object>
                {
                    ["received_at"]  = DateTime.UtcNow.ToString("O"),
                    ["user_info"]    = d != null && d.ContainsKey("user_info")    ? d["user_info"]    : null,
                    ["content_info"] = d != null && d.ContainsKey("content_info") ? d["content_info"] : null,
                    ["block_info"]   = d != null && d.ContainsKey("block_info")   ? d["block_info"]   : null,
                };

                var logDir = Path.Combine(Server.MapPath("~/"), "logs");
                Directory.CreateDirectory(logDir);
                System.IO.File.AppendAllText(
                    Path.Combine(logDir, "lms.log"),
                    _json.Serialize(log) + Environment.NewLine,
                    Encoding.UTF8);
            }
            catch (Exception ex)
            {
                // 로그 기록 실패는 플레이어 응답에 영향을 주지 않도록 예외를 삼키고 200 반환
                System.Diagnostics.Debug.WriteLine($"[Callback/lms] 로그 기록 실패: {ex.Message}");
            }

            return new HttpStatusCodeResult(200);
        }

        // ── Helpers ───────────────────────────────────────────────────

        private ActionResult SendCallbackJwt(Dictionary<string, object> payload)
        {
            Response.Headers.Add("X-KOLLUS-USERKEY", _customKey);
            return Content(_jwtService.GenerateJwtFromPayload(payload), "text/plain");
        }

        private string GetNextMck(string currentMck)
        {
            if (NextEpisodeMcks.Length == 0) return null;
            var idx = Array.IndexOf(NextEpisodeMcks, currentMck);
            if (idx < 0 || idx >= NextEpisodeMcks.Length - 1) return null;
            return NextEpisodeMcks[idx + 1];
        }
    }
}
