# Audit script for XAML spacing rhythm & VisualStateManager duplication

$files = Get-ChildItem -Path "c:\Dev\RummyBookyMaui\RummyBooky" -Recurse -Filter *.xaml | Where-Object { $_.FullName -notmatch '\\(bin|obj|\.git|\.vs)\\' }

$targetProps = @('Padding', 'Margin', 'RowSpacing', 'ColumnSpacing', 'Spacing')

function Get-CompliantVal ($numStr) {
    if ([double]::TryParse($numStr, [ref]$val)) {
        if ($val % 4 -ne 0) {
            # Round to nearest multiple of 4
            $rounded = [Math]::Round($val / 4.0) * 4
            # If original was integer, return int, else double
            if ($numStr -notmatch '\.') {
                return [int]$rounded
            } else {
                return $rounded
            }
        } else {
            return $numStr
        }
    }
    return $numStr
}

function Check-ValueString ($valStr) {
    # If resource or binding, return intact analysis
    if ($valStr.StartsWith('{') -or $valStr.StartsWith('OnPlatform')) {
        return @{ IsViolation = $false; Compliant = $valStr; Details = "Resource/Binding reference" }
    }
    
    # Split by comma or whitespace
    $parts = $valStr -split '[\s,]+' | Where-Object { $_ -ne '' }
    $isViolation = $false
    $compliantParts = @()
    
    foreach ($p in $parts) {
        $v = 0.0
        if ([double]::TryParse($p, [ref]$v)) {
            if ($v % 4 -ne 0) {
                $isViolation = $true
                $comp = [Math]::Round($v / 4.0) * 4
                $compliantParts += $comp
            } else {
                $compliantParts += $p
            }
        } else {
            $compliantParts += $p
        }
    }
    
    # Reconstruct compliant value preserving original formatting style
    if ($valStr -match ',\s*') {
        $compliantStr = $compliantParts -join ', '
    } else {
        $compliantStr = $compliantParts -join ','
    }
    
    return @{
        IsViolation = $isViolation
        Compliant = $compliantStr
        OriginalParts = $parts
    }
}

$results = @()
$vsmList = @()

foreach ($file in $files) {
    $relPath = $file.FullName.Replace("c:\Dev\RummyBookyMaui\", "")
    $lines = Get-Content -Path $file.FullName
    
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $lineNum = $i + 1
        $line = $lines[$i]
        
        # Check inline attributes
        foreach ($prop in $targetProps) {
            # Match Property="Value"
            $pattern = "$prop\s*=\s*`"([^`"]+)`""
            if ($line -match $pattern) {
                $val = $matches[1]
                
                # Determine element type
                $elemType = "Unknown"
                if ($line -match '^\s*<([A-Za-z0-9_\.:]+)') {
                    $elemType = $matches[1]
                } else {
                    # Look up preceding lines for element tag
                    for ($j = $i - 1; $j -ge [Math]::Max(0, $i - 10); $j--) {
                        if ($lines[$j] -match '<([A-Za-z0-9_\.:]+)') {
                            $elemType = $matches[1]
                            break
                        }
                    }
                }
                
                $chk = Check-ValueString $val
                $results += [PSCustomObject]@{
                    File = $relPath
                    Line = $lineNum
                    ElementType = $elemType
                    Attribute = $prop
                    CurrentValue = $val
                    IsViolation = $chk.IsViolation
                    CompliantValue = $chk.Compliant
                    LineContent = $line.Trim()
                }
            }
        }
        
        # Check Setters: <Setter Property="Padding" Value="..." /> or Value before Property
        if ($line -match '<Setter\s+Property\s*=\s*`"([^`"]+)`"\s+Value\s*=\s*`"([^`"]+)`"') {
            $prop = $matches[1]
            $val = $matches[2]
            if ($targetProps -contains $prop) {
                # Determine target style type if possible
                $styleTarget = "Style Setter"
                for ($j = $i - 1; $j -ge [Math]::Max(0, $i - 15); $j--) {
                    if ($lines[$j] -match 'TargetType\s*=\s*`"([^`"]+)`"') {
                        $styleTarget = "Style (" + $matches[1] + ")"
                        break
                    }
                }
                
                $chk = Check-ValueString $val
                $results += [PSCustomObject]@{
                    File = $relPath
                    Line = $lineNum
                    ElementType = $styleTarget
                    Attribute = "Setter:$prop"
                    CurrentValue = $val
                    IsViolation = $chk.IsViolation
                    CompliantValue = $chk.Compliant
                    LineContent = $line.Trim()
                }
            }
        } elseif ($line -match '<Setter\s+Value\s*=\s*`"([^`"]+)`"\s+Property\s*=\s*`"([^`"]+)`"') {
            $val = $matches[1]
            $prop = $matches[2]
            if ($targetProps -contains $prop) {
                $styleTarget = "Style Setter"
                for ($j = $i - 1; $j -ge [Math]::Max(0, $i - 15); $j--) {
                    if ($lines[$j] -match 'TargetType\s*=\s*`"([^`"]+)`"') {
                        $styleTarget = "Style (" + $matches[1] + ")"
                        break
                    }
                }
                
                $chk = Check-ValueString $val
                $results += [PSCustomObject]@{
                    File = $relPath
                    Line = $lineNum
                    ElementType = $styleTarget
                    Attribute = "Setter:$prop"
                    CurrentValue = $val
                    IsViolation = $chk.IsViolation
                    CompliantValue = $chk.Compliant
                    LineContent = $line.Trim()
                }
            }
        }
        
        # Check VisualStateManager
        if ($line -match '<VisualStateGroup\s+([^>]+)') {
            $attrStr = $matches[1]
            $groupName = "Unnamed"
            if ($attrStr -match 'x:Name\s*=\s*`"([^`"]+)`"' -or $attrStr -match 'Name\s*=\s*`"([^`"]+)`"') {
                $groupName = $matches[1]
            }
            
            # Find parent element
            $parentElement = "Unknown"
            for ($j = $i - 1; $j -ge [Math]::Max(0, $i - 20); $j--) {
                if ($lines[$j] -match '<([A-Za-z0-9_\.:]+)') {
                    $parentElement = $matches[1]
                    break
                }
            }
            
            $vsmList += [PSCustomObject]@{
                File = $relPath
                Line = $lineNum
                GroupName = $groupName
                ParentElement = $parentElement
                LineContent = $line.Trim()
            }
        }
    }
}

Write-Host "--- SPACING RHYTHM SCAN RESULTS ---"
Write-Host "Total property occurrences found: $($results.Count)"
$violations = $results | Where-Object { $_.IsViolation -eq $true }
Write-Host "Total violations (val % 4 != 0): $($violations.Count)"
Write-Host ""

$violations | Format-Table -AutoSize File, Line, ElementType, Attribute, CurrentValue, CompliantValue

Write-Host "--- VISUAL STATE MANAGERS FOUND ---"
$vsmList | Format-Table -AutoSize File, Line, GroupName, ParentElement
