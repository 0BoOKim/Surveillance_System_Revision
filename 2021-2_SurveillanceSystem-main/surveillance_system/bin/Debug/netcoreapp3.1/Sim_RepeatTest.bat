@echo off

setlocal enabledelayedexpansion
set start_time=%time%
set prefix=230503-
set start_idx=1

rem 첫 번째 배열의 길이
set "len1=11"

rem 두 번째 배열의 길이
set "len2=11"

rem 첫 번째 배열 ([0]은 무시함)
set "N_CCTV[0]=0"
set "N_CCTV[1]=10"
set "N_CCTV[2]=50"
set "N_CCTV[3]=100"
set "N_CCTV[4]=150"
set "N_CCTV[5]=200"
set "N_CCTV[6]=250"
set "N_CCTV[7]=300"
set "N_CCTV[8]=350"
set "N_CCTV[9]=400"
set "N_CCTV[10]=450"
set "N_CCTV[11]=500"

rem 두 번째 배열 ([0]은 무시함)
set "N_Ped[0]=0"
set "N_Ped[1]=10"
set "N_Ped[2]=50"
set "N_Ped[3]=100"
set "N_Ped[4]=150"
set "N_Ped[5]=200"
set "N_Ped[6]=250"
set "N_Ped[7]=300"
set "N_Ped[8]=350"
set "N_Ped[9]=400"
set "N_Ped[10]=450"
set "N_Ped[11]=500"



rem 이중 for문
for /l %%i in (1, 1, %len1%) do (
  
    for /l %%j in (1, 1, %len2%) do (
        set /a "temp_suffix=start_idx"
        set "suffix=0!temp_suffix!"
        set "suffix=!suffix:~-2!"
        set "args1=!prefix!!suffix!"
        echo !args1!	      
        echo N_CCTV[%%i]=!N_CCTV[%%i]!, N_Ped[%%j]=!N_Ped[%%j]!
        surveillance_system.exe !args1! !N_CCTV[%%i]! !N_Ped[%%j]!	
        set /a "start_idx=start_idx+1"	
    )
)
set end_time=%time%
set /a elapsed_time=(%end_time:~0,2% - %start_time:~0,2%)*3600 + (%end_time:~3,2% - %start_time:~3,2%)*60 + (%end_time:~6,2% - %start_time:~6,2%)
echo Elapsed time: %elapsed_time% seconds.
set soundfile=.\complete.wav
rem powershell 명령어 실행
powershell -Command "(New-Object Media.SoundPlayer '%soundfile%').PlaySync();"