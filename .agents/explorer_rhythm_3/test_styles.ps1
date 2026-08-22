$lines = Get-Content -Path 'c:\Dev\RummyBookyMaui\RummyBooky\Resources\Styles\Styles.xaml'
for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    if ($line -match 'Property="Padding"') {
        Write-Host "Line ($($i+1)): $line"
    }
}
