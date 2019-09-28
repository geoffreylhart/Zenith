rem this process took 91.788055 hours as of 9/28/2019
for /l %%x in (0, 1, 15) do (
	for /l %%y in (0, 1, 15) do (
		bin\DesktopGL\AnyCPU\Debug\Zenith.exe LE,X=%%x,Y=%%y,Zoom=4
	)
)
for /l %%x in (0, 1, 15) do (
	for /l %%y in (0, 1, 15) do (
		bin\DesktopGL\AnyCPU\Debug\Zenith.exe BO,X=%%x,Y=%%y,Zoom=4
	)
)
for /l %%x in (0, 1, 15) do (
	for /l %%y in (0, 1, 15) do (
		bin\DesktopGL\AnyCPU\Debug\Zenith.exe TO,X=%%x,Y=%%y,Zoom=4
	)
)
for /l %%x in (0, 1, 15) do (
	for /l %%y in (0, 1, 15) do (
		bin\DesktopGL\AnyCPU\Debug\Zenith.exe RI,X=%%x,Y=%%y,Zoom=4
	)
)
for /l %%x in (0, 1, 15) do (
	for /l %%y in (0, 1, 15) do (
		bin\DesktopGL\AnyCPU\Debug\Zenith.exe BA,X=%%x,Y=%%y,Zoom=4
	)
)
for /l %%x in (0, 1, 15) do (
	for /l %%y in (0, 1, 15) do (
		bin\DesktopGL\AnyCPU\Debug\Zenith.exe FR,X=%%x,Y=%%y,Zoom=4
	)
)