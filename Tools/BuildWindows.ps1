param([string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
if (!(Test-Path -LiteralPath $UnityPath)) { throw 'Unity 6000.6.7f1 경로를 -UnityPath로 지정해 주세요.' }
if (!(Test-Path -LiteralPath (Join-Path $projectRoot 'Packages/Local/com.github.homuler.mediapipe-0.16.3.tgz'))) { throw '먼저 Tools/PrepareMediaPipe.ps1을 실행해 주세요.' }
$editorRunning = Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.Contains($projectRoot) }
if ($editorRunning) { throw '이 프로젝트를 연 Unity Editor를 먼저 닫아 주세요. 실행 중인 편집기를 자동 종료하지 않습니다.' }
$logFolder = Join-Path $projectRoot 'Logs'
New-Item -ItemType Directory -Force -Path $logFolder | Out-Null
$logPath = Join-Path $logFolder ('build-windows-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.log')
Write-Output '생성 씬을 다시 구성한 뒤 Windows 실행본을 만듭니다. 수동으로 수정한 씬은 먼저 보존해 주세요.'
$buildArgs = @('-batchmode','-nographics','-projectPath',('"' + $projectRoot + '"'),'-executeMethod','HandArchery.EditorTools.BuildTools.BuildWindows','-logFile',('"' + $logPath + '"'))
$buildProcess = Start-Process -FilePath $UnityPath -ArgumentList $buildArgs -WindowStyle Hidden -PassThru -Wait
if ($buildProcess.ExitCode -ne 0) { throw "빌드 실패: exit=$($buildProcess.ExitCode), 로그=$logPath" }
if (!(Select-String -LiteralPath $logPath -Pattern 'HANDARCHERY BUILD: Succeeded' -Quiet)) { throw "성공 기록이 없습니다. 로그=$logPath" }
Write-Output "빌드 완료: $(Join-Path $projectRoot 'Builds/Windows/HandArchery.exe')"
Write-Output "로그: $logPath"
