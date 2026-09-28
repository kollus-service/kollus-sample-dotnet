// Kollus 연동 샘플 전반에서 공유하는 기본값 상수 모음.
// JWT 만료 시간, 업로드 URL 유효 시간, 워터마크 기본 옵션 등 매직 넘버를 한곳에서 관리한다.
namespace KollusSampleDotnet.Models
{
    /// <summary>
    /// Kollus 연동 샘플의 기본값 상수.
    /// 컨트롤러·모델·뷰(ViewBag 경유)가 같은 값을 참조하도록 단일 출처로 사용한다.
    /// </summary>
    public static class KollusDefaults
    {
        /// <summary>1분을 초로 환산하는 계수 (/jwt/create의 expt(분) → ExpireSeconds(초) 변환용)</summary>
        public const int SecondsPerMinute = 60;

        /// <summary>플레이어 JWT 옵션(KollusJwtOptions, MultiDrmJwtOptions)의 기본 만료 시간(초) — 2시간</summary>
        public const int DefaultJwtExpireSeconds = 7200;

        /// <summary>Player Demo 페이지에서 발급하는 JWT 만료 시간(초) — 1시간</summary>
        public const int DemoJwtExpireSeconds = 3600;

        /// <summary>Kollus Upload API create-url의 expire_time 기본값(초) — 발급된 업로드 URL 유효 시간 1시간</summary>
        public const int UploadUrlExpireSeconds = 3600;

        /// <summary>/jwt/create의 expt(만료 시간, 분) 기본값</summary>
        public const int DefaultExpireMinutes = 3;

        /// <summary>워터마크 기본 글꼴 크기(px)</summary>
        public const int WatermarkFontSize = 10;

        /// <summary>워터마크 기본 글꼴 색상(HEX, '#' 제외)</summary>
        public const string WatermarkFontColor = "FFFFFF";

        /// <summary>워터마크 기본 투명도(0=투명 ~ 255=불투명)</summary>
        public const int WatermarkAlpha = 128;

        /// <summary>워터마크 표시 유지 시간 기본값(초)</summary>
        public const int WatermarkShowTime = 3;

        /// <summary>워터마크 숨김 유지 시간 기본값(초)</summary>
        public const int WatermarkHideTime = 300;
    }
}
