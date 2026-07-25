Option Explicit

If WScript.Arguments.Count <> 1 Then
    WScript.Echo "Usage: cscript //nologo set_toc_to_level3.vbs full-document-path"
    WScript.Quit 2
End If

Dim targetPath, normalizedTarget, word, document, candidate
Dim createdWord, openedDocument, index, toc

targetPath = WScript.Arguments.Item(0)
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

If document.TablesOfContents.Count = 0 Then
    WScript.Echo "ERROR: document has no table of contents"
    If openedDocument Then document.Close False
    If createdWord Then word.Quit
    WScript.Quit 5
End If

For Each toc In document.TablesOfContents
    toc.UseHeadingStyles = True
    toc.UpperHeadingLevel = 1
    toc.LowerHeadingLevel = 3
    toc.Update
Next

document.Save
WScript.Echo "TOC_COUNT=" & document.TablesOfContents.Count
WScript.Echo "TOC_LEVELS=1-3"
WScript.Echo "DOCX=" & document.FullName

If openedDocument Then document.Close False
If createdWord Then word.Quit

Set toc = Nothing
Set candidate = Nothing
Set document = Nothing
Set word = Nothing
WScript.Quit 0
