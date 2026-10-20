// Kollus Upload API 프록시 컨트롤러: 업로드 URL 발급(create-url)과 진행률 조회(progress)를 서버에서 대신 호출한다.
using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Mvc;
using KollusSampleDotnet.Models;

namespace KollusSampleDotnet.Controllers
{
    /// <summary>
    /// Kollus Upload API 프록시
    /// POST /api/upload/create-url  — 업로드 URL 발급
    /// GET  /api/upload/progress/{uploadFileKey} — 업로드 진행률 조회
    /// </summary>
    [RoutePrefix("api")]
    public class ApiController : Controller
    {
        private const string KOLLUS_API_BASE = "https://c-api-kr.kollus.com/api";
        private readonly string _apiToken;
        private readonly string _defaultCategoryKey;

        /// <summary>Web.config의 Upload API 액세스 토큰과 카테고리 키를 읽어 둡니다. 액세스 토큰은 요청으로 받지 않고 항상 이 값을 사용하며, 카테고리 키는 요청에 값이 없을 때만 사용합니다.</summary>
        public ApiController()
        {
            _apiToken           = ConfigurationManager.AppSettings["Kollus.ApiAccessToken"];
            _defaultCategoryKey = ConfigurationManager.AppSettings["Kollus.CategoryKey"];
        }

        /// <summary>
        /// Kollus Upload API(create-url)를 호출해 1회용 업로드 URL과 upload_file_key를 발급받고, 응답 JSON을 그대로 전달합니다.
        /// 브라우저에서 액세스 토큰을 노출하지 않고 업로드하도록 서버가 대신 호출하는 프록시입니다.
        /// </summary>
        /// <param name="req">업로드 생성 요청 (JSON 바디). Title은 필수, ExpireTime은 60~21600초 범위로 보정</param>
        /// <returns>Kollus 응답 JSON 원문 — 실패 시 JSON { status: "fail", error }</returns>
        [Route("upload/create-url")]
        [HttpPost]
        public ActionResult CreateUrl(UploadCreateUrlRequest req)
        {
            if (req == null || string.IsNullOrEmpty(req.Title))
                return Json(new { status = "fail", error = "Missing required: title" });

            var category   = string.IsNullOrEmpty(req.CategoryKey)  ? _defaultCategoryKey : req.CategoryKey;
            var expireTime = ResolveExpireTime(req.ExpireTime);

            try
            {
                // Kollus Upload API: POST /upload/create-url?access_token={token}  (application/x-www-form-urlencoded)
                //   expire_time          — 발급되는 업로드 URL의 유효 시간(초)
                //   title                — 업로드할 콘텐츠 제목
                //   category_key         — 콘텐츠가 등록될 카테고리 키 (없으면 빈 문자열 전송)
                //   is_encryption_upload — 1=암호화 콘텐츠로 업로드 / 0=비암호화
                //   is_audio_upload      — 1=오디오 전용 콘텐츠 / 0=비디오 콘텐츠
                var form = new NameValueCollection
                {
                    { "expire_time",          expireTime.ToString() },
                    { "title",                req.Title },
                    { "category_key",         category ?? "" },
                    { "is_encryption_upload", req.IsEncryptionUpload },
                    { "is_audio_upload",      req.IsAudioUpload }
                };

                var url      = $"{KOLLUS_API_BASE}/upload/create-url?access_token={Uri.EscapeDataString(_apiToken ?? "")}";
                // 응답 JSON: { status, data: { upload_url, upload_file_key, progress_url } } — 클라이언트가 upload_url로 파일을 직접 POST
                var response = PostForm(url, form);

                return Content(response, "application/json");
            }
            catch (Exception ex)
            {
                // Kollus API 호출 실패(네트워크 오류, Kollus 에러 응답 등)를 HTTP 500 대신 { status: "fail" } JSON으로 반환:
                // 클라이언트 JS가 Kollus 정상 응답과 같은 status 필드로 성공/실패를 한 가지 방식으로 판별하도록 통일
                System.Diagnostics.Debug.WriteLine($"[Api/upload/create-url] {ex.Message}");
                return Json(new { status = "fail", error = ex.Message });
            }
        }

        /// <summary>Kollus Upload API(progress)를 호출해 업로드/인코딩 진행 상태 JSON을 그대로 전달합니다.</summary>
        /// <param name="uploadFileKey">create-url 응답으로 받은 업로드 파일 키</param>
        /// <returns>Kollus 응답 JSON 원문 — 실패 시 JSON { status: "fail", error }</returns>
        [Route("upload/progress/{uploadFileKey}")]
        [HttpGet]
        public ActionResult Progress(string uploadFileKey)
        {
            try
            {
                // Kollus Upload API: GET /upload/progress/{upload_file_key}?access_token={token}
                //   업로드 파일 키로 해당 콘텐츠의 업로드·인코딩 진행 상태를 조회
                var url      = $"{KOLLUS_API_BASE}/upload/progress/{Uri.EscapeDataString(uploadFileKey ?? "")}"
                             + $"?access_token={Uri.EscapeDataString(_apiToken ?? "")}";
                var response = GetJson(url);
                return Content(response, "application/json");
            }
            catch (Exception ex)
            {
                // 조회 실패도 create-url과 동일하게 { status: "fail" } JSON으로 반환해 클라이언트 처리 방식을 통일
                return Json(new { status = "fail", error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>expire_time 문자열을 정수로 변환하고 허용 범위(UploadUrlExpireMinSeconds~UploadUrlExpireMaxSeconds)로 보정합니다.</summary>
        /// <param name="value">요청의 ExpireTime 값</param>
        /// <returns>보정된 유효 시간(초) — 파싱 실패 시 KollusDefaults.UploadUrlExpireSeconds</returns>
        private static int ResolveExpireTime(string value)
        {
            if (!int.TryParse(value, out var seconds))
                return KollusDefaults.UploadUrlExpireSeconds;
            return Math.Min(Math.Max(seconds, KollusDefaults.UploadUrlExpireMinSeconds), KollusDefaults.UploadUrlExpireMaxSeconds);
        }

        private string PostForm(string url, NameValueCollection data)
        {
            var sb = new StringBuilder();
            foreach (string key in data.Keys)
                sb.Append($"&{Uri.EscapeDataString(key)}={Uri.EscapeDataString(data[key] ?? "")}");
            var body = Encoding.UTF8.GetBytes(sb.ToString().TrimStart('&'));

            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method      = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            req.ContentLength = body.Length;
            using (var s = req.GetRequestStream()) s.Write(body, 0, body.Length);

            try
            {
                using (var res = (HttpWebResponse)req.GetResponse())
                using (var sr = new StreamReader(res.GetResponseStream(), Encoding.UTF8))
                    return sr.ReadToEnd();
            }
            catch (WebException ex) when (ex.Response != null)
            {
                // Kollus가 4xx/5xx로 응답하면 HttpWebRequest는 WebException을 던지고 본문은 ex.Response에 남는다.
                // 그 본문이 Kollus 에러 JSON(원인 코드·메시지)이므로 읽어서 예외 메시지로 다시 던져
                // 상위 catch가 클라이언트에 Kollus 에러 내용을 그대로 전달할 수 있게 한다.
                using (var sr = new StreamReader(ex.Response.GetResponseStream(), Encoding.UTF8))
                    throw new Exception(sr.ReadToEnd());
            }
        }

        private string GetJson(string url)
        {
            using (var client = new WebClient())
                return client.DownloadString(url);
        }
    }

    /// <summary>
    /// /api/upload/create-url 요청 DTO (JSON 바디에서 모델 바인딩).
    /// 값은 Kollus API 폼 필드에 문자열 그대로 전달되므로 0/1 플래그도 문자열로 둡니다.
    /// </summary>
    public class UploadCreateUrlRequest
    {
        public string ExpireTime         { get; set; } = KollusDefaults.UploadUrlExpireSeconds.ToString();
        public string Title              { get; set; }
        public string CategoryKey        { get; set; }
        public string IsEncryptionUpload { get; set; } = "1";
        public string IsAudioUpload      { get; set; } = "0";
    }
}
