$xamlFiles = Get-ChildItem -Path "c:\Dev\RummyBookyMaui\RummyBooky" -Filter "*.xaml" -Recurse | Where-Object { 
    $_.FullName -notmatch '\\obj\\' -and $_.FullName -notmatch '\\bin\\' 
}

Write-Host "Found $($xamlFiles.Count) XAML files in source tree."

$properties = @("Padding", "Margin", "RowSpacing", "ColumnSpacing", "Spacing")

$dimensionTokens = @{
    "Spacing4" = 4
    "Spacing8" = 8
    "Spacing16" = 16
    "Spacing24" = 24
    "Spacing32" = 32
}

$violations = 0
$totalChecked = 0
$valNum = 0.0

foreach ($file in $xamlFiles) {
    if (Test-Path $file.FullName -PathType Leaf) {
        $content = Get-Content -Path $file.FullName -Raw
        $lines = $content -split "\r?\n"
        
        # 1. Check Attributes on elements: Property="Value"
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $lineNum = $i + 1
            $line = $lines[$i]
            
            foreach ($prop in $properties) {
                # Match Property="Value" or Property='Value'
                $pattern = "\b$prop\s*=\s*`"([^`"]+)`"|\b$prop\s*=\s*'([^']+)'"
                $matches = [regex]::Matches($line, $pattern)
                
                foreach ($match in $matches) {
                    $valStr = if ($match.Groups[1].Value) { $match.Groups[1].Value } else { $match.Groups[2].Value }
                    
                    # Ignore markup extensions or dynamic resources unless we resolve them
                    if ($valStr -match '^\{StaticResource\s+([^}]+)\}$') {
                        $tokenKey = $Matches[1].Trim()
                        if ($dimensionTokens.ContainsKey($tokenKey)) {
                            $val = $dimensionTokens[$tokenKey]
                            $totalChecked++
                            if ($val % 4 -ne 0) {
                                Write-Host "VIOLATION in $($file.Name):Line $lineNum - $prop=`"$valStr`" resolved to $val (not divisible by 4)" -ForegroundColor Red
                                $violations++
                            } else {
                                Write-Host "PASS: $($file.Name):Line $lineNum - $prop=`"$valStr`" ($val)" -ForegroundColor Green
                            }
                        } else {
                            Write-Host "INFO: $($file.Name):Line $lineNum - $prop=`"$valStr`" uses unmapped StaticResource '$tokenKey'"
                        }
                        continue
                    }
                    
                    if ($valStr.StartsWith("{") -and $valStr.EndsWith("}")) {
                        Write-Host "INFO: $($file.Name):Line $lineNum - $prop=`"$valStr`" uses markup extension"
                        continue
                    }
                    
                    # Split value by comma or space
                    $parts = $valStr -split '[\s,]+' | Where-Object { $_ -ne "" }
                    
                    foreach ($part in $parts) {
                        if ([double]::TryParse($part, [ref]$valNum)) {
                            $totalChecked++
                            if ($valNum % 4 -ne 0) {
                                Write-Host "VIOLATION in $($file.Name):Line $lineNum - $prop=`"$valStr`" component '$part' = $valNum (not divisible by 4)" -ForegroundColor Red
                                $violations++
                            } else {
                                Write-Host "PASS: $($file.Name):Line $lineNum - $prop=`"$valStr`" component '$part' = $valNum" -ForegroundColor Green
                            }
                        } else {
                            Write-Host "WARNING in $($file.Name):Line $lineNum - $prop=`"$valStr`" component '$part' could not be parsed as double" -ForegroundColor Yellow
                        }
                    }
                }
            }
        }

        # 2. Check Setters: <Setter Property="Padding" Value="16" />
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $lineNum = $i + 1
            $line = $lines[$i]
            
            foreach ($prop in $properties) {
                $patternSetter = "<Setter\s+Property=`"$prop`"\s+Value=`"([^`"]+)`"\s*/>|<Setter\s+Property=`"$prop`"\s+Value='([^']+)'\s*/>"
                $matchesSetter = [regex]::Matches($line, $patternSetter)
                
                foreach ($match in $matchesSetter) {
                    $valStr = if ($match.Groups[1].Value) { $match.Groups[1].Value } else { $match.Groups[2].Value }
                    
                    if ($valStr -match '^\{StaticResource\s+([^}]+)\}$') {
                        $tokenKey = $Matches[1].Trim()
                        if ($dimensionTokens.ContainsKey($tokenKey)) {
                            $val = $dimensionTokens[$tokenKey]
                            $totalChecked++
                            if ($val % 4 -ne 0) {
                                Write-Host "VIOLATION in $($file.Name):Line $lineNum - Setter $prop=`"$valStr`" resolved to $val (not divisible by 4)" -ForegroundColor Red
                                $violations++
                            } else {
                                Write-Host "PASS: $($file.Name):Line $lineNum - Setter $prop=`"$valStr`" ($val)" -ForegroundColor Green
                            }
                        }
                        continue
                    }
                    
                    if ($valStr.StartsWith("{") -and $valStr.EndsWith("}")) {
                        continue
                    }
                    
                    $parts = $valStr -split '[\s,]+' | Where-Object { $_ -ne "" }
                    
                    foreach ($part in $parts) {
                        if ([double]::TryParse($part, [ref]$valNum)) {
                            $totalChecked++
                            if ($valNum % 4 -ne 0) {
                                Write-Host "VIOLATION in $($file.Name):Line $lineNum - Setter $prop=`"$valStr`" component '$part' = $valNum (not divisible by 4)" -ForegroundColor Red
                                $violations++
                            } else {
                                Write-Host "PASS: $($file.Name):Line $lineNum - Setter $prop=`"$valStr`" component '$part' = $valNum" -ForegroundColor Green
                            }
                        }
                    }
                }
            }
        }
    }
}

Write-Host "----------------------------------------"
Write-Host "Total Spacing Values Checked: $totalChecked"
Write-Host "Total Violations Found: $violations"
