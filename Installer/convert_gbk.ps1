$path = Join-Path $PSScriptRoot "ChineseSimplified.isl"
$content = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
$gbk = [System.Text.Encoding]::GetEncoding(936)
[System.IO.File]::WriteAllText($path, $content, $gbk)
Write-Host "Converted to GBK successfully"