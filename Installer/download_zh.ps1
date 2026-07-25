$urls = @(
    'https://raw.githubusercontent.com/jrsoftware/issrc/main/Files/Languages/Unofficial/ChineseSimplified.isl',
    'https://raw.githubusercontent.com/jrsoftware/issrc/master/Files/Languages/Unofficial/ChineseSimplified.isl',
    'https://raw.githubusercontent.com/kira-96/innosetup-chinese-simplified/master/ChineseSimplified.isl'
)

$outPath = Join-Path $PSScriptRoot "ChineseSimplified_Official.isl"

foreach ($url in $urls) {
    try {
        Write-Host "Trying: $url"
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 20 -ErrorAction Stop
        if ($response.StatusCode -eq 200 -and $response.Content.Length -gt 1000) {
            $utf8Bom = New-Object System.Text.UTF8Encoding $true
            [System.IO.File]::WriteAllText($outPath, $response.Content, $utf8Bom)
            Write-Host "SUCCESS: Downloaded $( $response.Content.Length ) bytes"
            exit 0
        }
    } catch {
        Write-Host "FAILED: $_"
    }
}
Write-Host "All URLs failed"
exit 1