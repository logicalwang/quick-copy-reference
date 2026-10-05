$ErrorActionPreference = 'Stop'
$dialogRepo = Split-Path -Parent $PSScriptRoot
$dialogSource = Join-Path $dialogRepo 'portable\source'
$dialogFramework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
if (-not (Test-Path -LiteralPath $dialogFramework)) { $dialogFramework = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319' }
$dialogRoot = Join-Path ([IO.Path]::GetTempPath()) ('CopyReference-dialog-' + [Guid]::NewGuid().ToString('N'))
$dialogRoot = [IO.Path]::GetFullPath($dialogRoot)
$dialogTemp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
if (-not $dialogRoot.StartsWith($dialogTemp, [StringComparison]::OrdinalIgnoreCase)) { throw 'Test directory outside temporary root' }
New-Item -ItemType Directory -Path $dialogRoot | Out-Null
try {
    $dialogExe = Join-Path $dialogRoot 'LanguageDialogTest.exe'
    & (Join-Path $dialogFramework 'csc.exe') /nologo /main:LanguageDialogTest /reference:System.Web.Extensions.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:Microsoft.CSharp.dll ('/resource:' + (Join-Path $dialogSource 'translations.json') + ',translations.json') ('/out:' + $dialogExe) (Join-Path $dialogSource 'Localizer.cs') (Join-Path $dialogSource 'VickyReference.cs') (Join-Path $dialogRepo 'portable\tests\LanguageDialogTest.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Language dialog test compilation failed' }
    & $dialogExe
    if ($LASTEXITCODE -ne 0) { throw 'Bilingual settings dialog test failed' }
} finally {
    # Delete only files created by this test, inside the verified temporary root.
    foreach ($dialogFile in @('LanguageDialogTest.exe', 'reference-settings.json')) {
        $dialogPath = Join-Path $dialogRoot $dialogFile
        if (Test-Path -LiteralPath $dialogPath) { Remove-Item -LiteralPath $dialogPath }
    }
    if (Test-Path -LiteralPath $dialogRoot) { Remove-Item -LiteralPath $dialogRoot }
}
