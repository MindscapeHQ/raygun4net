properties {
    $root =                        $psake.build_script_dir
    $solution_file =                 "$root/Mindscape.Raygun4Net.sln"
    $winrt_solution_file =           "$root/Mindscape.Raygun4Net.WinRT.sln"
    $windows_phone_solution_file =   "$root/Mindscape.Raygun4Net.WindowsPhone.sln"
    $nugetspec =                     "$root/Mindscape.Raygun4Net.nuspec"
    $nugetpackage =                  "Mindscape.Raygun4Net.1.0.nupkg"
    $configuration =                 "Release"
    $build_dir =                     "$root\build\"
    $release_dir =                   "$root\release\"
    $nunit_dir =                     "$root\packages\NUnit.Runners.2.6.2\tools\"
    $tools_dir =                     "$root\tools"
    $nuget_dir =                     "$root\.nuget"
    $env:Path +=                     ";$nunit_dir;$tools_dir;$nuget_dir"
}

task default -depends Package

task Clean {
    remove-item -force -recurse $build_dir -ErrorAction SilentlyContinue | Out-Null
    remove-item -force -recurse $release_dir -ErrorAction SilentlyContinue | Out-Null
}

task Init -depends Clean {
    new-item $release_dir -itemType directory | Out-Null
    new-item $build_dir -itemType directory | Out-Null
}

task Compile -depends Init {
    exec { msbuild "$solution_file" /m /p:OutDir=$build_dir /p:Configuration=$configuration }
}

task CompileWinRT -depends Init {
    exec { msbuild "$winrt_solution_file" /m /p:OutDir=$build_dir /p:Configuration=$configuration }
}

task CompileWindowsPhone -depends Init {
    exec { msbuild "$windows_phone_solution_file" /m /p:OutDir=$build_dir /p:Configuration=$Configuration }
}

task Test -depends Compile {
    $test_assemblies = Get-ChildItem $build_dir -Include *Tests.dll -Name

    Push-Location -Path $build_dir

    exec { nunit-console.exe $test_assemblies }

    Pop-Location
}

task Package -depends Compile, CompileWinRT, CompileWindowsPhone {
    
}

task PushNugetPackage -depends Package {
    Push-Location -Path $release_dir

    exec { nuget push "$release_dir*.nupkg" }

    Pop-Location
}
