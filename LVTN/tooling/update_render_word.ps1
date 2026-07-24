param(
    [Parameter(Mandatory = $true)][string]$DocxPath,
    [Parameter(Mandatory = $true)][string]$PdfPath,
    [switch]$UpdateFields
)

$ErrorActionPreference = 'Stop'
$word = $null
$doc = $null

try {
    $word = New-Object -ComObject Word.Application
    $word.Visible = $false
    $word.DisplayAlerts = 0
    $word.Options.UpdateLinksAtOpen = $false

    $wordProcess = Get-Process WINWORD | Sort-Object StartTime -Descending | Select-Object -First 1
    Write-Output ("WINWORD_PID=" + $wordProcess.Id)

    $doc = $word.Documents.Open($DocxPath, $false, $false)
    if ($UpdateFields) {
        foreach ($toc in $doc.TablesOfContents) { $toc.Update() }
        foreach ($field in $doc.Fields) { [void]$field.Update() }
        foreach ($section in $doc.Sections) {
            foreach ($header in $section.Headers) {
                foreach ($field in $header.Range.Fields) { [void]$field.Update() }
            }
            foreach ($footer in $section.Footers) {
                foreach ($field in $footer.Range.Fields) { [void]$field.Update() }
            }
        }
        $doc.Repaginate()
        $doc.Save()
    }

    if (Test-Path -LiteralPath $PdfPath) {
        Remove-Item -LiteralPath $PdfPath -Force
    }
    $doc.ExportAsFixedFormat($PdfPath, 17)
    Write-Output ("PDF=" + $PdfPath)
}
finally {
    if ($doc -ne $null) {
        try { $doc.Close($false) } catch {}
    }
    if ($word -ne $null) {
        try { $word.Quit() } catch {}
    }
    if ($doc -ne $null) { [void][System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($doc) }
    if ($word -ne $null) { [void][System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($word) }
    [GC]::Collect()
    [GC]::WaitForPendingFinalizers()
}
