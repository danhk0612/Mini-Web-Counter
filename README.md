# JCMS Mini Monitoring

Windows용 소형 상태 모니터링 프로그램입니다.

## 환경

- Windows 10/11
- .NET 8
- C# / WinForms

## 기능

- 화재 / 설비 / 고장 / 차단 / 예비 상태 카드 표시
- 표시 항목별 ON/OFF
- 항상 위(TopMost) 메인 창
- 트레이 아이콘 및 표시 항목 2차 메뉴
- 설정 창에서 데이터 URL과 폴링 주기 설정
- HTTP/HTTPS GET JSON 폴링
- 창 닫기 시 트레이로 숨김, 트레이 메뉴의 `종료`로 완전 종료

## 응답 JSON

```json
{
  "fire": 2,
  "facility": 262,
  "fault": 35,
  "block": 6421,
  "spare": 0
}
```

## 설정

`appsettings.json`

```json
{
  "DataUrl": "",
  "PollingSeconds": 5,
  "ShowFire": true,
  "ShowFacility": true,
  "ShowFault": true,
  "ShowBlock": true,
  "ShowSpare": true
}
```

설정 창이나 트레이의 표시 항목 메뉴에서 변경한 값은 실행 폴더의 `appsettings.json`에 저장됩니다.

## 빌드

```powershell
dotnet build -c Release
```
