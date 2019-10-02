properties {
    $root =                             $psake.build_script_dir
    $configuration =                    "Release"
    $build_dir =                        "$root\build\"
    $build_dir_net_core =               "$build_dir\netcore"
    $build_dir_aspnet_core  =           "$build_dir\asp-netcore"
    $build_dir_net_core_common  =       "$build_dir\netcore-common"
	$build_dir_signed_net_core =        "$build_dir\signed\netcore"
	$build_dir_signed_net_core_common = "$build_dir\signed\netcore-common"
    $nunit_dir =                        "$root\packages\NUnit.Runners.2.6.2\tools\"
    $tools_dir =                        "$root\tools"
    $nuget_dir =                        "$root\.nuget"
    $env:Path +=                        ";$nunit_dir;$tools_dir;$nuget_dir"
}

task default -depends Compile

task Clean {
    remove-item -force -recurse $build_dir_net_core -ErrorAction SilentlyContinue | Out-Null
    remove-item -force -recurse $build_dir_aspnet_core -ErrorAction SilentlyContinue | Out-Null
    remove-item -force -recurse $build_dir_net_core_common -ErrorAction SilentlyContinue | Out-Null
	remove-item -force -recurse $build_dir_signed_net_core -ErrorAction SilentlyContinue | Out-Null
	remove-item -force -recurse $build_dir_signed_net_core_common -ErrorAction SilentlyContinue | Out-Null
}

task Init -depends Clean {
    new-item $build_dir_net_core -itemType directory | Out-Null
    new-item $build_dir_aspnet_core -itemType directory | Out-Null
    new-item $build_dir_net_core_common -itemType directory | Out-Null
	new-item $build_dir_signed_net_core -itemType directory | Out-Null
	new-item $build_dir_signed_net_core_common -itemType directory | Out-Null
}

task Compile -depends Init {
    exec { dotnet pack .\Mindscape.Raygun4Net.NetCore.Common\ --output build\NetCoreCommon --configuration Release }
    move-item -Path $root\Mindscape.Raygun4Net.NetCore.Common\build\NetCoreCommon\* -Destination $build_dir_net_core_common
    
    exec { dotnet pack .\Mindscape.Raygun4Net.NetCore\ --output build\NetCore --configuration Release }
    move-item -Path $root\Mindscape.Raygun4Net.NetCore\build\NetCore\* -Destination $build_dir_net_core
    
    exec { dotnet pack .\Mindscape.Raygun4Net.AspNetCore\ --output build\AspNetCore --configuration Release }
    move-item -Path $root\Mindscape.Raygun4Net.AspNetCore\build\AspNetCore\* -Destination $build_dir_aspnet_core

	# Signed
	exec { dotnet pack .\Mindscape.Raygun4Net.NetCore.Common\ --output build\Signed\NetCoreCommon --configuration Sign }
    move-item -Path $root\Mindscape.Raygun4Net.NetCore.Common\build\Signed\NetCoreCommon\* -Destination $build_dir_signed_net_core_common
    
	# Signed
    exec { dotnet pack .\Mindscape.Raygun4Net.NetCore\ --output build\Signed\NetCore --configuration Sign }
    move-item -Path $root\Mindscape.Raygun4Net.NetCore\build\Signed\NetCore\* -Destination $build_dir_signed_net_core
}