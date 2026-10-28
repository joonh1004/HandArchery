param([switch]$SyntheticCheck)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$gamePath = Join-Path $projectRoot 'Builds/Windows/HandArchery.exe'
if (!(Test-Path -LiteralPath $gamePath)) { throw 'Builds/Windows/HandArchery.exe가 없습니다. 먼저 Windows 빌드를 만들어 주세요.' }
$logFolder = Join-Path $projectRoot 'Logs'
New-Item -ItemType Directory -Force -Path $logFolder | Out-Null
$logPath = Join-Path $logFolder ('archery-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.log')
$gameArgs = @('-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-logFile',('"' + $logPath + '"'))
if ($SyntheticCheck) {
    $gameArgs += @('--handarchery-smoke','--handarchery-smoke-quit')
    Write-Output '합성 검사입니다. 열린 게임 창을 클릭해 활성화해 주세요. 실제 손 검증과는 별도입니다.'
}
$gameProcess = Start-Process -FilePath $gamePath -WorkingDirectory (Split-Path $gamePath -Parent) -ArgumentList $gameArgs -PassThru
Write-Output "게임 PID: $($gameProcess.Id) / 로그: $logPath"
if ($SyntheticCheck) {
    $gameProcess.WaitForExit()
    Get-Content -LiteralPath $logPath | Select-String -Pattern 'HANDARCHERY (SYNTHETIC|LIFECYCLE|TIMEOUT)'
    if ($gameProcess.ExitCode -ne 0) { throw "합성 검사 종료 코드: $($gameProcess.ExitCode). 로그를 확인해 주세요." }
    if (!(Select-String -LiteralPath $logPath -Pattern 'HANDARCHERY SYNTHETIC PASS' -Quiet)) { throw '합성 검사 통과 기록이 없습니다.' }
}
