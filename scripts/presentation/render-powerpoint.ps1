param(
    [Parameter(Mandatory = $true)]
    [string]$InputPptx,
    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

$resolvedInput = (Resolve-Path -LiteralPath $InputPptx).Path
$resolvedOutput = [System.IO.Path]::GetFullPath($OutputDirectory)
[System.IO.Directory]::CreateDirectory($resolvedOutput) | Out-Null

$powerPoint = $null
$presentation = $null
try {
    $powerPoint = New-Object -ComObject PowerPoint.Application
    $presentation = $powerPoint.Presentations.Open($resolvedInput, $true, $false, $false)
    for ($index = 1; $index -le $presentation.Slides.Count; $index++) {
        $outputPath = Join-Path $resolvedOutput ("slide-{0:D2}.png" -f $index)
        $presentation.Slides.Item($index).Export($outputPath, "PNG", 1920, 1080)
    }
    Write-Output ("slides={0};output={1}" -f $presentation.Slides.Count, $resolvedOutput)
}
finally {
    if ($presentation -ne $null) {
        $presentation.Close()
        [void][System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($presentation)
    }
    if ($powerPoint -ne $null) {
        $powerPoint.Quit()
        [void][System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($powerPoint)
    }
    [GC]::Collect()
    [GC]::WaitForPendingFinalizers()
}
