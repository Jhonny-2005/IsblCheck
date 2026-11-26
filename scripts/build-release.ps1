param([string]$DotNet = 'dotnet')
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
Push-Location $repoRoot
try {
  [xml]$props = Get-Content (Join-Path $repoRoot 'Directory.Build.props') -Raw
  $version = [string]$props.Project.PropertyGroup.Version
  $releaseRoot = Join-Path $repoRoot "artifacts/releases/$version"
  New-Item -ItemType Directory -Path $releaseRoot -Force | Out-Null
  function Invoke-DotNet([string[]]$Arguments) {
    & $DotNet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet command failed ($LASTEXITCODE): $Arguments" }
  }
  $localChecks = Test-Path -LiteralPath (Join-Path $repoRoot 'TestISBL/TestRules.csproj')
  Invoke-DotNet -Arguments @('build', 'IsblCheck.sln', '-c', 'Release', '-v:minimal', '-m:1', '-p:UseSharedCompilation=false', '-p:MSBuildEnableWorkloadResolver=false', '-p:NuGetAudit=false')
  if ($localChecks) {
    Invoke-DotNet -Arguments @('build', 'TestISBL/TestRules.csproj', '-c', 'Release', '-m:1', '-p:UseSharedCompilation=false', '-p:MSBuildEnableWorkloadResolver=false', '-p:NuGetAudit=false')
    Invoke-DotNet -Arguments @('run', '--no-build', '--no-restore', '--project', 'TestISBL/TestRules.csproj', '-c', 'Release', '--', 'TestISBL')
  }
  $products = @('IsblCheck', 'IsblCheck.Agent')
  foreach ($product in $products) {
    $destination = Join-Path $releaseRoot "$product-$version-win-x64"
    Invoke-DotNet -Arguments @('restore', "src/$product/$product.csproj", '-r', 'win-x64', '--ignore-failed-sources', '--disable-parallel', '-m:1', '-p:MSBuildEnableWorkloadResolver=false', '-p:NuGetAudit=false')
    Invoke-DotNet -Arguments @('publish', "src/$product/$product.csproj", '-c', 'Release', '-r', 'win-x64',
      '--self-contained', 'true', '--no-restore', '-o', $destination, '-v:minimal', '-m:1',
      '-p:UseSharedCompilation=false', '-p:DebugType=None', '-p:DebugSymbols=false', '-p:MSBuildEnableWorkloadResolver=false')
    if (-not (Test-Path (Join-Path $destination 'Rules/IsblCheck.BaseRules.dll'))) { throw 'Published rules are missing.' }
    $assemblies = @(Get-ChildItem $destination -Filter 'IsblCheck*.dll') + @(Get-Item (Join-Path $destination 'Rules/IsblCheck.BaseRules.dll'))
    foreach ($assembly in $assemblies) {
      $actual = [Reflection.AssemblyName]::GetAssemblyName($assembly.FullName).Version.ToString()
      if ($actual -ne "$version.0") { throw "Incorrect assembly version $actual in $assembly" }
    }
    if ($localChecks) {
      Invoke-DotNet -Arguments @('run', '--no-build', '--no-restore', '--project', 'TestISBL/TestRules.csproj', '-c', 'Release', '--', '--check-published-ai', $destination)
    }
    Get-ChildItem -LiteralPath $destination -Recurse -File -Filter '*.pdb' | Remove-Item
    Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE') -Destination $destination
    Compress-Archive -Path (Join-Path $destination '*') -DestinationPath "$destination.zip" -Force
  }
  $uiAssembly = Join-Path $releaseRoot "IsblCheck-$version-win-x64/IsblCheck.dll"
  if ($localChecks) {
    Invoke-DotNet -Arguments @('run', '--no-build', '--no-restore', '--project', 'TestISBL/TestRules.csproj', '-c', 'Release', '--', '--check-ui-resources', $uiAssembly)
  }
  $agent = Join-Path $releaseRoot "IsblCheck.Agent-$version-win-x64/IsblCheck.Agent.exe"
  $agentVersion = & $agent --version
  if ($LASTEXITCODE -ne 0 -or $agentVersion.Trim() -ne $version) { throw 'Agent version smoke check failed.' }
  & $agent --help
  if ($LASTEXITCODE -ne 0) { throw 'Agent help smoke check failed.' }
  & $agent --unknown-option 2>$null
  if ($LASTEXITCODE -ne 2) { throw 'Agent argument validation failed.' }
  $sourceFiles = @()
  $sourceExtensions = @('.cs', '.g4', '.csproj', '.resx', '.xaml', '.xshd', '.settings', '.manifest', '.config', '.xsd', '.ico', '.png', '.wixproj', '.wxs')
  foreach ($folder in @('src', 'installer')) {
    $sourceFiles += Get-ChildItem (Join-Path $repoRoot $folder) -File -Recurse -Force |
      Where-Object { $_.FullName -notmatch '[\\/](bin|obj|\.vs|\.git|ТМТ)[\\/]' -and
        ($_.Extension -in $sourceExtensions -or $_.Name -eq 'IsblPrompt.txt') -and $_.Name -notmatch 'Tests?\.cs$' }
  }
  foreach ($name in @('.gitignore', '.gitattributes', 'LICENSE', 'Directory.Build.props', 'Directory.Packages.props',
    'IsblCheck.sln', 'scripts/build-release.ps1', 'scripts/regenerate-parser.ps1')) {
    $sourceFiles += Get-Item -LiteralPath (Join-Path $repoRoot $name) -Force
  }
  Add-Type -AssemblyName System.IO.Compression.FileSystem
  $sourceZip = Join-Path $releaseRoot "ISBLCheck-$version-source.zip"
  $stream = [IO.File]::Open($sourceZip, [IO.FileMode]::Create)
  $archive = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create)
  try {
    foreach ($file in $sourceFiles | Sort-Object FullName -Unique) {
      $relative = $file.FullName.Substring($repoRoot.Length).TrimStart('\', '/').Replace('\', '/')
      [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $relative)
    }
    $readme = $archive.CreateEntry('README.md')
    $writer = [IO.StreamWriter]::new($readme.Open(), [Text.UTF8Encoding]::new($false))
    try {
      $writer.WriteLine(@"
# ISBL Check $version

Статический анализатор ISBL и типовых маршрутов classic Directum. Автор этой версии — KEA. Исходный проект DIRECTUM распространяется по лицензии MIT.

В Windows x64 распакуйте архив IsblCheck-$version-win-x64 и запустите IsblCheck.exe. Консольный агент доступен отдельным архивом.

В анализаторе можно открыть пакет ISX, каталог разработки, базу Directum или выгрузку маршрутов XML/DAT со связанными IMG. В дереве маршрута раздел «Схема и параметры» показывает объявления, привязки и переходы. В отчёте ошибки идут первыми, затем предупреждения и информационные подсказки.

Для сборки в Windows с .NET SDK 10 выполните: dotnet build IsblCheck.sln -c Release
"@)
    } finally { $writer.Dispose() }
  } finally { $archive.Dispose(); $stream.Dispose() }
  Get-ChildItem $releaseRoot -Filter '*.zip' | Sort-Object Name | ForEach-Object {
    (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + $_.Name
  } | Set-Content (Join-Path $releaseRoot 'SHA256SUMS.txt') -Encoding utf8
  Write-Host "Release $version verified: $releaseRoot"
} finally { Pop-Location }
exit 0
