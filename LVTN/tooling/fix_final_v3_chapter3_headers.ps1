param([Parameter(Mandatory = $true)][string]$DocxPath)

$ErrorActionPreference = 'Stop'
$word = $null
$document = $null

function Trim-WordText {
    param([string]$Text)
    return $Text.Trim([char]7, [char]13, [char]10, [char]32, [char]9)
}

function Find-HeadingRange {
    param([string]$Text)
    foreach ($paragraph in $document.Paragraphs) {
        if ((Trim-WordText $paragraph.Range.Text) -eq $Text) {
            return $paragraph.Range.Duplicate
        }
    }
    throw "Không tìm thấy tiêu đề: $Text"
}

try {
    $resolvedPath = (Resolve-Path -LiteralPath $DocxPath).Path
    $word = New-Object -ComObject Word.Application
    $word.Visible = $false
    $word.DisplayAlerts = 0
    $word.Options.UpdateLinksAtOpen = $false
    $document = $word.Documents.Open($resolvedPath, $false, $false)

    $chapter3 = Find-HeadingRange -Text 'CHƯƠNG 3. THIẾT KẾ'
    $chapter4 = Find-HeadingRange -Text 'CHƯƠNG 4. THỬ NGHIỆM'
    $updated = 0

    foreach ($section in $document.Sections) {
        if ($section.Range.End -le $chapter3.Start -or $section.Range.Start -ge $chapter4.Start) {
            continue
        }
        $header = $section.Headers.Item(1)
        $header.LinkToPrevious = $false
        $header.Range.Text = 'Chương 3. THIẾT KẾ'
        $header.Range.Font.Name = 'Times New Roman'
        $header.Range.Font.Size = 13
        $header.Range.Font.Italic = $true
        $header.Range.Font.Underline = 0
        $updated++
    }

    $document.Repaginate()
    $document.Save()
    Write-Output ("CHAPTER3_HEADERS_UPDATED=" + $updated)
}
finally {
    if ($document -ne $null) { try { $document.Close($false) } catch {} }
    if ($word -ne $null) { try { $word.Quit() } catch {} }
    if ($document -ne $null) { [void][System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($document) }
    if ($word -ne $null) { [void][System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($word) }
    [GC]::Collect()
    [GC]::WaitForPendingFinalizers()
}
