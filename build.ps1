properties {
    $root =                          $psake.build_script_dir
    $solution_file =                 "$root/Mindscape.Raygun4Net.sln"
	$solution_file2 =                "$root/Mindscape.Raygun4Net2.sln"
    $solution_file4 =                "$root/Mindscape.Raygun4Net4.sln"
	$solution_file4_client_profile = "$root/Mindscape.Raygun4Net4.ClientProfile.sln"
    $solution_file_client_profile =  "$root/Mindscape.Raygun4Net.ClientProfile.sln"
    $solution_file_mvc =             "$root/Mindscape.Raygun4Net.Mvc.sln"
    $solution_file_webjob =          "$root/Mindscape.Raygun4Net.Azure.WebJob.sln"
    $solution_file_webapi =          "$root/Mindscape.Raygun4Net.WebApi.sln"
    $solution_file_winrt =           "$root/Mindscape.Raygun4Net.WinRT.sln"
    $configuration =                 "Release"
    $build_dir =                     "$root\build\"
    $build_dir_client_profile =      "$build_dir\Net3.ClientProfile"
    $build_dir2 =                    "$build_dir\Net2"
    $build_dir4 =                    "$build_dir\Net4"
    $build_dir4_client_profile =     "$build_dir\Net4.ClientProfile"
    $build_dir_mvc =                 "$build_dir\Mvc"
    $build_dir_webjob =              "$build_dir\WebJob"
    $build_dir_webapi =              "$build_dir\WebApi"
    $nunit_dir =                     "$root\packages\NUnit.Runners.2.6.2\tools\"
    $tools_dir =                     "$root\tools"
    $nuget_dir =                     "$root\.nuget"
    $env:Path +=                     ";$nunit_dir;$tools_dir;$nuget_dir"
}

task default -depends Compile, CompileWinRT

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
    exec { msbuild "$solution_file_mvc" /m /p:OutDir=$build_dir_mvc /p:Configuration=$configuration }
    exec { msbuild "$solution_file_webjob" /m /p:OutDir=$build_dir_webjob /p:Configuration=$configuration }
    exec { msbuild "$solution_file_webapi" /m /p:OutDir=$build_dir_webapi /p:Configuration=$configuration }
    
    exec { msbuild "$solution_file_client_profile" /m /p:OutDir=$build_dir_client_profile /p:Configuration=$configuration }
    exec { msbuild "$solution_file4_client_profile" /m /p:OutDir=$build_dir4_client_profile /p:Configuration=$configuration }
}

task CompileWinRT -depends Init {
    exec { msbuild "$solution_file_winrt" /m /p:OutDir=$build_dir /p:Configuration=$configuration }
    move-item $build_dir/Mindscape.Raygun4Net.WinRT/Mindscape.Raygun4Net.WinRT.dll $build_dir
    move-item $build_dir/Mindscape.Raygun4Net.WinRT/Mindscape.Raygun4Net.WinRT.pdb $build_dir
    move-item $build_dir/Mindscape.Raygun4Net.WinRT.Tests/Mindscape.Raygun4Net.WinRT.Tests.dll $build_dir
    move-item $build_dir/Mindscape.Raygun4Net.WinRT.Tests/Mindscape.Raygun4Net.WinRT.Tests.pdb $build_dir
    remove-item -force -recurse $build_dir/Mindscape.Raygun4Net.WinRT -ErrorAction SilentlyContinue | Out-Null
    remove-item -force -recurse $build_dir/Mindscape.Raygun4Net.WinRT.Tests -ErrorAction SilentlyContinue | Out-Null
}

task Test -depends Compile, CompileWinRT {
    $test_assemblies = Get-ChildItem $build_dir -Include *Tests.dll -Name

    Push-Location -Path $build_dir

    exec { nunit-console.exe $test_assemblies }

    Pop-Location
}
