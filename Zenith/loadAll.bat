rem this process took 91.788055 hours as of 9/28/2019
time /T
echo off
for /l %%x in (32, 1, 47) do (
	for /l %%y in (0, 1, 15) do (
		..\ZenithCrossPlatform\ZenithCrossPlatform\bin\DesktopGL\AnyCPU\Debug\ZenithCrossPlatform.exe LE,X=%%x,Y=%%y,Zoom=6
	)
)
echo on
time /T