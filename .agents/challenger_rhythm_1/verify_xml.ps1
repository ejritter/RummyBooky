# verify_xml.ps1
$ErrorActionPreference = 'Stop'

$rootDir = "c:\Dev\RummyBookyMaui"
$xamlFiles = Get-ChildItem -Path $rootDir -Recurse -Filter "*.xaml" | Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' }

$allExtracted = [System.Collections.Generic.List[PSCustomObject]]::new()
$violations = [System.Collections.Generic.List[PSCustomObject]]::new()

$targetProps = @('Padding', 'Margin', 'RowSpacing', 'ColumnSpacing', 'Spacing')

foreach ($file in $xamlFiles) {
    $relativePath = $file.FullName.Replace($rootDir, "").TrimStart('\')
    $rawText = Get-Content -Path $file.FullName -Raw
    
    # Use XmlReader with settings that ignore DTD / schema verification
    $settings = New-Object System.Xml.XmlReaderSettings
    $settings.ConformanceLevel = [System.Xml.ConformanceLevel]::Fragment
    $settings.IgnoreComments = $true
    $settings.IgnoreWhitespace = $true
    
    $stringReader = New-Object System.IO.StringReader($rawText)
    $reader = [System.Xml.XmlReader]::Create($stringReader, $settings)
    
    try {
        while ($reader.Read()) {
            if ($reader.NodeType -eq [System.Xml.XmlNodeType]::Element) {
                $elName = $reader.Name
                # Read attributes
                if ($reader.HasAttributes) {
                    while ($reader.MoveToNextAttribute()) {
                        $localName = $reader.LocalName
                        $val = $reader.Value
                        
                        if ($targetProps -contains $localName) {
                            $tokens = $val -split '[\s,]+' | Where-Object { $_ -ne "" }
                            foreach ($token in $tokens) {
                                $num = 0.0
                                if ([double]::TryParse($token, [ref]$num)) {
                                    $isMod4 = ($num % 4 -eq 0)
                                    $allExtracted.Add([PSCustomObject]@{
                                        File = $relativePath
                                        Element = $elName
                                        Property = $localName
                                        Value = $val
                                        ParsedNumber = $num
                                        ValidMod4 = $isMod4
                                    })
                                    if (-not $isMod4) {
                                        $violations.Add([PSCustomObject]@{
                                            File = $relativePath
                                            Element = $elName
                                            Property = $localName
                                            Value = $val
                                            ParsedNumber = $num
                                        })
                                    }
                                }
                            }
                        }
                        
                        # Setter element check
                        if ($elName -eq "Setter" -and $localName -eq "Property" -and ($targetProps -contains $val)) {
                            $propName = $val
                            if ($reader.MoveToAttribute("Value")) {
                                $setterVal = $reader.Value
                                $tokens = $setterVal -split '[\s,]+' | Where-Object { $_ -ne "" }
                                foreach ($token in $tokens) {
                                    $num = 0.0
                                    if ([double]::TryParse($token, [ref]$num)) {
                                        $isMod4 = ($num % 4 -eq 0)
                                        $allExtracted.Add([PSCustomObject]@{
                                            File = $relativePath
                                            Element = "Setter"
                                            Property = "Setter:$propName"
                                            Value = $setterVal
                                            ParsedNumber = $num
                                            ValidMod4 = $isMod4
                                        })
                                        if (-not $isMod4) {
                                            $violations.Add([PSCustomObject]@{
                                                File = $relativePath
                                                Element = "Setter"
                                                Property = "Setter:$propName"
                                                Value = $setterVal
                                                ParsedNumber = $num
                                            })
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    } finally {
        $reader.Close()
        $stringReader.Close()
    }
}

Write-Host "=== XML READER PARSER RESULTS ==="
Write-Host "Total Files Processed: $($xamlFiles.Count)"
Write-Host "Total Parsed Spacing Numbers: $($allExtracted.Count)"
Write-Host "Total Violations: $($violations.Count)"
if ($violations.Count -gt 0) {
    Write-Host "`nVIOLATIONS FOUND:" -ForegroundColor Red
    $violations | Format-Table -AutoSize | Out-String | Write-Host
} else {
    Write-Host "`nXML Reader: 100% of parsed spacing numbers satisfy val % 4 == 0!" -ForegroundColor Green
}
