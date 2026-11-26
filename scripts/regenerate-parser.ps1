param([string]$AntlrJar)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
if (-not $AntlrJar) { $AntlrJar = Join-Path $repoRoot 'artifacts/tooling/antlr-4.13.1-complete.jar' }
if (-not (Test-Path -LiteralPath $AntlrJar)) {
  throw 'Download https://www.antlr.org/download/antlr-4.13.1-complete.jar and pass its path with -AntlrJar.'
}
$AntlrJar = (Resolve-Path -LiteralPath $AntlrJar).Path
$generated = Join-Path $repoRoot 'artifacts/generated-isbl'
$parserDir = Join-Path $repoRoot 'src/IsblCheck.Core/Parser'
New-Item -ItemType Directory -Path $generated -Force | Out-Null
Push-Location $parserDir
try {
  & java -jar $AntlrJar -encoding UTF-8 -Dlanguage=CSharp -package IsblCheck.Core.Parser -listener -no-visitor -o $generated Isbl.g4
  if ($LASTEXITCODE -ne 0) { throw 'ANTLR generation failed.' }
} finally { Pop-Location }
@{
  'IsblLexer.cs' = 'Isbl.g4.lexer.cs'
  'IsblParser.cs' = 'Isbl.g4.parser.cs'
  'IsblBaseListener.cs' = 'Isbl.g4.baselistener.cs'
  'IsblListener.cs' = 'IsblListener.cs'
}.GetEnumerator() | ForEach-Object {
  Copy-Item -LiteralPath (Join-Path $generated $_.Key) -Destination (Join-Path $parserDir $_.Value)
}
