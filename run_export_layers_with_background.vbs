Option Explicit

Dim fso, folder, scriptPath, ps, result
Set fso = CreateObject("Scripting.FileSystemObject")

folder = fso.GetParentFolderName(WScript.ScriptFullName)
scriptPath = fso.BuildPath(folder, "ps_export_layers_with_background.jsx")

If Not fso.FileExists(scriptPath) Then
    MsgBox "Missing script file:" & vbCrLf & scriptPath, vbCritical, "Photoshop Export"
    WScript.Quit 1
End If

On Error Resume Next
Set ps = CreateObject("Photoshop.Application")
If Err.Number <> 0 Then
    MsgBox "Cannot connect to Photoshop. Please open Photoshop first." & vbCrLf & Err.Description, vbCritical, "Photoshop Export"
    WScript.Quit 1
End If
On Error GoTo 0

On Error Resume Next
result = ps.DoJavaScriptFile(scriptPath)
If Err.Number <> 0 Then
    MsgBox "Photoshop is busy or the script failed." & vbCrLf & _
           "Close any Photoshop dialog boxes and run this file again." & vbCrLf & vbCrLf & _
           Err.Description, vbExclamation, "Photoshop Export"
    WScript.Quit 1
End If
On Error GoTo 0

MsgBox CStr(result), vbInformation, "Photoshop Export"
