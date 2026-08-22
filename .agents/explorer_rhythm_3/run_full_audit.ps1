# run_full_audit.ps1 - Cleaned up regex parsing script

$files = Get-ChildItem -Path "c:\Dev\RummyBookyMaui\RummyBooky" -Recurse -Filter *.xaml | Where-Object { $_.FullName -notmatch '\\(bin|obj|\.git|\.vs)\\' }

$targetProps = @('Padding', 'Margin', 'RowSpacing', 'ColumnSpacing', 'Spacing')

function Test-Compliant ($valStr) {
    if ([string]::IsNullOrWhiteSpace($valStr)) {
        return @{ IsViolation = $false; CompliantValue = $valStr }
    }
    
    if ($valStr -match '^\s*\{.*\}\s*$') {
        return @{ IsViolation = $false; CompliantValue = $valStr; Note = "Resource/Binding expression" }
    }
    
    $rawTokens = [regex]::Matches($valStr, '[-+]?[0-9]*\.?[0-9]+')
    if ($rawTokens.Count -eq 0) {
        return @{ IsViolation = $false; CompliantValue = $valStr; Note = "Non-numeric" }
    }
    
    $isViolation = $false
    $newValStr = $valStr
    
    foreach ($m in $rawTokens) {
        $num = [double]::Parse($m.Value)
        $rem = [Math]::Abs($num % 4)
        if ($rem -gt 0.0001 -and [Math]::Abs($rem - 4) -gt 0.0001) {
            $isViolation = $true
            $nearest = [Math]::Round($num / 4.0) * 4
            $pattern = '(?<![0-9\.])' + [regex]::Escape($m.Value) + '(?![0-9\.])'
            $newValStr = [regex]::Replace($newValStr, $pattern, $nearest.ToString())
        }
    }
    
    return @{
        IsViolation = $isViolation
        CompliantValue = $newValStr
    }
}

$allFindings = @()
$vsmGroupList = @()

foreach ($file in $files) {
    $relPath = $file.FullName.Replace("c:\Dev\RummyBookyMaui\", "")
    $lines = Get-Content -Path $file.FullName
    
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $lineNum = $i + 1
        $line = $lines[$i]
        
        # 1. Check direct attributes: Padding="...", Margin="...", Grid.RowSpacing="...", etc.
        foreach ($prop in $targetProps) {
            $attrPattern = '(?:[A-Za-z0-9_]+\.)?' + $prop + '="([^"]+)"'
            $matches = [regex]::Matches($line, $attrPattern)
            foreach ($match in $matches) {
                $val = $match.Groups[1].Value
                $attrName = $match.Value.Split('=')[0].Trim()
                
                $elemType = "Unknown"
                if ($line -match '^\s*<([A-Za-z0-9_\.:]+)') {
                    $elemType = $Matches[1]
                } else {
                    for ($j = $i - 1; $j -ge [Math]::Max(0, $i - 15); $j--) {
                        if ($lines[$j] -match '<([A-Za-z0-9_\.:]+)') {
                            $elemType = $Matches[1]
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
        
        # 2. Check Setters: <Setter Property="Padding" Value="..." />
        if ($line -match '<Setter\s+') {
            $propMatch = [regex]::Match($line, 'Property="([^"]+)"')
            $valMatch = [regex]::Match($line, 'Value="([^"]+)"')
            
            if ($propMatch.Success -and $valMatch.Success) {
                $propName = $propMatch.Groups[1].Value
                $val = $valMatch.Groups[1].Value
                
                $cleanProp = $propName
                if ($propName -match '\.([^.]+)$') {
                    $cleanProp = $Matches[1]
                }
                
                if ($targetProps -contains $cleanProp -or $targetProps -contains $propName) {
                    $styleTarget = "Style Setter"
                    for ($j = $i - 1; $j -ge [Math]::Max(0, $i - 20); $j--) {
                        if ($lines[$j] -match 'TargetType="([^"]+)"') {
                            $styleTarget = "Style (TargetType: " + $Matches[1] + ")"
                            break
                        }
                    }
                    
                    # Also check for x:Key of Style if available
                    for ($j = $i - 1; $j -ge [Math]::Max(0, $i - 20); $j--) {
                        if ($lines[$j] -match 'x:Key="([^"]+)"') {
                            $styleTarget += " [x:Key=" + $Matches[1] + "]"
                            break
                        }
                    }
                    
                    $chk = Test-Compliant $val
                    $allFindings += [PSCustomObject]@{
                        File = $relPath
                        Line = $lineNum
                        ElementType = $styleTarget
                        Attribute = "Setter Property=""$propName"""
                        CurrentValue = $val
                        IsViolation = $chk.IsViolation
                        CompliantValue = $chk.CompliantValue
                        LineContent = $line.Trim()
                    }
                }
            }
        }
        
        # 3. Check Thickness elements: <Thickness Left="..." Top="..." Right="..." Bottom="..."> or <Thickness>10, 5</Thickness>
        if ($line -match '<Thickness\b([^>]*)>(.*?)</Thickness>') {
            $attrPart = $Matches[1]
            $bodyPart = $Matches[2]
            
            if (![string]::IsNullOrWhiteSpace($bodyPart)) {
                $chk = Test-Compliant $bodyPart
                $allFindings += [PSCustomObject]@{
                    File = $relPath
                    Line = $lineNum
                    ElementType = "Thickness"
                    Attribute = "Thickness Content"
                    CurrentValue = $bodyPart
                    IsViolation = $chk.IsViolation
                    CompliantValue = $chk.CompliantValue
                    LineContent = $line.Trim()
                }
            }
            
            foreach ($tAttr in @('Left', 'Top', 'Right', 'Bottom')) {
                if ($attrPart -match "$tAttr=`"([^`"]+)`"") {
                    $val = $Matches[1]
                    $chk = Test-Compliant $val
                    $allFindings += [PSCustomObject]@{
                        File = $relPath
                        Line = $lineNum
                        ElementType = "Thickness"
                        Attribute = "Thickness.$tAttr"
                        CurrentValue = $val
                        IsViolation = $chk.IsViolation
                        CompliantValue = $chk.CompliantValue
                        LineContent = $line.Trim()
                    }
                }
            }
        }
        
        # 4. Check VisualStateManager Groups
        if ($line -match '<VisualStateGroup\s+([^>]+)') {
            $attrStr = $Matches[1]
            $gName = "Unnamed Group"
            if ($attrStr -match 'x:Name="([^"]+)"' -or $attrStr -match 'Name="([^"]+)"') {
                $gName = $Matches[1]
            }
            
            $parent = "Unknown Parent"
            for ($j = $i - 1; $j -ge [Math]::Max(0, $i - 20); $j--) {
                if ($lines[$j] -match 'TargetType="([^"]+)"') {
                    $parent = "Style (TargetType: " + $Matches[1] + ")"
                    if ($lines[$j] -match 'x:Key="([^"]+)"') {
                        $parent += " [x:Key=" + $Matches[1] + "]"
                    }
                    break
                } elseif ($lines[$j] -match '<([A-Za-z0-9_\.:]+)') {
                    if ($Matches[1] -notmatch 'VisualState') {
                        $parent = $Matches[1]
                        break
                    }
                }
            }
            
            $vsmGroupList += [PSCustomObject]@{
                File = $relPath
                Line = $lineNum
                GroupName = $gName
                ParentElement = $parent
                LineContent = $line.Trim()
            }
        }
    }
}

# Output to Markdown file
$outPath = "c:\Dev\RummyBookyMaui\.agents\explorer_rhythm_3\scan_output.md"
$sb = [System.Text.StringBuilder]::new()

[void]$sb.AppendLine("# XAML Audit Scan Output")
[void]$sb.AppendLine()
[void]$sb.AppendLine("## Summary")
[void]$sb.AppendLine("- Total XAML Files Scanned: $($files.Count)")
[void]$sb.AppendLine("- Total Spacing Properties Checked: $($allFindings.Count)")
$violations = $allFindings | Where-Object { $_.IsViolation -eq $true }
[void]$sb.AppendLine("- Total Spacing Violations (val % 4 != 0): $($violations.Count)")
[void]$sb.AppendLine("- Total VisualStateGroup Tags Found: $($vsmGroupList.Count)")
[void]$sb.AppendLine()

[void]$sb.AppendLine("## Master Spacing Rhythm Violation Index")
[void]$sb.AppendLine("| File Name | Element Type | Line # | Attribute Name | Current Value | Compliant Replacement |")
[void]$sb.AppendLine("| --- | --- | --- | --- | --- | --- |")

foreach ($v in $violations) {
    $f = $v.File
    $e = $v.ElementType
    $l = $v.Line
    $a = $v.Attribute
    $c = $v.CurrentValue
    $r = $v.CompliantValue
    [void]$sb.AppendLine("| $f | $e | $l | $a | $c | $r |")
}

[void]$sb.AppendLine()
[void]$sb.AppendLine("## All Spacing Occurrences (Compliant + Non-Compliant)")
[void]$sb.AppendLine("| File Name | Line # | Element | Attribute | Current Value | Valid (Divisible by 4)? | Compliant Value |")
[void]$sb.AppendLine("| --- | --- | --- | --- | --- | --- | --- |")

foreach ($item in $allFindings) {
    $f = $item.File
    $l = $item.Line
    $e = $item.ElementType
    $a = $item.Attribute
    $c = $item.CurrentValue
    $r = $item.CompliantValue
    $status = if ($item.IsViolation) { "❌ Violation" } else { "✅ Compliant" }
    [void]$sb.AppendLine("| $f | $l | $e | $a | $c | $status | $r |")
}

[void]$sb.AppendLine()
[void]$sb.AppendLine("## VisualStateManager Groups Index")
[void]$sb.AppendLine("| File Name | Line # | Group Name | Parent Element / TargetType | Line Snippet |")
[void]$sb.AppendLine("| --- | --- | --- | --- | --- |")

foreach ($vsm in $vsmGroupList) {
    $f = $vsm.File
    $l = $vsm.Line
    $g = $vsm.GroupName
    $p = $vsm.ParentElement
    $snip = $vsm.LineContent.Replace("|", "\|")
    [void]$sb.AppendLine("| $f | $l | $g | $p | $snip |")
}

[System.IO.File]::WriteAllText($outPath, $sb.ToString())
Write-Host "Scan completed successfully! Found $($violations.Count) violations out of $($allFindings.Count) occurrences. Output saved to $outPath"
