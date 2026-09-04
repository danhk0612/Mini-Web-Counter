# Mini Web Counter

웹에서 제공하는 숫자 값을 사용자가 설정한 항목 이름에 맞춰 주기적으로 확인하고, Windows 바탕화면에서 작은 카드 형태로 상시 확인할 수 있는 경량 카운터 프로그램입니다.

URL이 JSON 숫자 값을 반환하기만 하면 필요한 항목을 골라 카드로 표시할 수 있습니다. 각 항목의 이름, JSON 키, 색상, 표시 순서, 링크, 알림음을 개별 설정할 수 있어 간단한 상태판이나 수치 모니터링 용도로 사용할 수 있습니다.

## 주요 기능

- HTTP/HTTPS URL을 일정 주기로 폴링해 JSON 숫자 확인
- 설정한 JSON 키의 값을 카드 형태로 상시 표시
- 세로형 / 가로형 배치
- 전체 UI 배율 50~200%
- 비활성 시 반투명 표시 옵션 및 투명도(20~100%) 설정
- 비활성 시 Windows 타이틀바 숨김 옵션
- 항목 추가 / 삭제 및 위·아래 순서 변경
- 항목별 표시 ON/OFF
- 항목별 표시 이름, JSON 키, 배경색, 글자색 설정
- 색상은 `#RRGGBB` 형식으로 직접 입력
- 항목별 링크 URL 설정
  - 링크가 설정된 항목에만 링크 아이콘 표시
  - 클릭하면 기본 브라우저로 이동
- 항목별 WAV / MP3 알림음 설정
  - 값이 0이 아니면 다음 성공적인 폴링 전까지 반복 재생
  - 다음 폴링에서 최신 값 기준으로 재생 여부를 다시 판단
  - 카드의 소리 아이콘으로 일시 음소거 가능
  - 음소거 상태는 저장하지 않으며 재시작 시 초기화
- 값이 0이면 카드 배경을 약간 어둡게 표시
- 값이 이전 값과 달라지면 약 1초간 점멸
- 항상 위(TopMost) 표시
- 작업표시줄에는 표시하지 않고 시스템 트레이에만 상주
- Windows 시작 시 자동 실행 ON/OFF 설정
- 설정은 트레이 메뉴에서 접근
- 창을 닫으면 트레이로 숨김
- 중복 실행 차단
- 설정은 실행 파일과 동일한 폴더의 `appsettings.json`에 저장
- Windows 실행 파일에 전용 프로그램 아이콘 포함

## 다운로드 및 실행

GitHub Releases에서 최신 Windows x64 배포본을 받을 수 있습니다.

- `Mini-Web-Counter.exe`: 단일 실행 파일
- `Mini-Web-Counter-vX.Y.Z-win-x64.zip`: 배포 파일 묶음

배포본은 Windows 10/11 x64용 self-contained 빌드이므로 대상 PC에 .NET 런타임을 별도로 설치하지 않아도 됩니다.

처음 실행하면 실행 파일과 같은 폴더에 `appsettings.json`이 생성됩니다. 설정은 트레이 아이콘의 `설정` 메뉴에서 변경합니다.

> 프로그램이 설정 파일을 실행 파일과 같은 폴더에 저장하므로, 쓰기 권한이 있는 폴더에서 실행하는 것을 권장합니다.

## 응답 JSON 예시

설정한 `값 이름`과 같은 JSON 키의 숫자를 표시합니다.

```json
{
  "fire": 2,
  "facility": 262,
  "fault": 35
}
```

예를 들어 항목 이름을 `화재`, 값 이름을 `fire`로 설정하면 화면에는 `화재`라는 카드 제목과 `fire`의 숫자가 표시됩니다.

## 설정 예시

```json
{
  "ProgramName": "Mini Web Counter",
  "ProgramBackgroundColor": "#FFFFFF",
  "DataUrl": "https://example.com/status.php",
  "PollingSeconds": 5,
  "Layout": "Vertical",
  "ScalePercent": 100,
  "DimWhenInactive": false,
  "InactiveOpacityPercent": 75,
  "HideTitleBarWhenInactive": false,
  "StartWithWindows": false,
  "Items": [
    {
      "ValueName": "fire",
      "DisplayName": "화재",
      "BackgroundColor": "#D03939",
      "TextColor": "#FFFFFF",
      "LinkUrl": "https://example.com/fire",
      "SoundFile": "C:\\Sounds\\fire.mp3",
      "Visible": true
    }
  ]
}
```

`Items` 배열의 순서가 메인 화면과 트레이 메뉴의 표시 순서가 됩니다.

## 환경

- Windows 10/11 x64
- C# / WinForms
- .NET 8

## 빌드

```powershell
dotnet build .\Mini-Web-Counter.csproj -c Release
```

self-contained 단일 EXE 배포 예시:

```powershell
dotnet publish .\Mini-Web-Counter.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true
```

## 릴리스

### v1.0.7

- 메인 창을 Windows 작업표시줄에 표시하지 않고 시스템 트레이에만 상주하도록 변경
- 트레이 메뉴의 `화면 표시` 및 트레이 아이콘 더블클릭으로 메인 창 표시 가능

### v1.0.3

- Windows EXE 자체에 프로그램 아이콘이 표시되도록 `app.ico`와 `ApplicationIcon` 설정 추가
- 최종 프로젝트/배포 명칭을 `Mini Web Counter` 기준으로 정리
- README 및 배포 안내 정리

## 라이선스

MIT License
