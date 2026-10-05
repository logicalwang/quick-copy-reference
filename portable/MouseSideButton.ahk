#Requires AutoHotkey v2.0
#SingleInstance Force
; Optional: run this only if your mouse cannot map a side button in its own software.
; XButton2 is normally the forward side button. Change it to XButton1 for back.
XButton2::Run('"' A_ScriptDir '\QuickCopyReference.exe" --copy', , 'Hide')
