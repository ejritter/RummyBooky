# verify_rhythm.ps1
$ErrorActionPreference = 'Stop'

$rootDir = "c:\Dev\RummyBookyMaui"
$xamlFiles = Get-ChildItem -Path $rootDir -Recurse -Filter "*.xaml" | Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' }

Write-Host "Found $($xamlFiles.Count) XAML files to inspect.`n"

$allExtracted = [System.Collections.Generic.List[PSCustomObject]]::new()
$violations = [System.Collections.Generic.List[PSCustomObject]]::new()

foreach ($file in $xamlFiles) {
    $relativePath = $file.FullName.Replace($rootDir, "").TrimStart('\')
    $content = Get-Content -Path $file.FullName -Raw
    
    # 1. Regex to match XML attributes: Padding, Margin, RowSpacing, ColumnSpacing, Spacing
    $attrPattern = '(?i)\b(Padding|Margin|RowSpacing|ColumnSpacing|Spacing)\s*=\s*"([^"]+)"'
    $matches = [regex]::Matches($content, $attrPattern)
    
    foreach ($m in $matches) {
        $propName = $m.Groups[1].Value
        $propValue = $m.Groups[2].Value
        
        $rawTokens = $propValue -split '[\s,]+' | Where-Object { $_ -ne "" }
        
        foreach ($token in $rawTokens) {
            $num = 0.0
            if ([double]::TryParse($token, [ref]$num)) {
                $isMod4 = ($num % 4 -eq 0)
                $obj = [PSCustomObject]@{
                    File = $relativePath
                    Property = $propName
                    Value = $propValue
                    ParsedNumber = $num
                    ValidMod4 = $isMod4
                    Type = "Attribute"
                }
                $allExtracted.Add($obj)
                if (-not $isMod4) {
                    $violations.Add([PSCustomObject]@{
                        File = $relativePath
                        Property = $propName
                        Value = $propValue
                        ParsedNumber = $num
                        Reason = "Number $num is not divisible by 4 (mod 4 = $($num % 4))"
                    })
                }
            } else {
                $obj = [PSCustomObject]@{
                    File = $relativePath
                    Property = $propName
                    Value = $propValue
                    ParsedNumber = $token
                    ValidMod4 = $null
                    Type = "NonNumericRef"
                }
                $allExtracted.Add($obj)
            }
        }
    }

    # 2. Regex to match Setter properties in Styles: <Setter Property="Margin" Value="16" /> or Value="8,16"
    $setterPattern = '(?i)<Setter\s+Property\s*=\s*"(Padding|Margin|RowSpacing|ColumnSpacing|Spacing)"\s+Value\s*=\s*"([^"]+)"\s*/?>'
    $setterMatches = [regex]::Matches($content, $setterPattern)
    foreach ($m in $setterMatches) {
        $propName = $m.Groups[1].Value
        $propValue = $m.Groups[2].Value
        $rawTokens = $propValue -split '[\s,]+' | Where-Object { $_ -ne "" }
        foreach ($token in $rawTokens) {
            $num = 0.0
            if ([double]::TryParse($token, [ref]$num)) {
                $isMod4 = ($num % 4 -eq 0)
                $obj = [PSCustomObject]@{
                    File = $relativePath
                    Property = "Setter:$propName"
                    Value = $propValue
                    ParsedNumber = $num
                    ValidMod4 = $isMod4
                    Type = "Setter"
                }
                $allExtracted.Add($obj)
                if (-not $isMod4) {
                    $violations.Add([PSCustomObject]@{
                        File = $relativePath
                        Property = "Setter:$propName"
                        Value = $propValue
                        ParsedNumber = $num
                        Reason = "Number $num is not divisible by 4 (mod 4 = $($num % 4))"
                    })
                }
            }
        }
    }

    # 3. Check Thickness / Double resources in ResourceDictionaries
    $resourcePattern = '(?i)<(Thickness|x:Double)\s+x:Key\s*=\s*"([^"]+)"[^>]*>([^<]+)</\1>'
    $resMatches = [regex]::Matches($content, $resourcePattern)
    foreach ($m in $resMatches) {
        $tag = $m.Groups[1].Value
        $key = $m.Groups[2].Value
        $val = $m.Groups[3].Value.Trim()
        
        $rawTokens = $val -split '[\s,]+' | Where-Object { $_ -ne "" }
        foreach ($token in $rawTokens) {
            $num = 0.0
            if ([double]::TryParse($token, [ref]$num)) {
                $isMod4 = ($num % 4 -eq 0)
                $obj = [PSCustomObject]@{
                    File = $relativePath
                    Property = "Resource:$key ($tag)"
                    Value = $val
                    ParsedNumber = $num
                    ValidMod4 = $isMod4
                    Type = "Resource"
                }
                $allExtracted.Add($obj)
                if (-not $isMod4) {
                    $violations.Add([PSCustomObject]@{
                        File = $relativePath
                        Property = "Resource:$key ($tag)"
                        Value = $val
                        ParsedNumber = $num
                        Reason = "Resource number $num is not divisible by 4 (mod 4 = $($num % 4))"
                    })
                }
            }
        }
    }
}

Write-Host "=== TOTAL EXTRACTED ITEMS: $($allExtracted.Count) ==="
$numericExtracted = $allExtracted | Where-Object { $_.ValidMod4 -ne $null }
Write-Host "=== NUMERIC SPACING VALUES PARSED: $($numericExtracted.Count) ==="
Write-Host "=== VIOLATIONS FOUND: $($violations.Count) ==="

Write-Host "`nSummary of extracted items per file:"
$allExtracted | Group-Object File | Select-Object Name, Count | Format-Table -AutoSize | Out-String | Write-Host

if ($violations.Count -gt 0) {
    Write-Host "`nVIOLATIONS DETAILS:" -ForegroundColor Red
    $violations | Format-Table -AutoSize | Out-String | Write-Host
} else {
    Write-Host "`nRESULT: 100% of parsed spacing numbers satisfy val % 4 == 0!" -ForegroundColor Green
}

$outputData = @{
    TotalFilesInspected = $xamlFiles.Count
    TotalValuesParsed = $numericExtracted.Count
    TotalViolations = $violations.Count
    Violations = $violations
    Extracted = $allExtracted
}

$outputData | ConvertTo-Json -Depth 5 | Out-File -FilePath "c:\Dev\RummyBookyMaui\.agents\challenger_rhythm_1\script_output.json" -Encoding utf8
