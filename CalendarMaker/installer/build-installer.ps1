param(
  [ValidateSet('Release', 'Debug')]
  [string] $Configuration = 'Release',

  [ValidateSet('win-x64', 'win-arm64')]
  [string] $Runtime = 'win-x64',

  [bool] $SelfContained = $true,

  [string] $IsccPath
)

$ErrorActionPreference = 'Stop'

function Get-ProjectVersion([string] $ProjectPath) {
  try {
    [xml] $xml = Get-Content -LiteralPath $ProjectPath
    $versions = @()
    foreach ($pg in $xml.Project.PropertyGroup) {
      if ($pg.Version) { $versions += [string] $pg.Version }
    }
    $version = $versions | Where-Object { $_ } | Select-Object -First 1
    if ($version) { return $version.Trim() }
  } catch {
    # ignore and fall back
  }
  return (Get-Date -Format 'yyyy.MM.dd')
}

function Find-Iscc([string] $Preferred) {
  if ($Preferred) {
    if (-not (Test-Path -LiteralPath $Preferred)) {
      throw "ISCC.exe not found at: $Preferred"
    }
    return $Preferred
  }

  $cmd = Get-Command -Name 'ISCC.exe' -ErrorAction SilentlyContinue
  if ($cmd) { return $cmd.Source }

  $candidates = @(
    Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'
    Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe'
  ) | Where-Object { $_ -and (Test-Path -LiteralPath $_) }

  $found = $candidates | Select-Object -First 1
  if ($found) { return $found }

  throw "Inno Setup compiler (ISCC.exe) not found. Install Inno Setup 6, or pass -IsccPath."
}

function New-IsccDefine([string] $Name, [string] $Value) {
  if ($Value -match '\s') {
    $Value = '"' + ($Value -replace '"', '""') + '"'
  }
  return "/D$Name=$Value"
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot 'CalendarMaker\CalendarMaker.csproj'
$issPath = Join-Path $PSScriptRoot 'CalendarMaker.iss'

if (-not (Test-Path -LiteralPath $projectPath)) {
  throw "Project not found: $projectPath"
}
if (-not (Test-Path -LiteralPath $issPath)) {
  throw "Inno Setup script not found: $issPath"
}

$publishDir = Join-Path $repoRoot ("dist\publish\{0}" -f $Runtime)
$outputDir = Join-Path $repoRoot 'dist\installer'
$appVersion = Get-ProjectVersion -ProjectPath $projectPath

New-Item -ItemType Directory -Force -Path $publishDir, $outputDir | Out-Null

$selfContainedArg = if ($SelfContained) { 'true' } else { 'false' }

Write-Host "Publishing to: $publishDir"
dotnet publish $projectPath `
  -c $Configuration `
  -r $Runtime `
  --self-contained $selfContainedArg `
  -o $publishDir
if ($LASTEXITCODE -ne 0) {
  throw "dotnet publish failed (exit code: $LASTEXITCODE)"
}

try {
  $iscc = Find-Iscc -Preferred $IsccPath
  Write-Host "Building installer with: $iscc"

  & $iscc $issPath `
    (New-IsccDefine -Name 'PublishDir' -Value $publishDir) `
    (New-IsccDefine -Name 'OutputDir' -Value $outputDir) `
    (New-IsccDefine -Name 'AppVersion' -Value $appVersion) | Write-Host
} catch {
  Write-Warning $_
  $zipPath = Join-Path $outputDir ("CalendarMaker-Portable-{0}-{1}.zip" -f $appVersion, $Runtime)
  Write-Host "ISCC.exe not found; creating portable ZIP instead: $zipPath"
  if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
  Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zipPath -Force
  Write-Host "Install Inno Setup 6 and rerun to generate the installer EXE as well."
}

Write-Host "Done. Check: $outputDir"
