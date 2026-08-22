# audit_v2.ps1 - Comprehensive XAML spacing rhythm & VisualStateManager scanner

$files = Get-ChildItem -Path "c:\Dev\RummyBookyMaui\RummyBooky" -Recurse -Filter *.xaml | Where-Object { $_.FullName -notmatch '\\(bin|obj|\.git|\.vs)\\' }

$targetProps = @('Padding', 'Margin', 'RowSpacing', 'ColumnSpacing', 'Spacing')

function Test-Compliant ($valStr) {
    if ([string]::IsNullOrWhiteSpace($valStr)) {
        return @{ IsViolation = $false; CompliantValue = $valStr }
    }
    
    if ($valStr -match '^\s*\{.*\}\s*$') {
        return @{ IsViolation = $false; CompliantValue = $valStr }
    }
    
    $rawTokens = [regex]::Matches($valStr, '[-+]?[0-9]*\.?[0-9]+')
    if ($rawTokens.Count -eq 0) {
        return @{ IsViolation = $false; CompliantValue = $valStr }
    }
    
    $isViolation = $false
    $newValStr = $valStr
    
    foreach ($m in $rawTokens) {
        $num = [double]::Parse($m.Value)
        $rem = [Math]::Abs($num % 4)
        if ($rem -gt 0.0001 -and [Math]::Abs($rem - 4) -gt 0.0001) {
            $isViolation = $true
            $nearest = [Math]::Round($num / 4.0) * 4
            # Replace exact token in string
            $pattern = "(?<![0-9\.])" + [regex]::Escape($m.Value) + "(?![0-9\.])"
            $newValStr = [regex]::Replace($newValStr, $pattern, $nearest.ToString())
        }
    }
    
    return @{
        IsViolation = $isViolation
        CompliantValue = $newValStr
    }
}

$allFindings = @()
$vsmFindings = @()

foreach ($file in $files) {
    $relPath = $file.FullName.Replace("c:\Dev\RummyBookyMaui\", "")
    $lines = Get-Content -Path $file.FullName
    
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $lineNum = $i + 1
        $line = $lines[$i]
        
        # 1. Direct Attributes (e.g. Padding="...", Margin="...", Grid.RowSpacing="...")
        foreach ($prop in $targetProps) {
            # Match attribute like Padding="..." or Grid.Padding="..."
            $attrRegex = "(?:[A-Za-z0-9_]+\.)?$prop\s*=\s*`"([^`"]+)`""
            $matches = [regex]::Matches($line, $attrRegex)
            foreach ($match in $matches) {
                $val = $match.Groups[1].Value
                $attrName = $match.Value.Split('=')[0].Trim()
                
                # Determine element type
                $elemType = "Unknown"
                if ($line -match '^\s*<([A-Za-z0-9_\.:]+)') {
                    $elemType = $matches[1]
                } else {
                    for ($j = $i - 1; $j -ge [Math]::Max(0, $i - 15); $j--) {
                        if ($lines[$j] -match '<([A-Za-z0-9_\.:]+)') {
                            $elemType = $matches[1]
                            break
                        }
                    }
                }
                
                $chk = Test-Compliant $val
                $allFindings += [PSCustomObject]@{
                    File = $relPath
                    Line = $lineNum
                    ElementType = $elemType
                    Attribute = $attrName
                    CurrentValue = $val
                    IsViolation = $chk.IsViolation
                    CompliantValue = $chk.CompliantValue
                    LineContent = $line.Trim()
                }
            }
        }
        
        # 2. Setter tags (e.g. <Setter Property="Padding" Value="..." /> or <Setter Property="Grid.RowSpacing" Value="..." />)
        if ($line -match '<Setter\s+') {
            $propMatch = [regex]::Match($line, 'Property\s*=\s*`"([^`"]+)`"')
            $valMatch = [regex]::Match($line, 'Value\s*=\s*`"([^`"]+)`"')
            
            if ($propMatch.Success -and $valMatch.Success) {
                $propName = $propMatch.Groups[1].Value
                $val = $valMatch.Groups[1].Value
                
                # Check if propName ends with or equals targetProp
                $cleanProp = $propName
                if ($propName -match '\.([^.]+)$') {
                    $cleanProp = $matches[1]
                }
                
                if ($targetProps -contains $cleanProp -or $targetProps -contains $propName) {
                    $styleTarget = "Setter ($propName)"
                    for ($j = $i - 1; $j -ge [Math]::Max(0, $i - 15); $j--) {
                        if ($lines[$j] -match 'TargetType\s*=\s*`"([^`"]+)`"') {
                            $styleTarget = "Style Target: " + $matches[1]
                            break
                        }
                    }
                    
                    $chk = Test-Compliant $val
                    $allFindings += [PSCustomObject]@{
                        File = $relPath
                        Line = $lineNum
                        ElementType = $styleTarget
                        Attribute = "Setter Property=`"$propName`""
                        CurrentValue = $val
                        IsViolation = $chk.IsViolation
                        CompliantValue = $chk.CompliantValue
                        LineContent = $line.Trim()
                    }
                }
            }
        }
        
        # 3. VisualStateManager scanning
        if ($line -match '<VisualStateGroup\s+|x:Name\s*=\s*`"CommonStates`"|<VisualState\s+') {
            $vsmFindings += [PSCustomObject]@{
                File = $relPath
                Line = $lineNum
                LineContent = $line.Trim()
            }
        }
    }
}

Write-Host "=========================================="
Write-Host "ALL SPACING OCCURRENCES FOUND: $($allFindings.Count)"
Write-Host "VIOLATIONS FOUND (val % 4 != 0): $(($allFindings | Where-Object { $_.IsViolation -eq $true }).Count)"
Write-Host "=========================================="
Write-Host ""
Write-Host "--- ALL VIOLATIONS LIST ---"
$allFindings | Where-Object { $_.IsViolation -eq $true } | Format-Table -AutoSize File, Line, ElementType, Attribute, CurrentValue, CompliantValue

Write-Host ""
Write-Host "--- ALL OCCURRENCES LIST ---"
$allFindings | Format-Table -AutoSize File, Line, ElementType, Attribute, CurrentValue, IsViolation, CompliantValue

Write-Host ""
Write-Host "--- VISUAL STATE MANAGER OCCURRENCES ---"
$vsmFindings | Format-Table -AutoSize File, Line, LineContent
