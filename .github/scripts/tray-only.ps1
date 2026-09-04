$ErrorActionPreference = 'Stop'

function Replace-Required([string]$path, [string]$old, [string]$new) {
    $text = Get-Content $path -Raw -Encoding UTF8
    if (-not $text.Contains($old)) { throw "Required block not found in $path" }
    $text = $text.Replace($old, $new)
    [System.IO.File]::WriteAllText((Resolve-Path $path), $text, [System.Text.UTF8Encoding]::new($false))
}

Replace-Required 'MainForm.cs' '        ShowInTaskbar = true;' '        ShowInTaskbar = false;'

Replace-Required 'README.md' @'
- 설정은 트레이 메뉴에서 접근
- 창을 닫으면 트레이로 숨김
'@ @'
- 작업표시줄에는 표시하지 않고 시스템 트레이에만 상주
- 설정은 트레이 메뉴에서 접근
- 창을 닫으면 트레이로 숨김
'@

Replace-Required 'README.md' @'
## 릴리스

### v1.0.3
'@ @'
## 릴리스

### v1.0.7

- 메인 창을 Windows 작업표시줄에 표시하지 않고 시스템 트레이에만 상주하도록 변경
- 트레이 메뉴의 `화면 표시` 및 트레이 아이콘 더블클릭으로 메인 창 표시 가능

### v1.0.3
'@
