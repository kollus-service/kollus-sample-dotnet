// 멀티 DRM 서비스: 멀티 DRM 전용 키로 서명한 재생 URL과 DoveRunner 라이선스 토큰(암호화된 라이선스 룰·커스텀 헤더)을 생성한다.
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

        /// <summary>Kollus 채널 키와 DoveRunner 사이트 자격 정보로 서비스를 초기화합니다.</summary>
        /// <param name="securityKey">Kollus 채널 보안키 — 플레이어 JWT 서명</param>
        /// <param name="customKey">Kollus 채널 사용자키 — 재생 링크의 custom_key</param>
        /// <param name="doveRunnerSiteKey">DoveRunner Site Key — 라이선스 정책 AES 암호화 키</param>
        /// <param name="doveRunnerAccessKey">DoveRunner Access Key — 라이선스 토큰 해시 입력</param>
        /// <param name="doveRunnerSiteId">DoveRunner Site ID — 인증서 URL과 토큰 site_id 필드</param>
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
        /// <exception cref="ArgumentNullException">DoveRunner Site Key 또는 Kollus 보안키가 null이면 암호화/서명 단계에서 발생</exception>
        /// <exception cref="CryptographicException">DoveRunner Site Key 길이가 AES 키 규격에 맞지 않으면 발생</exception>
        public string GenerateMultiDrmPlayUrl(MultiDrmJwtOptions options)
        {
            // 라이선스 토큰은 클라이언트 입력을 받지 않고 서버 설정(DoveRunner 키)으로 항상 새로 생성
            var licenseToken = CreateInkaPayload(options.ClientUserId, options.UploadFileKey, options.StreamingType);

            // drm_policy: Kollus 플레이어가 DRM 라이선스를 요청할 때 사용하는 정책 (mc 항목 단위)
            var drmPolicy = new Dictionary<string, object>
            {
                // kind: DRM 종류 — "inka" = Inka Entworks(DoveRunner/PallyCon) 멀티 DRM
                { "kind",           "inka" },
                // streaming_type: 재생 스트림 포맷 — "dash"(Widevine/PlayReady) 또는 "hls"(FairPlay)
                { "streaming_type", options.StreamingType },
                { "data", new Dictionary<string, object>
                    {
                        // license_url: DoveRunner 라이선스 발급 서버 주소
                        { "license_url",     LICENSE_URL },
                        // certificate_url: FairPlay(HLS) 재생 시 필요한 사이트별 인증서 URL
                        { "certificate_url", string.Format(CERT_URL_TEMPLATE, _doveRunnerSiteId) },
                        // custom_header: 플레이어가 라이선스 요청에 추가하는 HTTP 헤더
                        //   key = pallycon-customdata-v2 (DoveRunner 규격 헤더명), value = 암호화된 라이선스 토큰
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
            // 일반 재생과 동일한 Kollus 웹 플레이어 URL 형식 (/s?jwt=&custom_key=)
            return $"https://v.kr.kollus.com/s?jwt={jwt}&custom_key={_customKey}";
        }

        /// <summary>DoveRunner 라이선스 서버에 전달할 Inka 라이선스 토큰(암호화된 정책 페이로드)을 생성합니다.</summary>
        /// <param name="userId">재생을 요청하는 사용자 ID</param>
        /// <param name="cid">콘텐츠 식별자(업로드 파일 키)</param>
        /// <param name="streamingType">스트리밍 타입 ("hls"면 FairPlay, 그 외 Widevine)</param>
        /// <returns>Base64로 인코딩된 라이선스 토큰</returns>
        /// <exception cref="ArgumentNullException">DoveRunner Site Key가 null이면 AES 키 생성 단계에서 발생</exception>
        /// <exception cref="CryptographicException">DoveRunner Site Key 길이가 AES 키 규격(16/24/32바이트)에 맞지 않으면 발생</exception>
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
            // 결합 순서(accessKey + drmType + siteId + userId + cid + token + timestamp)는 DoveRunner 규격으로 고정 —
            // 라이선스 서버가 같은 순서로 재계산해 비교하므로 순서가 바뀌면 검증에 실패한다
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
