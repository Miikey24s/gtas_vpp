param([Parameter(Mandatory = $true)][string]$DocxPath)

$ErrorActionPreference = 'Stop'
$word = $null
$doc = $null

function Update-StoryFields {
    param($StoryRange)

    $current = $StoryRange
    while ($null -ne $current) {
        foreach ($field in $current.Fields) {
            [void]$field.Update()
        }
        $current = $current.NextStoryRange
    }
}

try {
    $resolvedPath = (Resolve-Path -LiteralPath $DocxPath).Path
    $word = New-Object -ComObject Word.Application
    $word.Visible = $false
    $word.DisplayAlerts = 0
    $word.Options.UpdateLinksAtOpen = $false

    $doc = $word.Documents.Open($resolvedPath, $false, $false)
    $revisionCount = $doc.Revisions.Count
    $commentCount = $doc.Comments.Count

    if ($revisionCount -gt 0) {
        $doc.AcceptAllRevisions()
    }
    while ($doc.Comments.Count -gt 0) {
        $doc.Comments.Item(1).Delete()
    }

    foreach ($toc in $doc.TablesOfContents) {
        [void]$toc.Update()
    }
    foreach ($tof in $doc.TablesOfFigures) {
        [void]$tof.Update()
    }
    foreach ($storyRange in $doc.StoryRanges) {
        Update-StoryFields -StoryRange $storyRange
    }

    $doc.Repaginate()
    $pageCount = $doc.ComputeStatistics(2)
    $fieldCount = $doc.Fields.Count
    $tocCount = $doc.TablesOfContents.Count
    $tofCount = $doc.TablesOfFigures.Count
    $doc.Save()

    Write-Output ("PAGES=" + $pageCount)
    Write-Output ("FIELDS=" + $fieldCount)
    Write-Output ("TOC_COUNT=" + $tocCount)
    Write-Output ("TOF_COUNT=" + $tofCount)
    Write-Output ("REVISIONS_ACCEPTED=" + $revisionCount)
    Write-Output ("COMMENTS_DELETED=" + $commentCount)
}
finally {
    if ($doc -ne $null) { try { $doc.Close($false) } catch {} }
    if ($word -ne $null) { try { $word.Quit() } catch {} }
    if ($doc -ne $null) { [void][System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($doc) }
    if ($word -ne $null) { [void][System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($word) }
    [GC]::Collect()
    [GC]::WaitForPendingFinalizers()
}
