// Kollus 플레이어 재생 JWT 생성 서비스.
// KollusJwtOptions를 Kollus JWT 클레임(cuid, expt, mc, 워터마크 정책 등)으로 변환해 서명하고 재생 링크를 만든다.
using KollusSampleDotnet.Models;
using System;
using System.Collections.Generic;

namespace KollusSampleDotnet.Services
{
    /// <summary>JWT 발급 결과 — 서명된 토큰과 그 토큰이 포함된 플레이어 재생 링크</summary>
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

        /// <summary>채널 키로 서비스를 초기화합니다.</summary>
        /// <param name="securityKey">채널 보안키 — JWT 서명에 사용</param>
        /// <param name="customKey">채널 사용자키 — 재생 링크의 custom_key 파라미터에 사용</param>
        public KollusJwtService(string securityKey, string customKey)
        {
            _securityKey = securityKey;
            _customKey   = customKey;
        }

        /// <summary>옵션을 바탕으로 JWT와 플레이어 재생 링크를 생성합니다.</summary>
        /// <param name="options">재생 옵션(재생구간, 워터마킹, 인트로/아웃트로 등)</param>
        /// <returns>발급된 JWT와 재생 링크가 담긴 결과 객체</returns>
        /// <exception cref="ArgumentNullException">보안키(securityKey)가 null이면 서명 단계에서 발생</exception>
        public KollusTokenResult GenerateToken(KollusJwtOptions options)
        {
            var jwt  = GeneratePlayJwt(options);
            // Kollus 웹 플레이어 URL: /s = 플레이어 페이지, jwt = 재생 정책이 담긴 서명 토큰,
            // custom_key = 채널 사용자키(Kollus가 어느 채널의 보안키로 jwt를 검증할지 식별)
            var link = $"https://v.kr.kollus.com/s?jwt={jwt}&custom_key={_customKey}";
            return new KollusTokenResult { Token = jwt, Link = link };
        }

        /// <summary>
        /// 이미 조립된 클레임 딕셔너리를 그대로 서명해 JWT를 생성합니다.
        /// 재생 옵션과 구조가 다른 콜백 응답(play/drm 콜백의 data 클레임)에 사용합니다.
        /// </summary>
        /// <param name="payload">JWT body에 들어갈 클레임 목록</param>
        /// <returns>채널 보안키로 서명된 JWT 문자열</returns>
        /// <exception cref="ArgumentNullException">보안키(securityKey)가 null이면 서명 단계에서 발생</exception>
        public string GenerateJwtFromPayload(Dictionary<string, object> payload)
        {
            return GenerateJwt(payload);
        }

        // 옵션에 설정된 값만 조건부로 mc/payload에 채워 넣어 불필요한 필드가 JWT에 남지 않도록 함
        private string GeneratePlayJwt(KollusJwtOptions options)
        {
            // mckey: 재생할 미디어 콘텐츠 키(MCK)
            var mcItem = new Dictionary<string, object> { { "mckey", options.MediaContentKey } };

            // seek: false면 플레이어 탐색(스크러빙) 금지 — 기본이 허용이므로 금지할 때만 포함
            if (!options.SeekEnabled)
                mcItem["seek"] = false;

            // seekable_end: 이 시점(초)까지만 탐색 허용 — 0이면 제한 없음이라 생략
            if (options.SeekableEnd > 0)
                mcItem["seekable_end"] = options.SeekableEnd;

            // disable_playrate: true면 재생 속도 조절 UI 비활성화
            if (options.DisablePlayrate)
                mcItem["disable_playrate"] = true;

            // play_section: start_time~end_time(초) 구간만 재생
            if (options.PlaySection != null)
                mcItem["play_section"] = new Dictionary<string, object>
                {
                    { "start_time", options.PlaySection.StartTime },
                    { "end_time",   options.PlaySection.EndTime }
                };

            // subtitle_policy: is_showable = 자막 표시 허용 여부, show_by_filter = 필터 조건에 따른 자막 표시 여부
            if (options.SubtitlePolicy != null)
                mcItem["subtitle_policy"] = new Dictionary<string, object>
                {
                    { "is_showable",    options.SubtitlePolicy.IsShowable },
                    { "show_by_filter", options.SubtitlePolicy.ShowByFilter }
                };

            // mc: 재생 목록 — 인트로(intr: true) → 본편 → 아웃트로(outro: true) 순서로 나열
            var mcList = new List<object>();
            if (!string.IsNullOrEmpty(options.IntroMck))
                mcList.Add(new Dictionary<string, object> { { "mckey", options.IntroMck }, { "intr",  true } });
            mcList.Add(mcItem);
            if (!string.IsNullOrEmpty(options.OutroMck))
                mcList.Add(new Dictionary<string, object> { { "mckey", options.OutroMck }, { "outro", true } });

            var payload = new Dictionary<string, object>
            {
                // cuid: 시청자 식별자(client_user_id) — 콜백 요청과 워터마크 표시에 사용
                { "cuid", options.ClientUserId },
                // expt: 토큰 만료 시각(Unix timestamp, 초)
                { "expt", DateTimeOffset.UtcNow.ToUnixTimeSeconds() + options.ExpireSeconds },
                // mc: 위에서 조립한 미디어 콘텐츠 목록
                { "mc",   mcList }
            };

            // next_episode: true면 본편 종료 시 플레이어가 next-episode 콜백을 호출
            if (options.NextEpisode)
                payload["next_episode"] = true;

            // video_watermarking_code_policy: 시청자 식별 워터마크 정책
            //   code_kind = 표시할 값의 종류(client_user_id → cuid 표시), font_size(px), font_color(HEX),
            //   alpha(0~255 투명도), show_time/hide_time(초 단위 표시·숨김 반복 주기),
            //   enable_html5_player = HTML5 웹 플레이어에도 워터마크 적용 여부
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
