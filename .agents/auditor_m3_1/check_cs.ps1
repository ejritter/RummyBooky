$projectPath = "c:\Dev\RummyBookyMaui\RummyBooky"
$csFiles = Get-ChildItem -Path $projectPath -Recurse -Filter *.cs | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

Write-Host "=== SEARCHING ALL C# FILES FOR STUBS / HARDCODED RESULTS / FACADES ==="
foreach ($file in $csFiles) {
    $lines = Get-Content $file.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line -match 'NotImplementedException|//\s*stub|//\s*todo|//\s*fake|//\s*mock|hardcoded') {
            Write-Host "$($file.Name): line $($i+1): $line"
        }
    }
}
