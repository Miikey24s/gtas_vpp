param(
    [string]$DocumentPath
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($DocumentPath)) {
    $repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..\..")).Path
    $DocumentPath = Join-Path $repositoryRoot "LVTN\checkpoints\NguyenAnNam_DH52201078_final_v3.docx"
}

$resolvedPath = (Resolve-Path -LiteralPath $DocumentPath).Path
$backupPath = Join-Path $env:TEMP ("gtas-final-v3-pre-portrait-" + [Guid]::NewGuid().ToString("N") + ".docx")
Copy-Item -LiteralPath $resolvedPath -Destination $backupPath

$word = $null
$document = $null
$success = $false

try {
    $word = New-Object -ComObject Word.Application
    $word.Visible = $false
    $word.DisplayAlerts = 0
    $document = $word.Documents.Open($resolvedPath, $false, $false)

    $changedSections = 0
    $resizedImages = 0
    $maximumImageWidth = $word.CentimetersToPoints(15.8)
    for ($sectionIndex = 1; $sectionIndex -le $document.Sections.Count; $sectionIndex++) {
        $section = $document.Sections.Item($sectionIndex)
        if ($section.PageSetup.Orientation -ne 1) {
            continue
        }

        for ($shapeIndex = 1; $shapeIndex -le $section.Range.InlineShapes.Count; $shapeIndex++) {
            $shape = $section.Range.InlineShapes.Item($shapeIndex)
            if ($shape.Width -gt $maximumImageWidth) {
                $shape.LockAspectRatio = -1
                $shape.Width = $maximumImageWidth
                $resizedImages++
            }
        }

        $section.PageSetup.Orientation = 0
        $section.PageSetup.PageWidth = $word.CentimetersToPoints(21.0)
        $section.PageSetup.PageHeight = $word.CentimetersToPoints(29.7)
        $section.PageSetup.LeftMargin = $word.CentimetersToPoints(3.0)
        $section.PageSetup.TopMargin = $word.CentimetersToPoints(2.0)
        $section.PageSetup.RightMargin = $word.CentimetersToPoints(2.0)
        $section.PageSetup.BottomMargin = $word.CentimetersToPoints(2.0)
        $changedSections++
    }

    if ($changedSections -eq 0) {
        throw "Không tìm thấy section ngang thuộc mục 3.3."
    }

    $document.Save()
    $pages = $document.ComputeStatistics(2)
    $success = $true
    Write-Output "Updated: $resolvedPath"
    Write-Output "Portrait sections: $changedSections"
    Write-Output "Resized images: $resizedImages"
    Write-Output "Heading 4 formatting: untouched"
    Write-Output "Pages: $pages"
}
catch {
    if ($null -ne $document) {
        $document.Close($false)
        [void][System.Runtime.InteropServices.Marshal]::ReleaseComObject($document)
        $document = $null
    }
    Copy-Item -LiteralPath $backupPath -Destination $resolvedPath -Force
    throw
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
    if ($success -and (Test-Path -LiteralPath $backupPath)) {
        Remove-Item -LiteralPath $backupPath -Force
    }
    [GC]::Collect()
    [GC]::WaitForPendingFinalizers()
}
