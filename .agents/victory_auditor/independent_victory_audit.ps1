# Refined Independent Victory Audit Script for RummyBooky .NET MAUI Project
$projectDir = "c:\Dev\RummyBookyMaui\RummyBooky"
$xamlFiles = Get-ChildItem -Path $projectDir -Recurse -Include *.xaml | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
$csFiles = Get-ChildItem -Path $projectDir -Recurse -Include *.cs | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
$allFiles = $xamlFiles + $csFiles

Write-Host "=================== REFINED VICTORY AUDIT FORENSIC CHECK ==================="
$violations = @()

# --- R1: Interactive Control Touch Target Audit ---
Write-Host "`n--- Checking R1: Touch Target Sizes of Interactive Controls (>= 44dp) ---"
$interactiveTags = @("Button", "ImageButton", "CheckBox", "DatePicker", "Editor", "Entry", "Picker", "RadioButton", "SearchBar", "TimePicker", "Slider", "Switch", "SwipeItemView")

foreach ($file in $xamlFiles) {
    $relativePath = $file.FullName.Substring($projectDir.Length + 1)
    $lines = Get-Content $file.FullName

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]

        # Check inherent interactive control tags
        foreach ($tag in $interactiveTags) {
            if ($line -match "<\b$tag\b") {
                # Read tag block (up to closing / > or next tag)
                $block = $line
                $j = $i
                while ($block -notmatch '/>|>' -and $j -lt $lines.Count - 1) {
                    $j++
                    $block += " " + $lines[$j]
                }
                
                # Check for explicit HeightRequest < 44 or WidthRequest < 44 without MinimumHeightRequest / MinimumWidthRequest >= 44
                if ($block -match 'HeightRequest\s*=\s*"([0-9.]+)"') {
                    $h = [float]$matches[1]
                    if ($h -lt 44 -and $block -notmatch 'MinimumHeightRequest\s*=\s*"(4[4-9]|[5-9][0-9]|[1-9][0-9]{2,})"') {
                        $violations += "[R1 FAIL] Interactive <$tag> has HeightRequest ($h < 44) without MinHeight >= 44 in $relativePath (line $($i+1)): $($line.Trim())"
                    }
                }
                if ($block -match 'WidthRequest\s*=\s*"([0-9.]+)"') {
                    $w = [float]$matches[1]
                    if ($w -lt 44 -and $block -notmatch 'MinimumWidthRequest\s*=\s*"(4[4-9]|[5-9][0-9]|[1-9][0-9]{2,})"') {
                        $violations += "[R1 FAIL] Interactive <$tag> has WidthRequest ($w < 44) without MinWidth >= 44 in $relativePath (line $($i+1)): $($line.Trim())"
                    }
                }
            }
        }

        # Check for TapGestureRecognizer on non-interactive tags (Grid, Border, Image, Label, etc.)
        if ($line -match 'TapGestureRecognizer') {
            # Inspect parent context lines
            $start = [Math]::Max(0, $i - 15)
            $context = ($lines[$start..$i] -join " ")
            if ($context -match 'HeightRequest\s*=\s*"([0-9.]+)"') {
                $h = [float]$matches[1]
                if ($h -lt 44 -and $context -notmatch 'MinimumHeightRequest') {
                    $violations += "[R1 FAIL] Tapped element has HeightRequest ($h < 44) in $relativePath (line $($i+1))"
                }
            }
            if ($context -match 'WidthRequest\s*=\s*"([0-9.]+)"') {
                $w = [float]$matches[1]
                if ($w -lt 44 -and $context -notmatch 'MinimumWidthRequest') {
                    $violations += "[R1 FAIL] Tapped element has WidthRequest ($w < 44) in $relativePath (line $($i+1))"
                }
            }
        }
    }
}

# --- R2: StackLayout Nesting & Single-Child Wrappers ---
Write-Host "`n--- Checking R2: StackLayout Nesting & Single-Child Wrappers ---"
foreach ($file in $xamlFiles) {
    $relativePath = $file.FullName.Substring($projectDir.Length + 1)
    $content = Get-Content $file.FullName -Raw
    
    # Nested StackLayout check (depth >= 3)
    if ($content -match '<(VerticalStackLayout|HorizontalStackLayout|StackLayout)[\s\S]*?<(VerticalStackLayout|HorizontalStackLayout|StackLayout)[\s\S]*?<(VerticalStackLayout|HorizontalStackLayout|StackLayout)') {
        $violations += "[R2 FAIL] StackLayout nesting depth > 2 in $relativePath"
    }
}

# --- R3: Untinted Colors & Dynamic Resources ---
Write-Host "`n--- Checking R3: Untinted Colors & StaticResource Bindings ---"
foreach ($file in $allFiles) {
    $relativePath = $file.FullName.Substring($projectDir.Length + 1)
    $lines = Get-Content $file.FullName
    
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        
        # Pure B/W or untinted gray hex code search
        # Flag pure black (#000000 / #000), pure white (#FFFFFF / #FFF), untinted grays (#808080, #CCCCCC, #888888, #999999, #AAAAAA)
        if ($line -match '#000000\b|#FFFFFF\b|#808080\b|#CCCCCC\b|Colors\.White\b|Colors\.Black\b|Color\.White\b|Color\.Black\b') {
            $violations += "[R3 FAIL] Untinted/Pure B/W color in $relativePath (line $($i+1)): $($line.Trim())"
        }
        
        # Check StaticResource color bindings in view XAML files (excluding Styles/Theme/Colors dictionaries)
        if ($file.Extension -eq ".xaml" -and $file.Name -notmatch 'Colors.xaml|Typography.xaml|Dimensions.xaml|Theme.xaml|Styles.xaml') {
            if ($line -match '(Color|BackgroundColor|TextColor|Stroke|Fill|BorderColor|PlaceholderColor|TitleColor|ShadowColor)\s*=\s*"\{StaticResource') {
                $violations += "[R3 FAIL] StaticResource color binding in view $relativePath (line $($i+1)): $($line.Trim())"
            }
        }
    }
}

# --- R4: Anti-Patterns (Frame, Nested Borders, 3rd Party, VSM) ---
Write-Host "`n--- Checking R4: Anti-Patterns ---"
foreach ($file in $xamlFiles) {
    $relativePath = $file.FullName.Substring($projectDir.Length + 1)
    $content = Get-Content $file.FullName -Raw
    
    # R4a: Frame check
    if ($content -match '<Frame\b|</Frame>') {
        $violations += "[R4a FAIL] Legacy <Frame> found in $relativePath"
    }
    
    # R4b: Nested Border check
    if ($content -match '<Border[\s\S]*?<Border[\s\S]*?</Border>[\s\S]*?</Border>') {
        $violations += "[R4b FAIL] Nested <Border> card hierarchy found in $relativePath"
    }
    
    # R4d: Third-party toolkits check
    if ($content -match 'telerik|syncfusion|devexpress|infragistics|componentone') {
        $violations += "[R4d FAIL] Third-party toolkit reference found in $relativePath"
    }
}

# --- Cheating & Facade Audit ---
Write-Host "`n--- Checking Cheating / Facades / Dummy Returns ---"
foreach ($file in $csFiles) {
    $relativePath = $file.FullName.Substring($projectDir.Length + 1)
    $content = Get-Content $file.FullName -Raw
    if ($content -match 'throw new NotImplementedException' -or $content -match 'NotImplementedException') {
        $violations += "[FACADE FAIL] NotImplementedException in $relativePath"
    }
}

Write-Host "`n=================== REFINED VICTORY AUDIT SUMMARY ==================="
Write-Host "Total Violations Found: $($violations.Count)"
if ($violations.Count -eq 0) {
    Write-Host "RESULT: CLEAN (0 Violations)"
} else {
    Write-Host "RESULT: VIOLATIONS DETECTED ($($violations.Count))"
    foreach ($v in $violations) {
        Write-Host "  - $v"
    }
}
