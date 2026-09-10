using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace KollusSampleDotnet.Services
{
    internal static class JwtHelper
    {
        /// <summary>
        /// 페이로드를 HMAC-SHA256으로 서명해 JWT 문자열을 생성합니다.
        /// </summary>
        /// <param name="payload">JWT body에 들어갈 클레임 목록 (cuid, expt, mc 등)</param>
        /// <param name="secretKey">서명에 사용할 Kollus Security Key</param>
        /// <returns>header.body.signature 형태의 JWT 문자열</returns>
        internal static string Generate(Dictionary<string, object> payload, string secretKey)
        {
            var s      = new JavaScriptSerializer();
            var header = Base64Url(Encoding.UTF8.GetBytes(s.Serialize(new Dictionary<string, string> { { "alg", "HS256" }, { "typ", "JWT" } })));
            var body   = Base64Url(Encoding.UTF8.GetBytes(s.Serialize(payload)));
            var input  = $"{header}.{body}";
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
                return $"{input}.{Base64Url(hmac.ComputeHash(Encoding.UTF8.GetBytes(input)))}";
        }

        /// URL-safe Base64 인코딩 (패딩 제거, +/- 치환)
        internal static string Base64Url(byte[] input) =>
            Convert.ToBase64String(input).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
