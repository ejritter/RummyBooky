$projectDir = "c:\Dev\RummyBookyMaui\RummyBooky"
$files = Get-ChildItem -Path $projectDir -Recurse -Include *.xaml, *.cs | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

Write-Host "=================== FULL FORENSIC SCAN ==================="
$violations = @()

foreach ($file in $files) {
    $relativePath = $file.FullName.Substring($projectDir.Length + 1)
    $lines = Get-Content $file.FullName

    # 1. Pure Black/White or Untinted Grays
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line -match '#000000|#FFFFFF|#808080|#CCCCCC|Colors\.White|Colors\.Black|Color\.White|Color\.Black') {
            $msg = "Pure B/W or Untinted Gray in $relativePath (line $($i+1)): $($line.Trim())"
            $violations += $msg
            Write-Host "[FAIL] $msg"
        }
    }

    # 2. Legacy Frame in XAML
    if ($file.Extension -eq ".xaml") {
        $content = Get-Content $file.FullName -Raw
        if ($content -match '<Frame\b') {
            $msg = "Legacy <Frame> control in $relativePath"
            $violations += $msg
            Write-Host "[FAIL] $msg"
        }
    }

    # 3. StaticResource color bindings in non-dictionary XAML files
    if ($file.Extension -eq ".xaml" -and $file.Name -notmatch 'Colors.xaml|Typography.xaml|Dimensions.xaml|Theme.xaml|Styles.xaml') {
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $line = $lines[$i]
            if ($line -match '(Color|BackgroundColor|TextColor|Stroke|Fill|BorderColor|PlaceholderColor|TitleColor|ShadowColor)\s*=\s*"\{StaticResource') {
                $msg = "StaticResource color binding in $relativePath (line $($i+1)): $($line.Trim())"
                $violations += $msg
                Write-Host "[FAIL] $msg"
            }
        }
    }
}

Write-Host "`n=================== SCAN SUMMARY ==================="
Write-Host "Total violations found: $($violations.Count)"
if ($violations.Count -eq 0) {
    Write-Host "VERDICT: CLEAN"
} else {
    Write-Host "VERDICT: INTEGRITY VIOLATION"
}
