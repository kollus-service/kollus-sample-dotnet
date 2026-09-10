using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using KollusSampleDotnet.Models;

namespace KollusSampleDotnet.Services
{
    /// <summary>DoveRunner(Pallycon) 기반 멀티 DRM 재생 URL 및 라이선스 토큰을 생성하는 서비스</summary>
    public class KollusMultiDrmService
    {
        private readonly string _securityKey;
        private readonly string _customKey;
        private readonly string _doveRunnerSiteKey;
        private readonly string _doveRunnerAccessKey;
        private readonly string _doveRunnerSiteId;

        // DoveRunner 스펙에 정의된 고정 IV
        private const string DOVE_RUNNER_IV      = "0123456789abcdef";
        private const string LICENSE_URL         = "https://drm-license.doverunner.com/ri/licenseManager.do";
        private const string CERT_URL_TEMPLATE   = "https://drm-license.doverunner.com/ri/fpsKeyManager.do?siteId={0}";
        private const string CUSTOM_HEADER_KEY   = "pallycon-customdata-v2";
        private const int    POLICY_DURATION_SEC = 86400;
        private const int    PLAYREADY_SEC_LEVEL = 150;

        public KollusMultiDrmService(string securityKey, string customKey,
                                     string doveRunnerSiteKey, string doveRunnerAccessKey, string doveRunnerSiteId)
        {
            _securityKey         = securityKey;
            _customKey           = customKey;
            _doveRunnerSiteKey   = doveRunnerSiteKey;
            _doveRunnerAccessKey = doveRunnerAccessKey;
            _doveRunnerSiteId    = doveRunnerSiteId;
        }

        /// <summary>멀티 DRM(Inka/DoveRunner) 라이선스 정책이 포함된 플레이어 재생 URL을 생성합니다.</summary>
        /// <param name="options">재생 옵션(콘텐츠 키, 사용자 ID, 스트리밍 타입 등)</param>
        /// <returns>DRM 정책이 담긴 JWT를 포함한 플레이어 재생 URL</returns>
        public string GenerateMultiDrmPlayUrl(MultiDrmJwtOptions options)
        {
            var licenseToken = CreateInkaPayload(options.ClientUserId, options.UploadFileKey, options.StreamingType);

            var drmPolicy = new Dictionary<string, object>
            {
                { "kind",           "inka" },
                { "streaming_type", options.StreamingType },
                { "data", new Dictionary<string, object>
                    {
                        { "license_url",     LICENSE_URL },
                        { "certificate_url", string.Format(CERT_URL_TEMPLATE, _doveRunnerSiteId) },
                        { "custom_header",   new Dictionary<string, object>
                            {
                                { "key",   CUSTOM_HEADER_KEY },
                                { "value", licenseToken }
                            }
                        }
                    }
                }
            };

            var mcItem = new Dictionary<string, object>
            {
                { "mckey",      options.MediaContentKey },
                { "drm_policy", drmPolicy }
            };

            var payload = new Dictionary<string, object>
            {
                { "cuid", options.ClientUserId },
                { "expt", DateTimeOffset.UtcNow.ToUnixTimeSeconds() + options.ExpireSeconds },
                { "mc",   new List<object> { mcItem } }
            };

            var jwt = GenerateJwt(payload);
            return $"https://v.kr.kollus.com/s?jwt={jwt}&custom_key={_customKey}";
        }

        /// <summary>DoveRunner 라이선스 서버에 전달할 Inka 라이선스 토큰(암호화된 정책 페이로드)을 생성합니다.</summary>
        /// <param name="userId">재생을 요청하는 사용자 ID</param>
        /// <param name="cid">콘텐츠 식별자(업로드 파일 키)</param>
        /// <param name="streamingType">스트리밍 타입 ("hls"면 FairPlay, 그 외 Widevine)</param>
        /// <returns>Base64로 인코딩된 라이선스 토큰</returns>
        public string CreateInkaPayload(string userId, string cid, string streamingType)
        {
            var now       = DateTime.UtcNow;
            var timestamp = now.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var drmType   = streamingType == "hls" ? "FairPlay" : "Widevine";

            var s = new JavaScriptSerializer();

            // 1. Policy JSON 생성
            var policyObj = new Dictionary<string, object>
            {
                { "playback_policy", new Dictionary<string, object>
                    {
                        { "limit",      true  },
                        { "persistent", false },
                        { "duration",   POLICY_DURATION_SEC }
                    }
                },
                { "allow_mobile_abnormal_device", true },
                { "playready_security_level",     PLAYREADY_SEC_LEVEL }
            };
            var policyJson = s.Serialize(policyObj);

            // 2. AES-256-CBC 암호화 (PKCS7 padding)
            var token = AesEncryptBase64(policyJson, _doveRunnerSiteKey, DOVE_RUNNER_IV);

            // 3. SHA256 hash
            var hashInput = _doveRunnerAccessKey + drmType + _doveRunnerSiteId + userId + cid + token + timestamp;
            var hash      = Sha256Base64(hashInput);

            // 4. payload 조립 → JSON → Base64
            var drmPayload = new Dictionary<string, object>
            {
                { "drm_type",        drmType },
                { "site_id",         _doveRunnerSiteId },
                { "user_id",         userId },
                { "cid",             cid },
                { "token",           token },
                { "timestamp",       timestamp },
                { "hash",            hash },
                { "response_format", "original" },
                { "key_rotation",    false }
            };

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(s.Serialize(drmPayload)));
        }

        private string AesEncryptBase64(string plainText, string key, string iv)
        {
            using (var aes = Aes.Create())
            {
                aes.Mode    = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key     = Encoding.UTF8.GetBytes(key);
                aes.IV      = Encoding.UTF8.GetBytes(iv);

                using (var enc = aes.CreateEncryptor())
                {
                    var bytes     = Encoding.UTF8.GetBytes(plainText);
                    var encrypted = enc.TransformFinalBlock(bytes, 0, bytes.Length);
                    return Convert.ToBase64String(encrypted);
                }
            }
        }

        private string Sha256Base64(string input)
        {
            using (var sha = SHA256.Create())
                return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(input)));
        }

        private string GenerateJwt(Dictionary<string, object> payload) =>
            JwtHelper.Generate(payload, _securityKey);
    }
}
