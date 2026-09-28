// Kollus 플레이어 재생 JWT 생성 옵션 모델.
// KollusJwtService가 이 옵션을 읽어 JWT 클레임(mc, cuid, expt, 워터마크 정책 등)을 조립한다.
namespace KollusSampleDotnet.Models
{
    /// <summary>플레이어 재생 JWT 생성 옵션 (콘텐츠 키, 시청자 ID, 만료 시간, 재생 제어 정책)</summary>
    public class KollusJwtOptions
    {
        public string ClientUserId    { get; set; }
        public string MediaContentKey { get; set; }
        public int    ExpireSeconds   { get; set; } = KollusDefaults.DefaultJwtExpireSeconds;
        public bool   SeekEnabled     { get; set; } = true;
        public int    SeekableEnd     { get; set; } = 0;
        public bool   DisablePlayrate { get; set; } = false;
        public string IntroMck        { get; set; }
        public string OutroMck        { get; set; }
        public bool   NextEpisode     { get; set; } = false;
        public PlaySection      PlaySection    { get; set; }
        public SubtitlePolicy   SubtitlePolicy { get; set; }
        public WatermarkingPolicy Watermarking { get; set; }
    }

    /// <summary>재생 구간 제한 — StartTime~EndTime(초) 범위만 재생</summary>
    public class PlaySection
    {
        public int StartTime { get; set; }
        public int EndTime   { get; set; }
    }

    /// <summary>자막 표시 정책 (JWT의 subtitle_policy 클레임)</summary>
    public class SubtitlePolicy
    {
        public bool IsShowable   { get; set; } = false;
        public bool ShowByFilter { get; set; } = false;
    }

    /// <summary>시청자 식별 워터마크 정책 (JWT의 video_watermarking_code_policy 클레임)</summary>
    public class WatermarkingPolicy
    {
        public string CodeKind         { get; set; } = "client_user_id";
        public int    FontSize         { get; set; } = KollusDefaults.WatermarkFontSize;
        public string FontColor        { get; set; } = KollusDefaults.WatermarkFontColor;
        public int    Alpha            { get; set; } = KollusDefaults.WatermarkAlpha;
        public int    ShowTime         { get; set; } = KollusDefaults.WatermarkShowTime;
        public int    HideTime         { get; set; } = KollusDefaults.WatermarkHideTime;
        public bool   EnableHtml5Player { get; set; } = true;
    }
}
