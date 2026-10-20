# Kollus Sample .NET — 플레이어 JWT 발급·콜백·업로드·멀티 DRM 연동 샘플

ASP.NET MVC (.NET Framework 4.8) 기반의 Kollus 플레이어 JWT 발급 및 콜백 처리 샘플입니다.

---

## 개요

Kollus API를 연동하여 플레이어 토큰(JWT)을 발급하고, 재생/DRM/LMS 콜백을 수신하는 예제를 제공합니다.

| 기능 | 설명 |
|------|------|
| Player Demo | 설정된 MCK로 JWT를 즉시 발급하여 iframe 플레이어로 바로 확인 |
| Player Sample | MCK·사용자 ID 등 옵션을 직접 입력해 JWT를 발급하고 재생 |
| Player MultiDRM | 서버가 `Web.config`의 DoveRunner 키로 라이선스 토큰을 생성해 DRM 보호 콘텐츠 재생 |
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
   # Windows
   copy KollusSampleDotnet\Web.config.example KollusSampleDotnet\Web.config

   # macOS / Linux
   cp KollusSampleDotnet/Web.config.example KollusSampleDotnet/Web.config
   ```

2. `Web.config`의 각 값을 실제 계정 정보로 교체합니다.

   | 키 | 설명 |
   |----|------|
   | `Kollus.SecurityKey` | 채널 보안키 (JWT 서명) |
   | `Kollus.CustomKey` | 채널 사용자키 (URL 파라미터) |
   | `Kollus.MediaContentKey` | 기본 미디어 컨텐츠키 (MCK) |
   | `Kollus.NextEpisodeMcks` | 다음 회차 MCK 목록 — 콤마 구분, 재생 순서대로 (`/callback/next-episode`가 현재 MCK의 다음 항목을 반환) |
   | `Kollus.ApiAccessToken` | Kollus Upload API 액세스 토큰 (서버에서만 사용, 업로드 화면에서 입력받지 않음) |
   | `Kollus.CategoryKey` | 업로드 대상 카테고리 키 |
   | `Kollus.DoveRunner.SiteKey` | DoveRunner 사이트 키 (라이선스 정책 AES 암호화) |
   | `Kollus.DoveRunner.AccessKey` | DoveRunner 액세스 키 (라이선스 토큰 해시) |
   | `Kollus.DoveRunner.SiteId` | DoveRunner 사이트 ID (FairPlay 인증서 URL·토큰 `site_id`) |
   | `Kollus.MultiDrm.SecurityKey` | Multi DRM 채널 보안키 |
   | `Kollus.MultiDrm.CustomKey` | Multi DRM 채널 사용자키 |
   | `Kollus.MultiDrm.MediaContentKey` | Multi DRM 기본 MCK |
   | `Kollus.MultiDrm.UploadFileKey` | Multi DRM 기본 업로드 파일키 (라이선스 토큰 `cid`) |

   > `Web.config`는 `.gitignore`에 포함되어 있어 버전 관리에서 제외됩니다.
   > JWT 만료 시간·업로드 URL 유효 시간·워터마크 등 코드 기본값은 `Models/KollusDefaults.cs`에서 관리합니다.

---

## 실행 방법

1. Visual Studio에서 솔루션 파일(`KollusSampleDotnet.slnx`)을 엽니다.
2. NuGet 패키지를 복원합니다. Visual Studio는 빌드 시 자동으로 복원하며, 수동으로 복원하려면 다음 중 하나를 사용합니다.
   - 솔루션 탐색기에서 솔루션 우클릭 → **NuGet 패키지 복원**
   - 명령줄: `nuget restore KollusSampleDotnet.slnx`
     (솔루션 파일을 인식하지 못하는 구버전 `nuget.exe`는 `nuget restore KollusSampleDotnet\KollusSampleDotnet.csproj -SolutionDirectory .`)
3. `F5`를 눌러 IIS Express로 실행합니다.
4. 브라우저에서 IIS Express 프로필에 설정된 포트로 접속합니다 (기본값 예: `https://localhost:44365`).

---

## 주요 파일

| 파일 | 설명 |
|------|------|
| `KollusSampleDotnet/Controllers/HomeController.cs` | 홈 페이지 — 샘플 페이지 바로가기와 API 엔드포인트 목록 |
| `KollusSampleDotnet/Controllers/DemoController.cs` | Player Demo — 기본 MCK로 JWT를 발급해 즉시 재생 |
| `KollusSampleDotnet/Controllers/SampleController.cs` | Player Sample — JWT 발급 UI (채널 키·기본 옵션을 ViewBag으로 전달) |
| `KollusSampleDotnet/Controllers/MultiDrmController.cs` | Multi DRM — 재생 URL 생성(`GeneratePlayUrl`)·라이선스 토큰 미리보기(`GenerateLicenseToken`) |
| `KollusSampleDotnet/Controllers/UploadController.cs` | File Upload UI 페이지 (업로드 API는 `ApiController`) |
| `KollusSampleDotnet/Controllers/CallbackController.cs` | 재생/DRM/LMS/Next Episode 콜백 |
| `KollusSampleDotnet/Controllers/ApiController.cs` | Kollus Upload API 프록시 — 업로드 URL 발급·진행률 조회 (`/api/upload/*`) |
| `KollusSampleDotnet/Controllers/JwtController.cs` | JWT 발급 REST API (`/jwt/create`) |
| `KollusSampleDotnet/Services/JwtHelper.cs` | HMAC-SHA256 JWT 서명 공통 유틸 |
| `KollusSampleDotnet/Services/KollusJwtService.cs` | 플레이어 JWT 생성 |
| `KollusSampleDotnet/Services/KollusMultiDrmService.cs` | DoveRunner 라이선스 토큰(AES-256-CBC + SHA256)과 `drm_policy` JWT 생성 |
| `KollusSampleDotnet/Models/KollusDefaults.cs` | JWT 만료 시간·업로드 URL 유효 시간·워터마크 등 기본값 상수 (단일 출처) |
| `KollusSampleDotnet/Models/KollusJwtOptions.cs` | JWT 옵션 (seek, next_episode 등) |
| `KollusSampleDotnet/Models/MultiDrmJwtOptions.cs` | Multi DRM JWT 옵션 (콘텐츠 키, 시청자 ID, 스트리밍 타입, 업로드 파일 키) |
| `KollusSampleDotnet/Views/` | Razor 뷰 (Bootstrap 3.4.1) |
| `KollusSampleDotnet/Web.config` | 실 계정 정보 (버전 관리 제외) |
| `KollusSampleDotnet/Web.config.example` | 설정 템플릿 |

---

## API 흐름

### 플레이어 JWT 발급

```
GET  /jwt/create?mck=...&cuid=...&expt=3
POST /jwt/create  { Mck, Cuid, Expt, ... }
  ← { Token, Link }
```

`Link`를 iframe `src`에 설정하면 Kollus 플레이어가 로드됩니다. `Expt`는 분 단위이며 서버가 초로 환산합니다.

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

영상 종료 시 플레이어가 다음 회차 재생 URL을 요청합니다. 서버는 `Kollus.NextEpisodeMcks`에서 현재 MCK의 다음 항목을 찾아 SI 링크 JWT를 반환하고, 다음 항목이 없으면 `result: 0`을 반환합니다.

```
플레이어 → POST /callback/next-episode?mck=...&id=...
               ← { result: 1, next_url: "https://v.kr.kollus.com/si?jwt=..." }
```

---

### Multi DRM 재생 URL 생성

채널 키(`Kollus.MultiDrm.*`)와 DoveRunner 키는 서버 `Web.config`에서 읽으며, 라이선스 토큰도 서버가 생성해 JWT의 `drm_policy`에 넣습니다. 화면에서는 MCK·CUID·Upload File Key·Streaming Type만 입력합니다.

```
POST /MultiDrm/GeneratePlayUrl   (application/json)
     { ClientUserId, MediaContentKey, UploadFileKey, StreamingType }
  ← { success, play_url }
```

`play_url`을 iframe `src`에 설정하면 DRM 보호 콘텐츠가 재생됩니다. `StreamingType`은 `dash`(Widevine/PlayReady) 또는 `hls`(FairPlay)입니다.

---

### Multi DRM 라이선스 토큰 생성 (미리보기)

DoveRunner 정책을 AES-256-CBC로 암호화하고 SHA256 해시를 포함한 라이선스 토큰을 생성합니다. 화면의 읽기 전용 미리보기용이며, 재생 시에는 `GeneratePlayUrl`이 토큰을 다시 생성합니다.

```
POST /MultiDrm/GenerateLicenseToken   (application/x-www-form-urlencoded)
     uploadFileKey=...&streamingType=dash|hls
  ← { success, license_token }
```

---

### 파일 업로드

서버(`ApiController`)가 Kollus Upload API를 대신 호출합니다. 액세스 토큰은 요청으로 받지 않고 서버 `Web.config`의 `Kollus.ApiAccessToken`만 사용하므로, 업로드 화면에는 토큰 입력란이 없습니다.

```
POST /api/upload/create-url   (application/json)
     { Title, ExpireTime, CategoryKey, IsEncryptionUpload, IsAudioUpload }
  ← Kollus 응답 { status, data: { upload_url, upload_file_key, progress_url } }

GET  /api/upload/progress/{uploadFileKey}
  ← Kollus 응답 (업로드·인코딩 진행 상태)
```

`Title`은 필수입니다. `ExpireTime`(초)은 60~21600 범위로 보정되며 생략·파싱 실패 시 600초(`KollusDefaults.UploadUrlExpireSeconds`, Kollus 문서 기본값)를 사용합니다. `CategoryKey`를 비우면 `Kollus.CategoryKey`를 사용합니다. `access_token`과 `uploadFileKey`는 URL 인코딩해 Kollus API에 전달합니다. 브라우저는 발급된 `upload_url`로 파일을 직접 multipart POST합니다.

---

## 관련 문서

- [Kollus 개발자 문서](https://support.kollus.com)
- [DoveRunner 연동 가이드](https://docs.doverunner.com)
