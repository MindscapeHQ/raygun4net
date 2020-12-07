set EnableNuGetPackageRestore=true
call .\packages\psake.4.9.0\tools\psake\psake.cmd buildWebApi.ps1 %*

pause