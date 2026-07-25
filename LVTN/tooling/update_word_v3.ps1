param(
    [Parameter(Mandatory = $true)][string]$DocxPath,
    [Parameter(Mandatory = $true)][string]$PdfPath
)

$ErrorActionPreference = 'Stop'
$word = $null
$doc = $null

function Set-HeaderText {
    param($Section, [string]$Text)

    $Section.Headers.Item(1).LinkToPrevious = $false
    $Section.Headers.Item(1).Range.Text = $Text
    $Section.Headers.Item(1).Range.Font.Name = 'Times New Roman'
    $Section.Headers.Item(1).Range.Font.Size = 13
    $Section.Headers.Item(1).Range.Font.Italic = $true
    $Section.Headers.Item(1).Range.Font.Underline = 1
    $Section.PageSetup.DifferentFirstPageHeaderFooter = $true
}

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

function Trim-WordText {
    param([string]$Text)

    return $Text.Trim([char]7, [char]13, [char]10, [char]32, [char]9)
}

function Find-HeadingRange {
    param([string]$Text)

    foreach ($paragraph in $doc.Paragraphs) {
        $paragraphText = Trim-WordText $paragraph.Range.Text
        if ($paragraphText -eq $Text) {
            return $paragraph.Range.Duplicate
        }
    }
    throw "Could not find body heading: $Text"
}

function Find-SectionForRange {
    param($Range)

    foreach ($section in $doc.Sections) {
        if ($Range.Start -ge $section.Range.Start -and $Range.Start -lt $section.Range.End) {
            return $section
        }
    }
    throw "Could not locate section for range."
}

function Utf8Text {
    param([string]$Base64)

    return [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($Base64))
}

try {
    $resolvedDocx = (Resolve-Path -LiteralPath $DocxPath).Path
    $resolvedPdf = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($PdfPath)
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $resolvedPdf) | Out-Null

    $word = New-Object -ComObject Word.Application
    $word.Visible = $false
    $word.DisplayAlerts = 0
    $word.Options.UpdateLinksAtOpen = $false

    $doc = $word.Documents.Open($resolvedDocx, $false, $false)

    if ($doc.Revisions.Count -gt 0) {
        $doc.AcceptAllRevisions()
    }
    while ($doc.Comments.Count -gt 0) {
        $doc.Comments.Item(1).Delete()
    }

    $chapter1Title = Utf8Text 'Q2jGsMahbmcgMS4gR0nhu5pJIFRISeG7hlU='
    $chapter2Title = Utf8Text 'Q2jGsMahbmcgMi4gUEjGr8agTkcgUEjDgVAgVEjhu7BDIEhJ4buGTg=='
    $chapter3Title = Utf8Text 'Q0jGr8agTkcgMy4gVEhJ4bq+VCBL4bq+'
    $chapter4Title = Utf8Text 'Q0jGr8agTkcgNC4gVEjhu6wgTkdISeG7hk0='
    $chapter5Title = Utf8Text 'Q0jGr8agTkcgNS4gS+G6vlQgTFXhuqxO'
    $chapter3Header = Utf8Text 'Q2jGsMahbmcgMy4gVEhJ4bq+VCBL4bq+'
    $chapter4Header = Utf8Text 'Q2jGsMahbmcgNC4gVEjhu6wgTkdISeG7hk0='
    $chapter5Header = Utf8Text 'Q2jGsMahbmcgNS4gS+G6vlQgTFXhuqxO'
    $appendixTitle = Utf8Text 'UEjhu6QgTOG7pEM='
    $referencesTitle = Utf8Text 'VMOASSBMSeG7hlUgVEhBTSBLSOG6ok8='

    $chapter2Range = Find-HeadingRange -Text $chapter2Title
    $chapter2Section = Find-SectionForRange -Range $chapter2Range
    $chapter2Header = Trim-WordText $chapter2Section.Headers.Item(1).Range.Text
    if ($chapter2Header -ne $chapter2Title) {
        $insert = $chapter2Range.Duplicate
        $insert.Collapse(1)
        $insert.InsertBreak(3)
        $chapter2Range = Find-HeadingRange -Text $chapter2Title
        $chapter2Section = Find-SectionForRange -Range $chapter2Range
    }
    Set-HeaderText -Section $chapter2Section -Text $chapter2Title

    $chapterHeaders = @{
        $chapter1Title = $chapter1Title
        $chapter3Title = $chapter3Header
        $chapter4Title = $chapter4Header
        $chapter5Title = $chapter5Header
        $appendixTitle = $appendixTitle
        $referencesTitle = $referencesTitle
    }
    foreach ($key in $chapterHeaders.Keys) {
        $range = Find-HeadingRange -Text $key
        $section = Find-SectionForRange -Range $range
        $currentHeader = Trim-WordText $section.Headers.Item(1).Range.Text
        if ($currentHeader -ne $chapterHeaders[$key]) {
            $insert = $range.Duplicate
            $insert.Collapse(1)
            if ($key -eq $referencesTitle) {
                $insert.InsertBreak(2)
            }
            else {
                $insert.InsertBreak(3)
            }
            $range = Find-HeadingRange -Text $key
            $section = Find-SectionForRange -Range $range
        }
        Set-HeaderText -Section $section -Text $chapterHeaders[$key]
    }

    foreach ($toc in $doc.TablesOfContents) {
        $toc.LowerHeadingLevel = 4
        [void]$toc.Update()
    }
    foreach ($tof in $doc.TablesOfFigures) {
        [void]$tof.Update()
    }
    foreach ($storyRange in $doc.StoryRanges) {
        Update-StoryFields -StoryRange $storyRange
    }
    foreach ($toc in $doc.TablesOfContents) {
        [void]$toc.UpdatePageNumbers()
    }
    foreach ($tof in $doc.TablesOfFigures) {
        [void]$tof.UpdatePageNumbers()
    }

    $doc.Repaginate()
    $doc.Save()
    $doc.ExportAsFixedFormat($resolvedPdf, 17)

    Write-Output ("DOCX=" + $resolvedDocx)
    Write-Output ("PDF=" + $resolvedPdf)
    Write-Output ("PAGES=" + $doc.ComputeStatistics(2))
    Write-Output ("SECTIONS=" + $doc.Sections.Count)
    Write-Output ("TOC_COUNT=" + $doc.TablesOfContents.Count)
    Write-Output ("TOF_COUNT=" + $doc.TablesOfFigures.Count)
}
finally {
    if ($doc -ne $null) { try { $doc.Close($false) } catch {} }
    if ($word -ne $null) { try { $word.Quit() } catch {} }
    if ($doc -ne $null) { [void][System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($doc) }
    if ($word -ne $null) { [void][System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($word) }
    [GC]::Collect()
    [GC]::WaitForPendingFinalizers()
}
