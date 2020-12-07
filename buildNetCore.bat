.\.nuget\NuGet.exe install .nuget\packages.config -o packages
.\packages\psake.4.9.0\tools\psake\psake.cmd buildNetCore.ps1 %*

pause