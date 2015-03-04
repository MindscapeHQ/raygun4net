properties {
    $root =                             $psake.build_script_dir
    $solution_file =                    "$root/Mindscape.Raygun4Net.sln"
    $solution_file_client_profile =     "$root/Mindscape.Raygun4Net.ClientProfile.sln"
    $solution_file2 =                   "$root/Mindscape.Raygun4Net2.sln"
    $solution_file4 =                   "$root/Mindscape.Raygun4Net4.sln"
    $solution_file4_client_profile =    "$root/Mindscape.Raygun4Net4.ClientProfile.sln"
    $solution_file_winrt =              "$root/Mindscape.Raygun4Net.WinRT.sln"
    $configuration =                    "Sign"
    $build_dir =                        "$root\build\"
    $signed_build_dir =                 "$build_dir\signed"
    $signed_build_dir_client_profile =  "$build_dir\signed\Net3.ClientProfile"
    $signed_build_dir2 =                "$build_dir\signed\Net2"
    $signed_build_dir4 =                "$build_dir\signed\Net4"
    $signed_build_dir4_client_profile = "$build_dir\signed\Net4.ClientProfile"
    $nuget_dir =                        "$root\.nuget"
    $env:Path +=                        ";$nuget_dir"
}

task default -depends Compile, CompileWinRT

task Clean {
    remove-item -force -recurse $signed_build_dir -ErrorAction SilentlyContinue | Out-Null
}

task Init -depends Clean {
    new-item $signed_build_dir -itemType directory | Out-Null
}

task Compile -depends Init {
    exec { msbuild "$solution_file" /m /p:OutDir=$signed_build_dir /p:Configuration=$configuration }
    exec { msbuild "$solution_file2" /m /p:OutDir=$signed_build_dir2 /p:Configuration=$configuration }
    exec { msbuild "$solution_file4" /m /p:OutDir=$signed_build_dir4 /p:Configuration=$configuration }
    
    exec { msbuild "$solution_file_client_profile" /m /p:OutDir=$signed_build_dir_client_profile /p:Configuration=$configuration }
    exec { msbuild "$solution_file4_client_profile" /m /p:OutDir=$signed_build_dir4_client_profile /p:Configuration=$configuration }
}

task CompileWinRT -depends Init {
    exec { msbuild "$solution_file_winrt" /m /p:OutDir=$signed_build_dir /p:Configuration=$configuration }
    move-item $signed_build_dir/Mindscape.Raygun4Net.WinRT/Mindscape.Raygun4Net.WinRT.dll $signed_build_dir
    move-item $signed_build_dir/Mindscape.Raygun4Net.WinRT/Mindscape.Raygun4Net.WinRT.pdb $signed_build_dir
    move-item $signed_build_dir/Mindscape.Raygun4Net.WinRT.Tests/Mindscape.Raygun4Net.WinRT.Tests.dll $signed_build_dir
    move-item $signed_build_dir/Mindscape.Raygun4Net.WinRT.Tests/Mindscape.Raygun4Net.WinRT.Tests.pdb $signed_build_dir
    remove-item -force -recurse $signed_build_dir/Mindscape.Raygun4Net.WinRT -ErrorAction SilentlyContinue | Out-Null
    remove-item -force -recurse $signed_build_dir/Mindscape.Raygun4Net.WinRT.Tests -ErrorAction SilentlyContinue | Out-Null
}