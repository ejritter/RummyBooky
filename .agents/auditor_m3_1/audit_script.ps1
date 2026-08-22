$projectPath = "c:\Dev\RummyBookyMaui\RummyBooky"
$xamlFiles = Get-ChildItem -Path $projectPath -Recurse -Filter *.xaml | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
$csFiles = Get-ChildItem -Path $projectPath -Recurse -Filter *.cs | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

Write-Host "=== 1. LEGACY FRAME CONTROLS ==="
foreach ($file in $xamlFiles) {
    $matches = Select-String -Path $file.FullName -Pattern "<Frame"
    if ($matches) {
        Write-Host "VIOLATION - Legacy Frame found in: $($file.FullName)"
        $matches | ForEach-Object { Write-Host "   Line $($_.LineNumber): $($_.Line.Trim())" }
    }
}

Write-Host "`n=== 2. NESTED BORDER CARDS ==="
foreach ($file in $xamlFiles) {
    $content = Get-Content $file.FullName -Raw
    if ($content -match '<Border[\s\S]*?<Border') {
        Write-Host "NOTE - Border element present in: $($file.Name)"
    }
}

Write-Host "`n=== 3. UNTINTED GRAYS & PURE BLACK/WHITE ==="
$grayPattern = '#808080|#CCCCCC|#000000|#FFFFFF|#000\b|#FFF\b'
foreach ($file in ($xamlFiles + $csFiles)) {
    $matches = Select-String -Path $file.FullName -Pattern $grayPattern
    if ($matches) {
        Write-Host "VIOLATION - Untinted gray or pure black/white found in: $($file.FullName)"
        $matches | ForEach-Object { Write-Host "   Line $($_.LineNumber): $($_.Line.Trim())" }
    }
}

Write-Host "`n=== 4. COLOR PROPERTIES USING STATICRESOURCE INSTEAD OF DYNAMICRESOURCE ==="
$staticColorPattern = '(Color|BackgroundColor|TextColor|Stroke|Fill|BorderColor|PlaceholderColor|TitleColor|ShadowColor)\s*=\s*"\{StaticResource'
foreach ($file in $xamlFiles) {
    if ($file.Name -in @("Colors.xaml", "Typography.xaml", "Dimensions.xaml")) { continue }
    $matches = Select-String -Path $file.FullName -Pattern $staticColorPattern
    if ($matches) {
        Write-Host "VIOLATION - StaticResource color binding found in $($file.Name):"
        $matches | ForEach-Object { Write-Host "   Line $($_.LineNumber): $($_.Line.Trim())" }
    }
}

Write-Host "`n=== 5. INTERACTIVE TOUCH TARGET SIZE CHECK (< 44dp) ==="
foreach ($file in $xamlFiles) {
    $lines = Get-Content $file.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line -match '<(Button|ImageButton|SwipeItemView|CheckBox|Switch|Slider|Entry|Picker|DatePicker|TimePicker|SearchBar)\b') {
            $controlBlock = $line
            $j = $i
            while ($j -lt $lines.Count -and $controlBlock -notmatch '/>' -and $controlBlock -notmatch '</') {
                $j++
                if ($j -lt $lines.Count) { $controlBlock += " " + $lines[$j] }
            }
            if ($controlBlock -match 'HeightRequest="(\d+)"' -and [int]$Matches[1] -lt 44) {
                Write-Host "VIOLATION - HeightRequest < 44 on interactive control in $($file.Name) line $($i+1): $controlBlock"
            }
            if ($controlBlock -match 'MinimumHeightRequest="(\d+)"' -and [int]$Matches[1] -lt 44) {
                Write-Host "VIOLATION - MinimumHeightRequest < 44 on interactive control in $($file.Name) line $($i+1): $controlBlock"
            }
        }
    }
}

Write-Host "`n=== 6. HARDCODED TEST RESULTS OR FACADES ==="
$facadePattern = 'NotImplementedException|return true;\s*//\s*stub|return false;\s*//\s*stub|//\s*fake|//\s*mock|//\s*hardcoded'
foreach ($file in $csFiles) {
    $matches = Select-String -Path $file.FullName -Pattern $facadePattern
    if ($matches) {
        Write-Host "FLAG - Hardcoded stub or fake found in $($file.FullName):"
        $matches | ForEach-Object { Write-Host "   Line $($_.LineNumber): $($_.Line.Trim())" }
    }
}
