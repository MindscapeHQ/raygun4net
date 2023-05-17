# This causes psake to use the VS 2019 build tool:
Framework "4.8"

properties {
    $root =                       $psake.build_script_dir
    $solution_file_net4 =         "$root/Mindscape.Raygun4Net4.sln"
    $solution_file_net4_client =  "$root/Mindscape.Raygun4Net4.ClientProfile.sln"
    $solution_file_mvc =          "$root/Mindscape.Raygun4Net.Mvc.sln"
    $solution_file_webapi =       "$root/Mindscape.Raygun4Net.WebApi.sln"
    $configuration =              "Sign"
    $build_dir =                  "$root\build\signed"
    $build_dir_net4 =             "$build_dir\net40"
    $build_dir_net4_client =      "$build_dir\net40-client"
    $build_dir_mvc =              "$build_dir\mvc"
    $build_dir_webapi =           "$build_dir\webapi"
    $nuget_dir =                  "$root\.nuget"
    $env:Path +=                  ";$nuget_dir"
}

task default -depends Compile

task Clean {
    remove-item -force -recurse $build_dir -ErrorAction SilentlyContinue | Out-Null
}

task Init -depends Clean {
    new-item $build_dir -itemType directory | Out-Null
}

task Compile -depends Init {
    
    exec { msbuild "$solution_file_net4"  /p:OutDir=$build_dir_net4 /p:Configuration=$configuration }
    exec { msbuild "$solution_file_net4_client" /m /p:OutDir=$build_dir_net4_client /p:Configuration=$configuration }
    exec { msbuild "$solution_file_mvc" /m /p:OutDir=$build_dir_mvc /p:Configuration=$configuration }
    exec { msbuild "$solution_file_webapi" /m /p:OutDir=$build_dir_webapi /p:Configuration=$configuration }
}
