param([string]$ArchivePath)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$version = '0.16.3'
$archiveHash = 'cc3e77a219e0b99618ae3be64c31a566197deedc69c1e136acf52d65d7cf2e79'
$localDir = Join-Path $projectRoot 'Packages/Local'
New-Item -ItemType Directory -Force -Path $localDir | Out-Null
$target = Join-Path $localDir "com.github.homuler.mediapipe-$version.tgz"
if (!(Test-Path -LiteralPath $target)) {
    if ($ArchivePath) { Copy-Item -LiteralPath $ArchivePath -Destination $target }
    else {
        $download = "$target.download"
        Invoke-WebRequest -Uri "https://github.com/homuler/MediaPipeUnityPlugin/releases/download/v$version/com.github.homuler.mediapipe-$version.tgz" -OutFile $download
        if ((Get-FileHash -LiteralPath $download -Algorithm SHA256).Hash.ToLowerInvariant() -ne $archiveHash) { throw 'Archive SHA256 mismatch' }
        Move-Item -LiteralPath $download -Destination $target
    }
}
if ((Get-Item -LiteralPath $target).Length -ne 290414365 -or (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash.ToLowerInvariant() -ne $archiveHash) { throw 'Pinned archive size/hash mismatch' }
$staging = Join-Path $localDir 'extracted'
New-Item -ItemType Directory -Force -Path $staging | Out-Null
& tar -xf $target -C $staging 'package/PackageResources/MediaPipe/hand_landmarker.bytes' 'package/LICENSE.md' 'package/Third Party Notices.md'
if ($LASTEXITCODE -ne 0) { throw 'tar extraction failed' }
$streaming = Join-Path $projectRoot 'Assets/StreamingAssets'
$notices = Join-Path $projectRoot 'ThirdParty/MediaPipe'
New-Item -ItemType Directory -Force -Path $streaming,$notices | Out-Null
$model = Join-Path $staging 'package/PackageResources/MediaPipe/hand_landmarker.bytes'
if ((Get-FileHash -LiteralPath $model -Algorithm SHA256).Hash.ToLowerInvariant() -ne '32d1eab97e80a9a20edb29231e15301ce65abfd0fa9d41cf1757e0ecc8078a4e') { throw 'Model SHA256 mismatch' }
Copy-Item -LiteralPath $model -Destination $streaming
Copy-Item -LiteralPath (Join-Path $staging 'package/LICENSE.md') -Destination $notices
Copy-Item -LiteralPath (Join-Path $staging 'package/Third Party Notices.md') -Destination $notices
$manifestPath = Join-Path $projectRoot 'Packages/manifest.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$manifest.dependencies | Add-Member -MemberType NoteProperty -Name 'com.github.homuler.mediapipe' -Value "file:Local/com.github.homuler.mediapipe-$version.tgz" -Force
$manifest | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $manifestPath -Encoding utf8
Write-Output "Prepared pinned MediaPipe $version; archive and model hashes verified."
