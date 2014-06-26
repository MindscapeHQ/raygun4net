properties {
    $root =                        $psake.build_script_dir
    $solution_file =                 "$root/Mindscape.Raygun4Net.sln"
    $solution_file2 =                "$root/Mindscape.Raygun4Net2.sln"
    $solution_file4 =                "$root/Mindscape.Raygun4Net4.sln"
    $winrt_solution_file =           "$root/Mindscape.Raygun4Net.WinRT.sln"
    $windows_phone_solution_file =   "$root/Mindscape.Raygun4Net.WindowsPhone.sln"
    $windows_phone_81_solution_file =   "$root/Mindscape.Raygun4Net.WindowsPhone81.sln"
    $nugetspec =                     "$root/Mindscape.Raygun4Net.nuspec"
    $nugetpackage =                  "Mindscape.Raygun4Net.1.0.nupkg"
    $configuration =                 "Release"
    $build_dir =                     "$root\build\"
    $build_dir2 =                    "$root\build\Net2"
    $build_dir4 =                    "$root\build\Net4"
    $nunit_dir =                     "$root\packages\NUnit.Runners.2.6.2\tools\"
    $tools_dir =                     "$root\tools"
    $nuget_dir =                     "$root\.nuget"
    $env:Path +=                     ";$nunit_dir;$tools_dir;$nuget_dir"
}

task default -depends Compile, CompileWinRT, CompileWindowsPhone, CompileWindowsPhone81

task Clean {
    remove-item -force -recurse $build_dir -ErrorAction SilentlyContinue | Out-Null
}

task Init -depends Clean {
    new-item $build_dir -itemType directory | Out-Null
}

task Compile -depends Init {
    exec { msbuild "$solution_file" /m /p:OutDir=$build_dir /p:Configuration=$configuration }
    exec { msbuild "$solution_file2" /m /p:OutDir=$build_dir2 /p:Configuration=$configuration }
    exec { msbuild "$solution_file4" /m /p:OutDir=$build_dir4 /p:Configuration=$configuration }
}

task CompileWinRT -depends Init {
    exec { msbuild "$winrt_solution_file" /m /p:OutDir=$build_dir /p:Configuration=$configuration }
    move-item $build_dir/Mindscape.Raygun4Net.WinRT/Mindscape.Raygun4Net.WinRT.dll $build_dir
    move-item $build_dir/Mindscape.Raygun4Net.WinRT/Mindscape.Raygun4Net.WinRT.pdb $build_dir
    move-item $build_dir/Mindscape.Raygun4Net.WinRT.Tests/Mindscape.Raygun4Net.WinRT.Tests.dll $build_dir
    move-item $build_dir/Mindscape.Raygun4Net.WinRT.Tests/Mindscape.Raygun4Net.WinRT.Tests.pdb $build_dir
    remove-item -force -recurse $build_dir/Mindscape.Raygun4Net.WinRT -ErrorAction SilentlyContinue | Out-Null
    remove-item -force -recurse $build_dir/Mindscape.Raygun4Net.WinRT.Tests -ErrorAction SilentlyContinue | Out-Null
}

task CompileWindowsPhone -depends Init {
    exec { msbuild "$windows_phone_solution_file" /m /p:OutDir=$build_dir /p:Configuration=$Configuration }
}

task CompileWindowsPhone81 -depends Init {
    exec { msbuild "$windows_phone_81_solution_file" /m /p:OutDir=$build_dir /p:Configuration=$Configuration }
}

task Test -depends Compile, CompileWinRT, CompileWindowsPhone, CompileWindowsPhone81 {
    $test_assemblies = Get-ChildItem $build_dir -Include *Tests.dll -Name

    Push-Location -Path $build_dir

    exec { nunit-console.exe $test_assemblies }

    Pop-Location
}
