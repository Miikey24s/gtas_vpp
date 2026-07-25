param(
    [string]$DocumentPath
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($DocumentPath)) {
    $repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
    $DocumentPath = Join-Path $repositoryRoot "LVTN\checkpoints\NguyenAnNam_DH52201078_final_v3.docx"
}

$resolvedPath = (Resolve-Path -LiteralPath $DocumentPath).Path
$directory = Split-Path -Parent $resolvedPath
$baseName = [System.IO.Path]::GetFileNameWithoutExtension($resolvedPath)
$backupPath = Join-Path $directory ($baseName + ".pre-academic-tone-20260725.docx")

if (-not (Test-Path -LiteralPath $backupPath)) {
    Copy-Item -LiteralPath $resolvedPath -Destination $backupPath
}

$word = $null
$document = $null

function Replace-ExactText {
    param(
        [Parameter(Mandatory)] $Document,
        [Parameter(Mandatory)] [string] $OldText,
        [Parameter(Mandatory)] [string] $NewText
    )

    $range = $Document.Content.Duplicate
    $find = $range.Find
    $find.ClearFormatting()
    $find.Replacement.ClearFormatting()
    $find.Text = $OldText
    $find.Replacement.Text = $NewText
    $find.Forward = $true
    $find.Wrap = 0
    $find.Format = $false
    $find.MatchCase = $true
    $find.MatchWholeWord = $false
    $find.MatchWildcards = $false

    if (-not $find.Execute()) {
        throw "Không tìm thấy nội dung cần thay: $OldText"
    }

    $range.Text = $NewText
}

function Replace-ExactParagraph {
    param(
        [Parameter(Mandatory)] $Document,
        [Parameter(Mandatory)] [string] $OldText,
        [Parameter(Mandatory)] [string] $NewText
    )

    foreach ($paragraph in @($Document.Paragraphs)) {
        $paragraphText = $paragraph.Range.Text.TrimEnd([char[]]@(13, 7))
        if ($paragraphText -ceq $OldText) {
            $textRange = $paragraph.Range.Duplicate
            if ($textRange.End -gt $textRange.Start) {
                $textRange.MoveEnd(1, -1) | Out-Null
            }
            $textRange.Text = $NewText
            return
        }
    }

    throw "Không tìm thấy đoạn cần thay: $OldText"
}

try {
    $word = New-Object -ComObject Word.Application
    $word.Visible = $false
    $word.DisplayAlerts = 0
    $document = $word.Documents.Open($resolvedPath, $false, $false)

    Replace-ExactParagraph -Document $document `
        -OldText "Phần này trình bày các màn hình được chuẩn hóa bằng Atlas. Nội dung nghiệp vụ kế thừa bản luận văn hiện hành và được đối chiếu với cơ chế xử lý phía máy chủ; giao diện cũ chỉ được dùng để nhận diện các chức năng đã có. Các hình sau là thiết kế giao diện phục vụ mô tả hệ thống, không được dùng làm bằng chứng kiểm thử ở Chương 4." `
        -NewText "Phần này trình bày các màn hình chính của hệ thống, được xây dựng dựa trên yêu cầu nghiệp vụ, phạm vi quyền và các quy trình đã phân tích."

    Replace-ExactText -Document $document `
        -OldText "để tránh làm bảng quá rộng." `
        -NewText "nhằm giúp người dùng dễ theo dõi và tra cứu."

    Replace-ExactText -Document $document `
        -OldText "trên cùng không gian làm việc." `
        -NewText "trong cùng màn hình."

    $obsoleteText = "Hoàn thiện giao diện: Bổ sung thêm ảnh minh họa cho báo cáo, hiệu chỉnh kết quả chốt kỳ và hộp thư thông báo khi có bộ ảnh chính thức hơn."
    $obsoleteDeleted = $false
    foreach ($paragraph in @($document.Paragraphs)) {
        $paragraphText = $paragraph.Range.Text.TrimEnd([char[]]@(13, 7))
        if ($paragraphText -ceq $obsoleteText) {
            $paragraph.Range.Delete()
            $obsoleteDeleted = $true
            break
        }
    }
    if (-not $obsoleteDeleted) {
        throw "Không tìm thấy đoạn ghi chú ảnh minh họa cần xóa."
    }

    foreach ($field in @($document.Fields)) {
        try { [void]$field.Update() } catch { }
    }
    foreach ($tableOfContents in @($document.TablesOfContents)) {
        [void]$tableOfContents.Update()
    }

    $document.Save()
    $pages = $document.ComputeStatistics(2)
    Write-Output "Updated: $resolvedPath"
    Write-Output "Backup:  $backupPath"
    Write-Output "Pages:   $pages"
}
finally {
    if ($null -ne $document) {
        $document.Close($false)
        [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($document)
    }
    if ($null -ne $word) {
        $word.Quit()
        [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($word)
    }
    [GC]::Collect()
    [GC]::WaitForPendingFinalizers()
}
