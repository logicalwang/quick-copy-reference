$ErrorActionPreference = 'Stop'
$browserTestRoot = Split-Path -Parent $PSScriptRoot
$browserTestPortable = Join-Path $browserTestRoot 'portable'
$browserTestFramework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
if (-not (Test-Path -LiteralPath $browserTestFramework)) { $browserTestFramework = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319' }
$browserTestOutput = Join-Path $browserTestPortable 'tests\BrowserSelectionClipboardTest.exe'
& (Join-Path $browserTestFramework 'csc.exe') /nologo /reference:System.Windows.Forms.dll /reference:System.Drawing.dll ('/reference:' + (Join-Path $browserTestPortable 'ReferenceCapture.exe')) ('/out:' + $browserTestOutput) (Join-Path $browserTestPortable 'tests\BrowserSelectionClipboardTest.cs')
if ($LASTEXITCODE -ne 0) { throw 'Browser selection test compilation failed' }
Copy-Item -LiteralPath (Join-Path $browserTestPortable 'ReferenceCapture.exe') -Destination (Join-Path $browserTestPortable 'tests\ReferenceCapture.exe')
& $browserTestOutput
if ($LASTEXITCODE -ne 0) { throw 'Browser selection and clipboard tests failed' }
