$projectPath = "c:\Dev\RummyBookyMaui\RummyBooky"
$allFiles = Get-ChildItem -Path $projectPath -Recurse -Include *.xaml, *.cs | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

Write-Host "=== SEARCHING FOR PURE BLACK/WHITE AND UNTINTED GRAYS IN ALL FILES ==="
foreach ($file in $allFiles) {
    $lines = Get-Content $file.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        # Check for pure #000000, #FFFFFF, #000, #FFF, #808080, #CCCCCC
        if ($line -match '#000000|#FFFFFF|#808080|#CCCCCC|#000\b|#FFF\b|Colors\.White|Colors\.Black|Color\.White|Color\.Black') {
            Write-Host "$($file.Name): line $($i+1): $($line.Trim())"
        }
    }
}
