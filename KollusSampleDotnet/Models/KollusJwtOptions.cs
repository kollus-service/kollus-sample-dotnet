namespace KollusSampleDotnet.Models
{
    public class KollusJwtOptions
    {
        public string ClientUserId    { get; set; }
        public string MediaContentKey { get; set; }
        public int    ExpireSeconds   { get; set; } = 7200;
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

    public class PlaySection
    {
        public int StartTime { get; set; }
        public int EndTime   { get; set; }
    }

    public class SubtitlePolicy
    {
        public bool IsShowable   { get; set; } = false;
        public bool ShowByFilter { get; set; } = false;
    }

    public class WatermarkingPolicy
    {
        public string CodeKind         { get; set; } = "client_user_id";
        public int    FontSize         { get; set; } = 10;
        public string FontColor        { get; set; } = "FFFFFF";
        public int    Alpha            { get; set; } = 128;
        public int    ShowTime         { get; set; } = 3;
        public int    HideTime         { get; set; } = 300;
        public bool   EnableHtml5Player { get; set; } = true;
    }
}
