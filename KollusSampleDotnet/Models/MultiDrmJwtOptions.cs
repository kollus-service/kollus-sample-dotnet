// 멀티 DRM(DoveRunner) 재생 URL 생성 옵션 모델.
// KollusMultiDrmService가 이 옵션으로 drm_policy가 포함된 플레이어 JWT를 조립한다.
namespace KollusSampleDotnet.Models
{
    /// <summary>멀티 DRM 재생 URL 생성 옵션 (콘텐츠 키, 시청자 ID, 스트리밍 타입, 업로드 파일 키)</summary>
    public class MultiDrmJwtOptions
    {
        public string ClientUserId    { get; set; }
        public string MediaContentKey { get; set; }
        public int    ExpireSeconds   { get; set; } = KollusDefaults.DefaultJwtExpireSeconds;
        public string StreamingType   { get; set; } = "dash";
        public string UploadFileKey   { get; set; }
    }
}
