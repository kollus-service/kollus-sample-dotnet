using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Mvc;
using System.Web.Script.Serialization;

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
        private readonly string _defaultToken;
        private readonly string _defaultCategoryKey;
        private readonly JavaScriptSerializer _json = new JavaScriptSerializer();

        public ApiController()
        {
            _defaultToken       = ConfigurationManager.AppSettings["Kollus.ApiAccessToken"];
            _defaultCategoryKey = ConfigurationManager.AppSettings["Kollus.CategoryKey"];
        }

        [Route("upload/create-url")]
        [HttpPost]
        public ActionResult CreateUrl(UploadCreateUrlRequest req)
        {
            if (req == null || string.IsNullOrEmpty(req.Title) || string.IsNullOrEmpty(req.ExpireTime))
                return Json(new { status = "fail", error = "Missing required: expire_time, title" });

            var token    = string.IsNullOrEmpty(req.AccessToken)  ? _defaultToken       : req.AccessToken;
            var category = string.IsNullOrEmpty(req.CategoryKey)  ? _defaultCategoryKey : req.CategoryKey;

            try
            {
                var form = new NameValueCollection
                {
                    { "expire_time",          req.ExpireTime },
                    { "title",                req.Title },
                    { "category_key",         category ?? "" },
                    { "is_encryption_upload", req.IsEncryptionUpload },
                    { "is_audio_upload",      req.IsAudioUpload }
                };

                var url      = $"{KOLLUS_API_BASE}/upload/create-url?access_token={token}";
                var response = PostForm(url, form);

                return Content(response, "application/json");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Api/upload/create-url] {ex.Message}");
                return Json(new { status = "fail", error = ex.Message });
            }
        }

        [Route("upload/progress/{uploadFileKey}")]
        [HttpGet]
        public ActionResult Progress(string uploadFileKey)
        {
            var token = Request.QueryString["access_token"] ?? _defaultToken;
            try
            {
                var url      = $"{KOLLUS_API_BASE}/upload/progress/{uploadFileKey}?access_token={token}";
                var response = GetJson(url);
                return Content(response, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = "fail", error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
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

    public class UploadCreateUrlRequest
    {
        public string AccessToken        { get; set; }
        public string ExpireTime         { get; set; } = "3600";
        public string Title              { get; set; }
        public string CategoryKey        { get; set; }
        public string IsEncryptionUpload { get; set; } = "1";
        public string IsAudioUpload      { get; set; } = "0";
    }
}
