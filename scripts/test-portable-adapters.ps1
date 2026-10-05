$ErrorActionPreference = 'Stop'
$adapterRoot = Split-Path -Parent $PSScriptRoot
$adapterPortable = Join-Path $adapterRoot 'portable'
$adapterFramework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
if (-not (Test-Path -LiteralPath $adapterFramework)) { $adapterFramework = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319' }
$adapterCompiler = Join-Path $adapterFramework 'csc.exe'
$adapterOutput = Join-Path $adapterPortable 'tests\DocumentCaptureAdapterTest.exe'
& $adapterCompiler /nologo /main:DocumentCaptureAdapterTest ('/reference:' + (Join-Path $adapterPortable 'ReferenceCapture.exe')) ('/out:' + $adapterOutput) (Join-Path $adapterPortable 'tests\DocumentCaptureAdapterTest.cs')
if ($LASTEXITCODE -ne 0) { throw 'Adapter test compilation failed' }
Copy-Item -LiteralPath (Join-Path $adapterPortable 'ReferenceCapture.exe') -Destination (Join-Path $adapterPortable 'tests\ReferenceCapture.exe')
& $adapterOutput
if ($LASTEXITCODE -ne 0) { throw 'Adapter source-correlation tests failed' }
