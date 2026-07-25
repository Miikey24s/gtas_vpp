Option Explicit

If WScript.Arguments.Count <> 2 Then
    WScript.Echo "Usage: cscript //nologo move_usecase_figures_before_tables.vbs document.docx backup.docx"
    WScript.Quit 2
End If

Dim targetPath, backupPath, normalizedTarget
Dim word, document, candidate, createdWord, openedDocument
Dim fso, index

targetPath = WScript.Arguments.Item(0)
backupPath = WScript.Arguments.Item(1)
normalizedTarget = LCase(Replace(targetPath, "/", "\"))
createdWord = False
openedDocument = False

On Error Resume Next
Set word = GetObject(, "Word.Application")
If Err.Number <> 0 Then
    Err.Clear
    Set word = CreateObject("Word.Application")
    If Err.Number <> 0 Then
        WScript.Echo "ERROR creating Word: " & Err.Description
        WScript.Quit 3
    End If
    createdWord = True
    word.Visible = False
    word.DisplayAlerts = 0
End If

Set document = Nothing
For index = 1 To word.Documents.Count
    Set candidate = word.Documents.Item(index)
    If LCase(Replace(candidate.FullName, "/", "\")) = normalizedTarget Then
        Set document = candidate
        Exit For
    End If
Next

If document Is Nothing Then
    Err.Clear
    Set document = word.Documents.Open(targetPath, False, False)
    If Err.Number <> 0 Then
        WScript.Echo "ERROR opening DOCX: " & Err.Description
        If createdWord Then word.Quit
        WScript.Quit 4
    End If
    openedDocument = True
End If

' Persist any edits already made by the user before taking the backup.
document.Save
Set fso = CreateObject("Scripting.FileSystemObject")
fso.CopyFile targetPath, backupPath, True

Dim pairs
pairs = Array( _
    Array("3-5:", "3-7:"), _
    Array("3-6:", "3-8:"), _
    Array("3-7:", "3-9:"), _
    Array("3-8:", "3-10:"), _
    Array("3-9:", "3-11:"), _
    Array("3-10:", "3-12:"), _
    Array("3-11:", "3-13:"), _
    Array("3-12:", "3-14:"), _
    Array("3-14:", "3-15:"), _
    Array("3-15:", "3-16:") _
)

Dim pair, movedCount
movedCount = 0
For Each pair In pairs
    If MoveFigureBeforeTable(document, pair(0), pair(1)) Then
        movedCount = movedCount + 1
        WScript.Echo "MOVED=" & pair(1) & " -> " & pair(0)
    Else
        WScript.Echo "ERROR moving pair: " & pair(1) & " / " & pair(0)
        If openedDocument Then document.Close False
        If createdWord Then word.Quit
        WScript.Quit 6
    End If
Next

If movedCount <> 10 Then
    WScript.Echo "ERROR expected 10 moves, completed " & movedCount
    If openedDocument Then document.Close False
    If createdWord Then word.Quit
    WScript.Quit 7
End If

' Keep the existing TOC limited to heading levels 1-3 and refresh all links.
Dim toc
For Each toc In document.TablesOfContents
    toc.UseHeadingStyles = True
    toc.UpperHeadingLevel = 1
    toc.LowerHeadingLevel = 3
    toc.Update
Next
document.Fields.Update
document.Save

WScript.Echo "MOVED_COUNT=" & movedCount
WScript.Echo "BACKUP=" & backupPath
WScript.Echo "DOCX=" & document.FullName

If openedDocument Then document.Close False
If createdWord Then word.Quit

Set toc = Nothing
Set fso = Nothing
Set candidate = Nothing
Set document = Nothing
Set word = Nothing
WScript.Quit 0


Function FindLastText(doc, searchText)
    Dim searchRange
    Set searchRange = doc.Content.Duplicate
    With searchRange.Find
        .ClearFormatting
        .Text = searchText
        .Forward = False
        .Wrap = 0
    End With
    If searchRange.Find.Execute Then
        Set FindLastText = searchRange
    Else
        Set FindLastText = Nothing
    End If
End Function


Function MoveFigureBeforeTable(doc, tableCaption, figureCaption)
    Dim tableRange, figureRange, captionParagraph, drawingParagraph
    Dim previousParagraph, attempt, pairRange, insertionRange

    Set tableRange = FindLastText(doc, tableCaption)
    Set figureRange = FindLastText(doc, figureCaption)
    If tableRange Is Nothing Or figureRange Is Nothing Then
        MoveFigureBeforeTable = False
        Exit Function
    End If

    Set captionParagraph = figureRange.Paragraphs.Item(1)
    Set previousParagraph = captionParagraph.Previous(1)
    Set drawingParagraph = Nothing

    For attempt = 1 To 4
        If previousParagraph Is Nothing Then Exit For
        If previousParagraph.Range.InlineShapes.Count > 0 Then
            Set drawingParagraph = previousParagraph
            Exit For
        End If
        Set previousParagraph = previousParagraph.Previous(1)
    Next

    If drawingParagraph Is Nothing Then
        MoveFigureBeforeTable = False
        Exit Function
    End If

    Set pairRange = doc.Range(drawingParagraph.Range.Start, captionParagraph.Range.End)
    Set insertionRange = doc.Range(tableRange.Paragraphs.Item(1).Range.Start, tableRange.Paragraphs.Item(1).Range.Start)

    pairRange.Cut
    insertionRange.Paste
    MoveFigureBeforeTable = True
End Function
