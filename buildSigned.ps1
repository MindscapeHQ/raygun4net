properties {
    $root =                                 $psake.build_script_dir
    $solution_file =                        "$root/Mindscape.Raygun4Net.sln"
    $solution_file_net2 =                   "$root/Mindscape.Raygun4Net2.sln"
    $solution_file_net4 =                   "$root/Mindscape.Raygun4Net4.sln"
    $solution_file_net4_client_profile =    "$root/Mindscape.Raygun4Net4.ClientProfile.sln"
	$solution_file_net3_client_profile =    "$root/Mindscape.Raygun4Net.ClientProfile.sln"
    $solution_file_mvc =                    "$root/Mindscape.Raygun4Net.Mvc.sln"
	$solution_file_webapi =                 "$root/Mindscape.Raygun4Net.WebApi.sln"
	$solution_file_winrt =                  "$root/Mindscape.Raygun4Net.WinRT.sln" 
    $configuration =                        "Sign"
    $build_dir =                            "$root\build\"
    $signed_build_dir =                     "$build_dir\signed"
    $signed_build_dir_net2 =                "$build_dir\signed\Net2"
    $signed_build_dir_net4 =                "$build_dir\signed\Net4"
    $signed_build_dir_net4_client_profile = "$build_dir\signed\Net4.ClientProfile"
	$signed_build_dir_net3_client_profile = "$build_dir\signed\Net3.ClientProfile"
    $signed_build_dir_mvc =                 "$build_dir\signed\Mvc"
    $signed_build_dir_webapi =              "$build_dir\signed\WebApi"
	$signed_build_dir_winrt =               "$build_dir\signed\WinRT"
    $nuget_dir =                            "$root\.nuget"
    $env:Path +=                            ";$nuget_dir"
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
    exec { msbuild "$solution_file_net2" /m /p:OutDir=$signed_build_dir_net2 /p:Configuration=$configuration }
    exec { msbuild "$solution_file_net4" /m /p:OutDir=$signed_build_dir_net4 /p:Configuration=$configuration }
    exec { msbuild "$solution_file_net3_client_profile" /m /p:OutDir=$signed_build_dir_net3_client_profile /p:Configuration=$configuration }
    exec { msbuild "$solution_file_net4_client_profile" /m /p:OutDir=$signed_build_dir_net4_client_profile /p:Configuration=$configuration }
    exec { msbuild "$solution_file_mvc" /m /p:OutDir=$signed_build_dir_mvc /p:Configuration=$configuration }
    exec { msbuild "$solution_file_webapi" /m /p:OutDir=$signed_build_dir_webapi /p:Configuration=$configuration }
}

task CompileWinRT -depends Init {
    exec { msbuild "$solution_file_winrt" /m /p:OutDir=$signed_build_dir_winrt /p:Configuration=$configuration }
    move-item $signed_build_dir_winrt/Mindscape.Raygun4Net.WinRT/Mindscape.Raygun4Net.WinRT.dll $signed_build_dir_winrt
    move-item $signed_build_dir_winrt/Mindscape.Raygun4Net.WinRT/Mindscape.Raygun4Net.WinRT.pdb $signed_build_dir_winrt
    move-item $signed_build_dir_winrt/Mindscape.Raygun4Net.WinRT.Tests/Mindscape.Raygun4Net.WinRT.Tests.dll $signed_build_dir_winrt
    move-item $signed_build_dir_winrt/Mindscape.Raygun4Net.WinRT.Tests/Mindscape.Raygun4Net.WinRT.Tests.pdb $signed_build_dir_winrt
    remove-item -force -recurse $signed_build_dir_winrt/Mindscape.Raygun4Net.WinRT -ErrorAction SilentlyContinue | Out-Null
    remove-item -force -recurse $signed_build_dir_winrt/Mindscape.Raygun4Net.WinRT.Tests -ErrorAction SilentlyContinue | Out-Null
}