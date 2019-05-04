@setlocal enableextensions
@cd /d "%~dp0"
cd ConsoleApp2\bin\Debug
 
start MockRepo.exe
cd ../../..
cd MotherBuilder\bin\Debug
 
start MotherBuilder.exe
cd ../../..
cd MockTestHarness\bin\Debug
 
start MockTestHarness.exe
cd ../../..
cd ClientGUI\bin\Debug
 
start ClientGUI.exe