# JCMS Mini Monitoring

Windows용 소형 상태 모니터링 프로그램입니다.

## 환경

- Windows 10/11
- .NET 8
- C# / WinForms

## 기능

- 설정한 항목을 컬러 카드로 표시
- 세로형 / 가로형 배치 전환
- 전체 UI 배율 50~200% 설정
- 항목 추가 / 삭제
- 항목별 표시 ON/OFF
- 항목별 값 이름(JSON 키), 배경 색, 글자 색 설정
- 아이콘 없이 굵고 큰 글자로 값 표시
- 항상 위(TopMost) 메인 창
- 트레이 아이콘 및 표시 항목 2차 메뉴
- 설정 창에서 데이터 URL과 폴링 주기 설정
- HTTP/HTTPS GET JSON 폴링
- 창 닫기 시 트레이로 숨김, 트레이 메뉴의 `종료`로 완전 종료

## 응답 JSON

응답은 JSON 객체이며, 설정한 `값 이름`과 같은 이름의 숫자를 찾아 표시합니다.

예를 들어 항목 값 이름이 `fire`, `facility`, `fault`라면 다음처럼 반환할 수 있습니다.

```json
{
  "fire": 2,
  "facility": 262,
  "fault": 35
}
```

설정에 없는 JSON 값은 무시하고, 설정한 이름이 응답에 없으면 해당 카드에는 `-`가 표시됩니다.

## 설정

설정은 실행 폴더의 `appsettings.json`에 저장됩니다.

```json
{
  "DataUrl": "https://example.com/status.php",
  "PollingSeconds": 5,
  "Layout": "Vertical",
  "ScalePercent": 100,
  "Items": [
    {
      "ValueName": "fire",
      "BackgroundColor": "#D03939",
      "TextColor": "#FFFFFF",
      "Visible": true
    },
    {
      "ValueName": "facility",
      "BackgroundColor": "#318E5A",
      "TextColor": "#FFFFFF",
      "Visible": true
    }
  ]
}
```

`Layout` 값은 `Vertical` 또는 `Horizontal`이며, 일반적으로 설정 창에서 변경하면 됩니다.

## 빌드

```powershell
dotnet build -c Release
```
