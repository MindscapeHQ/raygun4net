properties {
    $root =                 $psake.build_script_dir
    $raygun_project =       "$root/Mindscape.Raygun4Net/Mindscape.Raygun4Net.csproj"
    $rayguntests_project =  "$root/Mindscape.Raygun4Net.Tests/Mindscape.Raygun4Net.Tests.csproj"
    $raygunwinrt_project =  "$root/Mindscape.Raygun4Net.WinRT/Mindscape.Raygun4Net.WinRT.csproj"
    $nugetspec =            "$root/Mindscape.Raygun4Net.nuspec"
    $nugetpackage =         "Mindscape.Raygun4Net.1.0.nupkg"
    $configuration =        "Debug"
    $build_dir =            "$root\build\"
    $release_dir =          "$root\release\"
    $nunit_dir =            "$root\packages\NUnit.Runners.2.6.2\tools\"
    $tools_dir =            "$root\tools"
    $nuget_dir =            "$root\.nuget"
    $env:Path +=            ";$nunit_dir;$tools_dir;$nuget_dir"
    $assemblies_to_merge =  "Mindscape.Raygun4Net.dll", `
                            "Newtonsoft.Json.dll"
    $winrt_assemblies =     "Mindscape.Raygun4Net.WinRT.dll", `
                            "Newtonsoft.Json.dll"
    $merged_assemlby_name = "Mindscape.Raygun4Net.dll"
    $windowsversion =       (Get-WmiObject Win32_OperatingSystem).Version
}

task default -depends Package

task Clean {
    remove-item -force -recurse $build_dir -ErrorAction SilentlyContinue | Out-Null
    remove-item -force -recurse $release_dir -ErrorAction SilentlyContinue | Out-Null
}

task Init -depends Clean {
    new-item $release_dir -itemType directory | Out-Null
    new-item "${release_dir}/lib" -itemType directory | Out-Null
    new-item "${release_dir}/lib/net35" -itemType directory | Out-Null
    new-item "${release_dir}/lib/windows8" -itemType directory | Out-Null
    new-item $build_dir -itemType directory | Out-Null
}

task Compile -depends Init {

    exec { msbuild "$raygun_project" /m /p:OutDir=$build_dir /p:Configuration=$configuration }
    exec { msbuild "$rayguntests_project" /m /p:OutDir=$build_dir /p:Configuration=$configuration }

    if($windowsversion -ge 6.2) { #if we're using Windows 8 or better
        echo "building winrt version for $windowsversion"
        exec { msbuild "$raygunwinrt_project" /m /p:OutDir=$build_dir /p:Configuration=$configuration }
    }
}

task Test -depends Compile {
    $test_assemblies = Get-ChildItem $build_dir -Include *Tests.dll -Name

    Push-Location -Path $build_dir

    exec { nunit-console.exe $test_assemblies }

    Pop-Location
}

task Merge -depends Compile {
    Push-Location -Path $build_dir

    exec { ilmerge.exe /internalize /out:"${release_dir}/lib/net35/${merged_assemlby_name}" $assemblies_to_merge }

    Push-Location -Path "Mindscape.Raygun4Net.WinRT"

    exec { ilmerge.exe /internalize /out:"${release_dir}/lib/windows8/${merged_assemlby_name}" $winrt_assemblies }

    Pop-Location

    Pop-Location
}

task Package -depends Merge {
    Copy-Item readme.txt $release_dir/readme.txt
    Copy-Item $nugetspec $release_dir

    Push-Location -Path $release_dir

    exec { nuget pack }

    Pop-Location
}

task PushNugetPackage -depends Package {
    Push-Location -Path $release_dir

    exec { nuget push "$release_dir*.nupkg" }

    Pop-Location
}
