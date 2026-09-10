# Kollus Sample .NET

ASP.NET MVC (.NET Framework 4.8) 기반의 Kollus 플레이어 JWT 발급 및 콜백 처리 샘플입니다.

---

## 개요

Kollus API를 연동하여 플레이어 토큰(JWT)을 발급하고, 재생/DRM/LMS 콜백을 수신하는 예제를 제공합니다.

| 기능 | 설명 |
|------|------|
| Player Demo | 설정된 MCK로 JWT를 즉시 발급하여 iframe 플레이어로 바로 확인 |
| Player Sample | MCK·사용자 ID 등 옵션을 직접 입력해 JWT를 발급하고 재생 |
| Player MultiDRM | DoveRunner 라이선스 토큰과 함께 DRM 보호 콘텐츠 재생 |
| File Upload | Kollus Upload API로 동영상·오디오 파일 업로드 |

---

## 사전 요구사항

- Visual Studio 2022 이상
- .NET Framework 4.8
- IIS Express (Visual Studio에 포함)
- Kollus 계정 및 채널 설정

---

## 환경 설정

1. `Web.config.example`을 복사해 `Web.config`로 저장합니다.

   ```
   copy KollusSampleDotnet\Web.config.example KollusSampleDotnet\Web.config
   ```

2. `Web.config`의 각 값을 실제 계정 정보로 교체합니다.

   | 키 | 설명 |
   |----|------|
   | `Kollus.SecurityKey` | 채널 보안키 (JWT 서명) |
   | `Kollus.CustomKey` | 채널 사용자키 (URL 파라미터) |
   | `Kollus.MediaContentKey` | 기본 미디어 컨텐츠키 (MCK) |
   | `Kollus.ApiAccessToken` | Kollus Upload API 액세스 토큰 |
   | `Kollus.CategoryKey` | 업로드 대상 카테고리 키 |
   | `Kollus.DoveRunner.SiteKey` | DoveRunner 사이트 키 |
   | `Kollus.DoveRunner.AccessKey` | DoveRunner 액세스 키 |
   | `Kollus.MultiDrm.SecurityKey` | Multi DRM 채널 보안키 |
   | `Kollus.MultiDrm.CustomKey` | Multi DRM 채널 사용자키 |
   | `Kollus.MultiDrm.MediaContentKey` | Multi DRM 기본 MCK |
   | `Kollus.MultiDrm.SiteId` | Multi DRM 사이트 ID |
   | `Kollus.MultiDrm.UploadFileKey` | Multi DRM 업로드 파일키 |

   > `Web.config`는 `.gitignore`에 포함되어 있어 버전 관리에서 제외됩니다.

---

## 실행 방법

1. Visual Studio에서 솔루션 파일(`KollusSampleDotnet.slnx`)을 엽니다.
2. `F5`를 눌러 IIS Express로 실행합니다.
3. 브라우저에서 IIS Express 프로필에 설정된 포트로 접속합니다 (기본값 예: `https://localhost:44365`).

---

## 주요 파일

```
KollusSampleDotnet/
├── Controllers/
│   ├── HomeController.cs          # 홈 페이지
│   ├── DemoController.cs          # Player Demo — 즉시 재생 확인
│   ├── SampleController.cs        # Player Sample — JWT 발급 UI
│   ├── MultiDrmController.cs      # Multi DRM — DoveRunner 연동
│   ├── UploadController.cs        # 파일 업로드
│   ├── CallbackController.cs      # 재생/DRM/LMS/Next Episode 콜백
│   ├── ApiController.cs           # 업로드 URL 생성 REST API
│   └── JwtController.cs           # JWT 발급 REST API (/jwt/create)
├── Services/
│   ├── JwtHelper.cs               # HMAC-SHA256 JWT 서명 공통 유틸
│   ├── KollusJwtService.cs        # 플레이어 JWT 생성
│   └── KollusMultiDrmService.cs   # AES-256-CBC 라이선스 토큰 생성
├── Models/
│   ├── KollusJwtOptions.cs        # JWT 옵션 (seek, next_episode 등)
│   └── MultiDrmJwtOptions.cs      # Multi DRM JWT 옵션
├── Views/                         # Razor 뷰 (Bootstrap 3.4.1)
├── Web.config                     # 실 계정 정보 (버전 관리 제외)
└── Web.config.example             # 설정 템플릿
```

---

## API 흐름

### 플레이어 JWT 발급

```
GET  /jwt/create?mck=...&cuid=...&expt=3
POST /jwt/create  { mck, cuid, expt, ... }
  ← { Token, Link }
```

`Link`를 iframe `src`에 설정하면 Kollus 플레이어가 로드됩니다.

---

### 재생 콜백 (Play Callback)

플레이어가 재생을 시작할 때 호출합니다. kind 값에 따라 응답 형식이 다릅니다.

```
플레이어 → POST /callback/play   { kind, media_content_key, client_user_id, ... }
            ← JWT { data: { result, expiration_date } }
```

| kind | 설명 |
|------|------|
| 1 | 재생 허용 여부 및 만료일 응답 |
| 3 | 최종 재생 승인 응답 |

---

### DRM 콜백

오프라인 DRM 콘텐츠 다운로드 및 재생 시 플레이어가 호출합니다. 요청에는 복수의 콘텐츠 정보가 배열로 포함되며, kind 값에 따라 처리합니다.

```
플레이어 → POST /callback/drm   { items: [{ kind, media_content_key, ... }] }
            ← JWT { data: [{ kind, media_content_key, result }] }
```

| kind | 설명 |
|------|------|
| 1 | 다운로드 허용 여부 및 만료 정책 응답 |
| 2 | 다운로드 완료 후 처리 응답 |
| 3 | 오프라인 재생 확인 응답 |

---

### LMS 콜백

플레이어가 재생 진도를 주기적으로 전송합니다. 수신된 데이터는 `logs/lms.log`에 JSON Lines 형식으로 기록됩니다.

```
플레이어 → POST /callback/lms   { json_data: "{ user_info, content_info, block_info }" }
            ← HTTP 200
```

---

### Next Episode 콜백

영상 종료 시 플레이어가 다음 회차 재생 URL을 요청합니다. 서버는 다음 MCK의 SI 링크 JWT를 반환합니다.

```
플레이어 → POST /callback/next-episode?mck=...&id=...
               ← { result: 1, next_url: "https://v.kr.kollus.com/si?jwt=..." }
```

---

### Multi DRM 라이선스 토큰 생성

DoveRunner 정책을 AES-256-CBC로 암호화하고 SHA256 해시를 포함한 라이선스 토큰을 생성합니다.

```
POST /MultiDrm/GenerateLicenseToken
     { siteId, uploadFileKey, streamingType }
  ← { success, license_token }
```

---

## 관련 문서

- [Kollus 개발자 문서](https://support.kollus.com)
- [DoveRunner 연동 가이드](https://docs.doverunner.com)
