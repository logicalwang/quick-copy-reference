$ErrorActionPreference = 'Stop'
$referenceRoot = Split-Path -Parent $PSScriptRoot
$referenceFramework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
if (-not (Test-Path -LiteralPath $referenceFramework)) { $referenceFramework = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319' }
$referenceCompiler = Join-Path $referenceFramework 'csc.exe'
$referenceUiAssemblies = @('UIAutomationClient.dll','UIAutomationTypes.dll','WindowsBase.dll') | ForEach-Object { '/reference:' + (Join-Path $referenceFramework ('WPF\' + $_)) }
$referenceLocalization = @((Join-Path $PSScriptRoot 'Localizer.cs'), ('/resource:' + (Join-Path $PSScriptRoot 'translations.json') + ',translations.json'))
& $referenceCompiler /nologo /target:winexe /optimize+ /platform:anycpu ('/out:' + (Join-Path $referenceRoot 'ReferenceCapture.exe')) /reference:System.Web.Extensions.dll /reference:System.Windows.Forms.dll /reference:Microsoft.CSharp.dll @referenceUiAssemblies @referenceLocalization (Join-Path $PSScriptRoot 'ReferenceCapture.cs')
if ($LASTEXITCODE -ne 0) { throw 'Reference helper compilation failed' }
& $referenceCompiler /nologo /target:winexe /optimize+ /platform:anycpu ('/out:' + (Join-Path $referenceRoot 'VickyReference.exe')) /reference:System.Web.Extensions.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:Microsoft.CSharp.dll @referenceLocalization (Join-Path $PSScriptRoot 'VickyReference.cs')
if ($LASTEXITCODE -ne 0) { throw 'Portable hotkey tool compilation failed' }
Copy-Item -LiteralPath (Join-Path $referenceRoot 'VickyReference.exe') -Destination (Join-Path $referenceRoot 'CopyReference.exe')
Write-Output 'Built portable reference tool.'
