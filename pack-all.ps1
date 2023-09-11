# This causes psake to use the VS 2019 build tool:
Framework "4.8"

properties {
    $root = $psake.build_script_dir
    $build_dir = "$root\build"
    $nuget_dir = "$root\.nuget"
    $nuspec_folder = "$root\nuspecs"
    $version = "7.0.0"
    $include_signed = $true
}

task default -depends Pack

task Clean {
    remove-item -force -recurse $build_dir -ErrorAction SilentlyContinue | Out-Null
}

task Build -depends Clean {
    & dotnet build $root\Raygun.CrashReporting.sln -c Release
}

task Pack -depends Build {

    # Pack .NET SDK Projects (these use the project files and not nuspecs)
    # Because of old style projects we need to pack the specific projects directly
    & dotnet pack $root\Mindscape.Raygun4Net.AspNetCore\Mindscape.Raygun4Net.AspNetCore.csproj -c Release -o $build_dir -p:PackageVersion=$version -p:FileVersion=$version
    & dotnet pack $root\Mindscape.Raygun4Net.NetCore\Mindscape.Raygun4Net.NetCore.csproj -c Release -o $build_dir -p:PackageVersion=$version -p:FileVersion=$version
    & dotnet pack $root\Mindscape.Raygun4Net.NetCore.Common\Mindscape.Raygun4Net.NetCore.Common.csproj -c Release -o $build_dir -p:PackageVersion=$version -p:FileVersion=$version

    # Pack nuspecs
    $nuspecs;

    if ($include_signed) {
        $nuspecs = Get-ChildItem -Path $nuspec_folder -Filter *.nuspec -Recurse
    } else {
        $nuspecs = Get-ChildItem -Path $nuspec_folder -Filter *.nuspec
    }
    
    foreach ($nuspec in $nuspecs) {
        Write-Output "Building $($nuspec.Name)"
        & $nuget_dir\nuget.exe pack $nuspec.FullName -OutputDirectory $build_dir -Properties Configuration=Release -Version $version -Verbosity quiet
    }

    # count the number of packages produced and write out
    $packageCount = (Get-ChildItem -Path $build_dir -Filter *.nupkg).Count
    Write-Output "Created $packageCount packages"
}