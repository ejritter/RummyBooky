$raw = Get-Content "c:\Dev\RummyBookyMaui\.agents\challenger_rhythm_1\script_output.json" -Raw
$data = $raw | ConvertFrom-Json
$data.Extracted | Where-Object { $_.ValidMod4 -ne $null } | Select-Object File, Property, Value, ParsedNumber, ValidMod4 | Format-Table -AutoSize | Out-String -Width 250
