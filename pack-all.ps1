# This causes psake to use the VS 2019 build tool:
Framework "4.8"

properties {
    $root = $psake.build_script_dir
    $build_dir = "$root\build"
    $nuget_dir = "$root\.nuget"
    $nuspec_folder = "$root\nuspecs"
    # Assembly Info should stay the same until there's an API change and we increment it
    $assemblyVersion = "8.0.0"
    $version = "8.0.0"
    $include_signed = $false
    $global_assembly_info = ".\GlobalAssemblyInfo.cs"
    $is_pre_release = $true
    $pre_release_counter = 1
}

task default -depends Pack

task Clean {
    remove-item -force -recurse $build_dir -ErrorAction SilentlyContinue | Out-Null
}

task Build -depends Clean {
    # Replace AssemblyVersion and AssemblyFileVersion
    (Get-Content $global_assembly_info) -replace 'Version\("[^"]*"\)\]', "Version(`"$assemblyVersion`")]" | Set-Content $global_assembly_info
    (Get-Content $global_assembly_info) -replace 'AssemblyInformationalVersion\("[^"]*"\)\]', "AssemblyInformationalVersion(`"$version`")]" | Set-Content $global_assembly_info

    & dotnet clean $root\Raygun.CrashReporting.sln -c Release
    & dotnet build $root\Raygun.CrashReporting.sln -c Release -p:FileVersion=$version -p:Version=$version -p:AssemblyVersion=$assemblyVersion
}

task Pack -depends Build {

    $package_version = $version
    if ($is_pre_release) {
        $package_version = "$version-pre-$pre_release_counter"
    }

    # Pack .NET SDK Projects (these use the project files and not nuspecs)
    # Because of old style projects we need to pack the specific projects directly
    & dotnet pack $root\Mindscape.Raygun4Net.AspNetCore\Mindscape.Raygun4Net.AspNetCore.csproj -c Release -o $build_dir -p:PackageVersion=$package_version -p:FileVersion=$version -p:Version=$version -p:AssemblyVersion=$assemblyVersion
    & dotnet pack $root\Mindscape.Raygun4Net.NetCore\Mindscape.Raygun4Net.NetCore.csproj -c Release -o $build_dir -p:PackageVersion=$package_version -p:FileVersion=$version -p:Version=$version -p:AssemblyVersion=$assemblyVersion
    & dotnet pack $root\Mindscape.Raygun4Net.NetCore.Common\Mindscape.Raygun4Net.NetCore.Common.csproj -c Release -o $build_dir -p:PackageVersion=$package_version -p:FileVersion=$version -p:Version=$version -p:AssemblyVersion=$assemblyVersion

    # Pack nuspecs
    $nuspecs;

    if ($include_signed) {
        $nuspecs = Get-ChildItem -Path $nuspec_folder -Filter *.nuspec -Recurse
    } else {
        $nuspecs = Get-ChildItem -Path $nuspec_folder -Filter *.nuspec
    }
    
    foreach ($nuspec in $nuspecs) {
        Write-Output "Building $($nuspec.Name)"
        & $nuget_dir\nuget.exe pack $nuspec.FullName -OutputDirectory $build_dir -Properties Configuration=Release -Version $package_version -Verbosity quiet
    }

    # count the number of packages produced and write out
    $packageCount = (Get-ChildItem -Path $build_dir -Filter *.nupkg).Count
    Write-Output "Created $packageCount packages"
}
