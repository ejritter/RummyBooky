$projectPath = "c:\Dev\RummyBookyMaui\RummyBooky"
$xamlFiles = Get-ChildItem -Path $projectPath -Recurse -Filter *.xaml | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

Write-Host "=================== ALL INTERACTIVE CONTROLS AUDIT ==================="

foreach ($file in $xamlFiles) {
    $lines = Get-Content $file.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line -match '<(Button|ImageButton|SwipeItemView|CheckBox|Switch|Slider|Entry|Picker|DatePicker|TimePicker|SearchBar|TapGestureRecognizer)\b') {
            $tag = $line.Trim()
            $j = $i
            while ($j -lt $lines.Count -and $tag -notmatch '/>' -and $tag -notmatch '</') {
                $j++
                if ($j -lt $lines.Count) { $tag += " " + $lines[$j].Trim() }
            }
            Write-Host "$($file.Name): line $($i+1): $tag`n"
        }
    }
}
