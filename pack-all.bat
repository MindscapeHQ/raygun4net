set EnableNuGetPackageRestore=true
call .\packages\psake.4.9.0\tools\psake\psake.cmd pack-all.ps1 %*
pause