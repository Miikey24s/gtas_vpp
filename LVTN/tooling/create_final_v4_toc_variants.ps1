param(
    [string]$SourcePath,
    [string]$StandardOutputPath,
    [string]$CompactOutputPath
)

$ErrorActionPreference = "Stop"

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
if ([string]::IsNullOrWhiteSpace($SourcePath)) {
    $SourcePath = Join-Path $repositoryRoot "LVTN\checkpoints\NguyenAnNam_DH52201078_final_v3.docx"
}
if ([string]::IsNullOrWhiteSpace($StandardOutputPath)) {
    $StandardOutputPath = Join-Path $repositoryRoot "LVTN\checkpoints\NguyenAnNam_DH52201078_final_v4_standard.docx"
}
if ([string]::IsNullOrWhiteSpace($CompactOutputPath)) {
    $CompactOutputPath = Join-Path $repositoryRoot "LVTN\checkpoints\NguyenAnNam_DH52201078_final_v4_compact_toc.docx"
}

$source = (Resolve-Path -LiteralPath $SourcePath).Path
$standard = [System.IO.Path]::GetFullPath($StandardOutputPath)
$compact = [System.IO.Path]::GetFullPath($CompactOutputPath)
Copy-Item -LiteralPath $source -Destination $standard -Force

$replacements = [ordered]@{
    "CHƯƠNG 3. THIẾT KẾ" = "Chương 3. THIẾT KẾ"
    "CHƯƠNG 4. THỬ NGHIỆM" = "Chương 4. THỬ NGHIỆM"
    "CHƯƠNG 5. KẾT LUẬN" = "Chương 5. KẾT LUẬN"
    "Dashboard, Library, quyền và Report" = "Bảng điều khiển, Danh mục, Phân quyền và Báo cáo"
    "chốt kỳ (đóng kỳ)" = "chốt kỳ"
    ". dịch vụ phía máy chủ" = ". Dịch vụ phía máy chủ"
    "Hình 3-38: Thiết kế giao diện quản lý mặt hàng" = "Hình 3-38: Thiết kế giao diện quản lý văn phòng phẩm"
}

function Open-WordDocument([string]$path) {
    $word = New-Object -ComObject Word.Application
    $word.Visible = $false
    $word.DisplayAlerts = 0
    $document = $word.Documents.Open($path, $false, $false)
    return @($word, $document)
}

function Close-WordDocument($word, $document, [bool]$save) {
    if ($null -ne $document) {
        $document.Close($save)
        [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($document)
    }
    if ($null -ne $word) {
        $word.Quit()
        [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($word)
    }
    [GC]::Collect()
    [GC]::WaitForPendingFinalizers()
}

$word = $null
$document = $null
try {
    $opened = Open-WordDocument $standard
    $word = $opened[0]
    $document = $opened[1]

    foreach ($entry in $replacements.GetEnumerator()) {
        $range = $document.Content.Duplicate
        $find = $range.Find
        $find.ClearFormatting()
        $find.Replacement.ClearFormatting()
        $find.Text = $entry.Key
        $find.Replacement.Text = $entry.Value
        $find.Forward = $true
        $find.Wrap = 1
        $find.Format = $false
        $find.MatchCase = $true
        $find.MatchWholeWord = $false
        $find.MatchWildcards = $false
        [void]$find.Execute($entry.Key, $true, $false, $false, $false, $false, $true, 1, $false, $entry.Value, 2)
    }

    for ($index = 1; $index -le $document.TablesOfContents.Count; $index++) {
        [void]$document.TablesOfContents.Item($index).Update()
    }
    for ($index = 1; $index -le $document.TablesOfFigures.Count; $index++) {
        [void]$document.TablesOfFigures.Item($index).Update()
    }

    $updatedFigureCaption = "Hình 3-38: Thiết kế giao diện quản lý văn phòng phẩm"
    for ($paragraphIndex = 1; $paragraphIndex -le $document.Paragraphs.Count; $paragraphIndex++) {
        $paragraph = $document.Paragraphs.Item($paragraphIndex)
        $paragraphText = $paragraph.Range.Text.TrimEnd([char[]]@(13, 7)).Trim()
        if ($paragraphText -ceq $updatedFigureCaption -and $paragraph.Range.Start -gt 20000) {
            $bookmarkRange = $paragraph.Range.Duplicate
            $bookmarkRange.End = $bookmarkRange.End - 1
            [void]$document.Bookmarks.Add("fig_3_38", $bookmarkRange)
            break
        }
    }

    if (-not $document.Bookmarks.Exists("fig_3_38")) {
        throw "Không thể phục hồi bookmark fig_3_38 sau khi cập nhật caption."
    }

    $document.Save()
    $standardPages = $document.ComputeStatistics(2)
    Close-WordDocument $word $document $false
    $word = $null
    $document = $null
}
catch {
    Close-WordDocument $word $document $false
    throw
}

Copy-Item -LiteralPath $standard -Destination $compact -Force

$word = $null
$document = $null
try {
    $opened = Open-WordDocument $compact
    $word = $opened[0]
    $document = $opened[1]

    $compactIndents = @{
        "TOC 1" = 0.0
        "TOC 2" = 1.25
        "TOC 3" = 1.8
    }
    foreach ($entry in $compactIndents.GetEnumerator()) {
        $style = $document.Styles.Item($entry.Key)
        $style.ParagraphFormat.LeftIndent = $word.CentimetersToPoints($entry.Value)
    }

    for ($tocIndex = 1; $tocIndex -le $document.TablesOfContents.Count; $tocIndex++) {
        $toc = $document.TablesOfContents.Item($tocIndex)
        for ($paragraphIndex = 1; $paragraphIndex -le $toc.Range.Paragraphs.Count; $paragraphIndex++) {
            $paragraph = $toc.Range.Paragraphs.Item($paragraphIndex)
            $styleName = [string]$paragraph.Style
            if ($compactIndents.ContainsKey($styleName)) {
                $paragraph.Format.LeftIndent = $word.CentimetersToPoints($compactIndents[$styleName])
            }
        }
    }

    $document.Repaginate()
    $document.Save()
    $compactPages = $document.ComputeStatistics(2)
    Close-WordDocument $word $document $false
    $word = $null
    $document = $null
}
catch {
    Close-WordDocument $word $document $false
    throw
}

Write-Output "Standard: $standard ($standardPages pages)"
Write-Output "Compact TOC: $compact ($compactPages pages)"
Write-Output "Compact TOC indents: level 1 = 0 cm; level 2 = 1.25 cm; level 3 = 1.8 cm"
