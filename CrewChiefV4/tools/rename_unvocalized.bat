@echo off
REM renames sequentially named .wav files (1.wav, 2.wav, ...) based on the new-line separated name list in unvocalized_driver_names.txt.
REM This batch file expects the recordings to be in a subfolder called temp (relative to the location of this batch file)

REM chcp 65001 changes the output to UTF-8 so filenames don't get mangled
chcp 65001

set /a COUNTER=0
setlocal ENABLEDELAYEDEXPANSION
for /F "tokens=*" %%A in  ( unvocalized_driver_names.txt) do  (
   set /a COUNTER=COUNTER+1
   ECHO Renaming temp\!COUNTER!.wav to temp\%%A.wav
   move temp\!COUNTER!.wav "temp\%%A.wav"
)
@echo on
