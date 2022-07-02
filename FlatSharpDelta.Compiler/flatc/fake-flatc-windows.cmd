@echo off
set o=none

:initial
if "%1"=="" goto done
set aux=%1
if "%aux:~0,1%"=="-" (
   set nome=%aux:~1,250%
) else (
   set "%nome%=%1"
   set nome=
)
shift
goto initial
:done

for %%a in (%*) do set last=%%a

copy %last% %o%