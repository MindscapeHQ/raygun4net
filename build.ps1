properties {
    $root =                 $psake.build_script_dir
    $solution_file =        "$root/Mindscape.Raygun4Net.sln"
    $configuration =        "Debug"
    $build_dir =            "$root\build\"
    $release_dir =          "$root\release\"
    $nunit_dir =            "$root\packages\NUnit.Runners.2.6.2\tools\"
    $tools_dir =            "$root\tools"
    $env:Path +=            ";$nunit_dir;$tools_dir"
    $assemblies_to_merge =  "Mindscape.Raygun4Net.dll", `
                            "Newtonsoft.Json.dll"
    $merged_assemlby_name = "Mindscape.Raygun4Net.dll"
}

task default -depends Merge

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

task Test -depends Compile {
    $test_assemblies = Get-ChildItem $build_dir -Include *Tests.dll -Name

    Push-Location -Path $build_dir

    exec { nunit-console.exe $test_assemblies }

    Pop-Location
}

task Merge -depends Compile {
    Push-Location -Path $build_dir

    exec { ilmerge.exe /out:"$release_dir\$merged_assemlby_name" $assemblies_to_merge }

    Pop-Location
}
