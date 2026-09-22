$ErrorActionPreference = 'Stop'
$source = Join-Path (Split-Path -Parent $PSScriptRoot) 'vscode-reference'
$destination = Join-Path $env:USERPROFILE '.vscode\extensions\vicky-local.copy-reference-bridge-0.1.0'
if (-not (Test-Path -LiteralPath $source)) { throw 'Bridge source directory is missing.' }
New-Item -ItemType Directory -Force -Path $destination | Out-Null
Copy-Item -LiteralPath (Join-Path $source 'extension.js') -Destination $destination -Force
Copy-Item -LiteralPath (Join-Path $source 'package.json') -Destination $destination -Force
Write-Host "Installed Copy Reference Bridge to $destination. Restart VS Code completely."
