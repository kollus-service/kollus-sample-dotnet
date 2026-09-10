using KollusSampleDotnet.Models;
using System;
using System.Collections.Generic;

namespace KollusSampleDotnet.Services
{
    public class KollusTokenResult
    {
        public string Token { get; set; }
        public string Link  { get; set; }
    }

    /// <summary>Kollus 플레이어 재생용 JWT를 생성하는 서비스</summary>
    public class KollusJwtService
    {
        private readonly string _securityKey;
        private readonly string _customKey;

        public KollusJwtService(string securityKey, string customKey)
        {
            _securityKey = securityKey;
            _customKey   = customKey;
        }

        /// <summary>옵션을 바탕으로 JWT와 플레이어 재생 링크를 생성합니다.</summary>
        /// <param name="options">재생 옵션(재생구간, 워터마킹, 인트로/아웃트로 등)</param>
        /// <returns>발급된 JWT와 재생 링크가 담긴 결과 객체</returns>
        public KollusTokenResult GenerateToken(KollusJwtOptions options)
        {
            var jwt  = GeneratePlayJwt(options);
            var link = $"https://v.kr.kollus.com/s?jwt={jwt}&custom_key={_customKey}";
            return new KollusTokenResult { Token = jwt, Link = link };
        }

        public string GenerateJwtFromPayload(Dictionary<string, object> payload)
        {
            return GenerateJwt(payload);
        }

        // 옵션에 설정된 값만 조건부로 mc/payload에 채워 넣어 불필요한 필드가 JWT에 남지 않도록 함
        private string GeneratePlayJwt(KollusJwtOptions options)
        {
            var mcItem = new Dictionary<string, object> { { "mckey", options.MediaContentKey } };

            if (!options.SeekEnabled)
                mcItem["seek"] = false;

            if (options.SeekableEnd > 0)
                mcItem["seekable_end"] = options.SeekableEnd;

            if (options.DisablePlayrate)
                mcItem["disable_playrate"] = true;

            if (options.PlaySection != null)
                mcItem["play_section"] = new Dictionary<string, object>
                {
                    { "start_time", options.PlaySection.StartTime },
                    { "end_time",   options.PlaySection.EndTime }
                };

            if (options.SubtitlePolicy != null)
                mcItem["subtitle_policy"] = new Dictionary<string, object>
                {
                    { "is_showable",    options.SubtitlePolicy.IsShowable },
                    { "show_by_filter", options.SubtitlePolicy.ShowByFilter }
                };

            var mcList = new List<object>();
            if (!string.IsNullOrEmpty(options.IntroMck))
                mcList.Add(new Dictionary<string, object> { { "mckey", options.IntroMck }, { "intr",  true } });
            mcList.Add(mcItem);
            if (!string.IsNullOrEmpty(options.OutroMck))
                mcList.Add(new Dictionary<string, object> { { "mckey", options.OutroMck }, { "outro", true } });

            var payload = new Dictionary<string, object>
            {
                { "cuid", options.ClientUserId },
                { "expt", DateTimeOffset.UtcNow.ToUnixTimeSeconds() + options.ExpireSeconds },
                { "mc",   mcList }
            };

            if (options.NextEpisode)
                payload["next_episode"] = true;

            if (options.Watermarking != null)
                payload["video_watermarking_code_policy"] = new Dictionary<string, object>
                {
                    { "code_kind",           options.Watermarking.CodeKind },
                    { "font_size",           options.Watermarking.FontSize },
                    { "font_color",          options.Watermarking.FontColor },
                    { "alpha",               options.Watermarking.Alpha },
                    { "show_time",           options.Watermarking.ShowTime },
                    { "hide_time",           options.Watermarking.HideTime },
                    { "enable_html5_player", options.Watermarking.EnableHtml5Player }
                };

            return GenerateJwt(payload);
        }

        private string GenerateJwt(Dictionary<string, object> payload) =>
            JwtHelper.Generate(payload, _securityKey);
    }
}
