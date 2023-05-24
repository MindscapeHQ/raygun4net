# This causes psake to use the VS 2017 build tool:
Framework "4.6"

properties {
    $root =                       $psake.build_script_dir
    $solution_file_net2 =         "$root/Mindscape.Raygun4Net2.sln"
    $solution_file_net35 =        "$root/Mindscape.Raygun4Net.sln"
    $solution_file_net35_client = "$root/Mindscape.Raygun4Net.ClientProfile.sln"
    $solution_file_net4 =         "$root/Mindscape.Raygun4Net4.sln"
    $solution_file_net4_client =  "$root/Mindscape.Raygun4Net4.ClientProfile.sln"
    $solution_file_mvc =          "$root/Mindscape.Raygun4Net.Mvc.sln"
    $solution_file_webapi =       "$root/Mindscape.Raygun4Net.WebApi.sln"
    $solution_file_winrt =        "$root/Mindscape.Raygun4Net.WinRT.sln" 
    $configuration =              "Sign"
    $build_dir =                  "$root\build\signed"
    $build_dir_net2 =             "$build_dir\net20"
    $build_dir_net35 =            "$build_dir\net35"
    $build_dir_net35_client =     "$build_dir\net35-client"
    $build_dir_net4 =             "$build_dir\net40"
    $build_dir_net4_client =      "$build_dir\net40-client"
    $build_dir_mvc =              "$build_dir\mvc"
    $build_dir_webapi =           "$build_dir\webapi"
    $build_dir_winrt =            "$build_dir\winrt"
    $nuget_dir =                  "$root\.nuget"
    $env:Path +=                  ";$nuget_dir"
}

task default -depends Compile #, CompileWinRT

task Clean {
    remove-item -force -recurse $build_dir -ErrorAction SilentlyContinue | Out-Null
}

task Init -depends Clean {
    new-item $build_dir -itemType directory | Out-Null
}

task Compile -depends Init {
    
    exec { msbuild "$solution_file_net2" /m /p:OutDir=$build_dir_net2 /p:Configuration=$configuration }
    exec { msbuild "$solution_file_net35" /m /p:OutDir=$build_dir_net35 /p:Configuration=$configuration }
    exec { msbuild "$solution_file_net35_client" /m /p:OutDir=$build_dir_net35_client /p:Configuration=$configuration }
    exec { "msbuild.exe $solution_file_net4 /m /p:OutDir=$build_dir_net4 /p:Configuration=$configuration" }
    exec { msbuild "$solution_file_net4_client" /m /p:OutDir=$build_dir_net4_client /p:Configuration=$configuration }
    exec { msbuild "$solution_file_mvc" /m /p:OutDir=$build_dir_mvc /p:Configuration=$configuration }
    exec { msbuild "$solution_file_webapi" /m /p:OutDir=$build_dir_webapi /p:Configuration=$configuration }
}

task CompileWinRT -depends Init {
    exec { msbuild "$solution_file_winrt" /m /p:OutDir=$build_dir_winrt /p:Configuration=$configuration }
    move-item $build_dir_winrt/Mindscape.Raygun4Net.WinRT/Mindscape.Raygun4Net.WinRT.dll $build_dir_winrt
    move-item $build_dir_winrt/Mindscape.Raygun4Net.WinRT/Mindscape.Raygun4Net.WinRT.pdb $build_dir_winrt
    move-item $build_dir_winrt/Mindscape.Raygun4Net.WinRT.Tests/Mindscape.Raygun4Net.WinRT.Tests.dll $build_dir_winrt
    move-item $build_dir_winrt/Mindscape.Raygun4Net.WinRT.Tests/Mindscape.Raygun4Net.WinRT.Tests.pdb $build_dir_winrt
    remove-item -force -recurse $build_dir_winrt/Mindscape.Raygun4Net.WinRT -ErrorAction SilentlyContinue | Out-Null
    remove-item -force -recurse $build_dir_winrt/Mindscape.Raygun4Net.WinRT.Tests -ErrorAction SilentlyContinue | Out-Null
}