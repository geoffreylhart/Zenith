rem this process took 91.788055 hours as of 9/28/2019
time /T
echo off
for /l %%x in (0, 1, 15) do (
	for /l %%y in (0, 1, 15) do (
		..\ZenithCrossPlatform\ZenithCrossPlatform\bin\DesktopGL\AnyCPU\Debug\ZenithCrossPlatform.exe LE,X=%%x,Y=%%y,Zoom=4
	)
)
echo on
time /T