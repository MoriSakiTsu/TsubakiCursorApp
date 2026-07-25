$path = Join-Path $PSScriptRoot "ChineseSimplified.isl"
$content = [System.IO.File]::ReadAllText($path)
$utf8Bom = New-Object System.Text.UTF8Encoding $true
[System.IO.File]::WriteAllText($path, $content, $utf8Bom)
Write-Host "BOM added successfully"