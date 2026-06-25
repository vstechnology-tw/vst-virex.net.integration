param(
    [string]$SitePath = "site"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $SitePath -PathType Container)) {
    throw "Site path not found: $SitePath"
}

$siteRoot = (Resolve-Path -LiteralPath $SitePath).Path
$missing = New-Object System.Collections.Generic.List[string]

$htmlFiles = Get-ChildItem -LiteralPath $siteRoot -Recurse -Filter *.html -File
foreach ($htmlFile in $htmlFiles) {
    $html = Get-Content -LiteralPath $htmlFile.FullName -Raw
    $matches = [regex]::Matches($html, '<img\b[^>]*\bsrc\s*=\s*["'']([^"'']+)["'']', 'IgnoreCase')

    foreach ($match in $matches) {
        $src = $match.Groups[1].Value
        if ($src -match '^(https?:)?//' -or $src -match '^(data|mailto):') {
            continue
        }

        $cleanSrc = ($src -split '[?#]', 2)[0]
        if ([string]::IsNullOrWhiteSpace($cleanSrc)) {
            continue
        }

        $decodedSrc = [System.Uri]::UnescapeDataString($cleanSrc)
        if ($decodedSrc.StartsWith("/")) {
            $relativePath = $decodedSrc.TrimStart("/") -replace '/', [System.IO.Path]::DirectorySeparatorChar
            $assetPath = Join-Path $siteRoot $relativePath
        }
        else {
            $relativePath = $decodedSrc -replace '/', [System.IO.Path]::DirectorySeparatorChar
            $assetPath = Join-Path $htmlFile.DirectoryName $relativePath
        }

        $normalizedPath = [System.IO.Path]::GetFullPath($assetPath)
        if (-not $normalizedPath.StartsWith($siteRoot, [System.StringComparison]::OrdinalIgnoreCase) -or
            -not (Test-Path -LiteralPath $normalizedPath -PathType Leaf)) {
            $siteRelativeHtml = [System.IO.Path]::GetRelativePath($siteRoot, $htmlFile.FullName)
            $missing.Add("${siteRelativeHtml}: missing image asset '${src}' -> ${normalizedPath}")
        }
    }
}

if ($missing.Count -gt 0) {
    $missing | ForEach-Object { Write-Host "Missing documentation image asset: $_" }
    throw "Documentation image asset verification failed with $($missing.Count) missing asset(s)."
}

Write-Host "Verified documentation image assets in $($htmlFiles.Count) HTML file(s)."
