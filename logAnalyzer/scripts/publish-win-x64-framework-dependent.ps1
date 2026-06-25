[CmdletBinding()]
param(
    [string]$OutputDirectory = "artifacts\publish\win-x64-framework-dependent"
)

$ErrorActionPreference = "Stop"

function ConvertFrom-Utf8Base64 {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    return [System.Text.Encoding]::UTF8.GetString(
        [System.Convert]::FromBase64String($Value))
}

$repositoryRoot = [System.IO.Path]::GetFullPath(
    (Split-Path -Parent $PSScriptRoot))
$projectPath = Join-Path `
    $repositoryRoot `
    "src\FFXI_LogAnalyzer.App\FFXI_LogAnalyzer.App.csproj"
$publishRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $repositoryRoot "artifacts\publish"))
$publishDirectory = [System.IO.Path]::GetFullPath(
    (Join-Path $repositoryRoot $OutputDirectory))

if (-not $publishDirectory.StartsWith(
        $publishRoot + [System.IO.Path]::DirectorySeparatorChar,
        [System.StringComparison]::OrdinalIgnoreCase)) {
    $message = ConvertFrom-Utf8Base64 `
        "55m66KGM5YWI44GvIGFydGlmYWN0c1xwdWJsaXNoIOmFjeS4i+OCkuaMh+WumuOBl+OBpuOBj+OBoOOBleOBhA=="
    throw "${message}: $publishDirectory"
}

if (Test-Path -LiteralPath $publishDirectory) {
    Remove-Item -LiteralPath $publishDirectory -Recurse -Force
}

dotnet publish `
    $projectPath `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -p:PublishProfile=win-x64-framework-dependent `
    -o $publishDirectory

if ($LASTEXITCODE -ne 0) {
    $message = ConvertFrom-Utf8Base64 `
        "55m66KGM44Gr5aSx5pWX44GX44G+44GX44Gf44CC57WC5LqG44Kz44O844OJ"
    throw "${message}: $LASTEXITCODE"
}

Get-ChildItem `
    -LiteralPath $publishDirectory `
    -Filter "*.pdb" `
    -File |
    Remove-Item -Force

$executablePath = Join-Path `
    $publishDirectory `
    "FFXI_LogAnalyzer.exe"

if (-not (Test-Path -LiteralPath $executablePath)) {
    $message = ConvertFrom-Utf8Base64 `
        "6YWN5biD55SoZXhl44GM55Sf5oiQ44GV44KM44Gm44GE44G+44Gb44KT"
    throw "${message}: $executablePath"
}

$message = ConvertFrom-Utf8Base64 `
    "55m66KGM44GM5a6M5LqG44GX44G+44GX44Gf"
Write-Host "${message}: $publishDirectory"
