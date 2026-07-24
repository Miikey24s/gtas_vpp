param([Parameter(Mandatory = $true)][string]$DocxPath)

$ErrorActionPreference = 'Stop'
$word = $null
$doc = $null
try {
    $word = New-Object -ComObject Word.Application
    $word.Visible = $false
    $word.DisplayAlerts = 0
    $doc = $word.Documents.Open($DocxPath, $false, $false)
    foreach ($toc in $doc.TablesOfContents) {
        $toc.Update()
    }
    $doc.Repaginate()
    $doc.Save()
    Write-Output ("TOC_COUNT=" + $doc.TablesOfContents.Count)
}
finally {
    if ($doc -ne $null) { try { $doc.Close($false) } catch {} }
    if ($word -ne $null) { try { $word.Quit() } catch {} }
    if ($doc -ne $null) { [void][System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($doc) }
    if ($word -ne $null) { [void][System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($word) }
    [GC]::Collect()
    [GC]::WaitForPendingFinalizers()
}
