namespace KollusSampleDotnet.Models
{
    public class MultiDrmJwtOptions
    {
        public string ClientUserId    { get; set; }
        public string MediaContentKey { get; set; }
        public int    ExpireSeconds   { get; set; } = 7200;
        public string StreamingType   { get; set; } = "dash";
        public string SiteId          { get; set; }
        public string UploadFileKey   { get; set; }
    }
}
