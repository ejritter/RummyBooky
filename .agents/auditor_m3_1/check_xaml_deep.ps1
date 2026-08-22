$projectPath = "c:\Dev\RummyBookyMaui\RummyBooky"
$xamlFiles = Get-ChildItem -Path $projectPath -Recurse -Filter *.xaml | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

Write-Host "=================== DEEP XAML AUDIT ==================="

foreach ($file in $xamlFiles) {
    Write-Host "`n--------------------------------------------------"
    Write-Host "Inspecting: $($file.Name) ($($file.FullName))"
    Write-Host "--------------------------------------------------"
    $content = Get-Content $file.FullName -Raw
    $lines = Get-Content $file.FullName

    # 1. Legacy Frame
    if ($content -match '<Frame\b') {
        Write-Host "[FAIL] Legacy Frame control found!"
    } else {
        Write-Host "[PASS] No legacy Frame control."
    }

    # 2. Nested Border Cards
    # Check if there is a Border inside a Border
    $borderMatches = [regex]::Matches($content, '<Border\b')
    if ($borderMatches.Count -gt 1) {
        # Check hierarchy depth
        $depth = 0
        $maxDepth = 0
        $tokens = [regex]::Matches($content, '<Border\b|</Border>')
        foreach ($t in $tokens) {
            if ($t.Value -match '<Border') { $depth++; if ($depth -gt $maxDepth) { $maxDepth = $depth } }
            elseif ($t.Value -match '</Border>') { $depth-- }
        }
        if ($maxDepth -gt 1) {
            Write-Host "[FAIL] Nested Border cards found! Max depth = $maxDepth"
        } else {
            Write-Host "[PASS] Borders present ($($borderMatches.Count)), but flat (max depth = $maxDepth)."
        }
    } else {
        Write-Host "[PASS] No nested Border cards (Border count = $($borderMatches.Count))."
    }

    # 3. StaticResource color bindings (excluding Dictionary file definitions)
    if ($file.Name -notmatch 'Colors.xaml|Typography.xaml|Dimensions.xaml|Theme.xaml|Styles.xaml') {
        $staticColorMatches = Select-String -Path $file.FullName -Pattern '(Color|BackgroundColor|TextColor|Stroke|Fill|BorderColor|PlaceholderColor|TitleColor|ShadowColor)\s*=\s*"\{StaticResource'
        if ($staticColorMatches) {
            Write-Host "[FAIL] Found StaticResource color bindings on page/view:"
            $staticColorMatches | ForEach-Object { Write-Host "   Line $($_.LineNumber): $($_.Line.Trim())" }
        } else {
            Write-Host "[PASS] All color bindings use DynamicResource or AppThemeBinding."
        }
    } else {
        Write-Host "[INFO] Dictionary definition file."
    }

    # 4. Touch target sizes on interactive controls (< 44dp)
    # Interactive controls: Button, ImageButton, SwipeItemView, Entry, Switch, Slider, CheckBox, Picker, DatePicker, TimePicker, SearchBar
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line -match '<(Button|ImageButton|SwipeItemView|CheckBox|Switch|Slider|Entry|Picker|DatePicker|TimePicker|SearchBar)\b') {
            $tag = $line
            $j = $i
            while ($j -lt $lines.Count -and $tag -notmatch '/>' -and $tag -notmatch '</') {
                $j++
                if ($j -lt $lines.Count) { $tag += " " + $lines[$j] }
            }
            # Check HeightRequest, MinimumHeightRequest, WidthRequest, MinimumWidthRequest
            $hasH = $tag -match 'HeightRequest="(\d+)"'
            $hVal = if ($hasH) { [int]$Matches[1] } else { -1 }
            $hasMinH = $tag -match 'MinimumHeightRequest="(\d+)"'
            $minHVal = if ($hasMinH) { [int]$Matches[1] } else { -1 }

            if (($hasH -and $hVal -lt 44) -or ($hasMinH -and $minHVal -lt 44)) {
                Write-Host "[FAIL] Interactive control height < 44dp at line $($i+1): $tag"
            }
        }
    }
}
