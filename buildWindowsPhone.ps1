properties {
    $root = $psake.build_script_dir
    $solution_file = "$root/Mindscape.Raygun4Net.WindowsPhone.sln"
    $configuration = "Release"
    $build_dir = "$root\build\windowsphone\"
    $nunit_dir = "$root\packages\NUnit.Runners.2.6.2\tools\"
    $tools_dir = "$root\tools"
    $nuget_dir = "$root\.nuget"
    $env:Path += ";$nunit_dir;$tools_dir;$nuget_dir"
}

task default -depends Compile

task Clean {
    remove-item -force -recurse $build_dir -ErrorAction SilentlyContinue | Out-Null
}

task Init -depends Clean {
    new-item $build_dir -itemType directory | Out-Null
}

task Compile -depends Init {
    exec { msbuild "$solution_file" /m /p:OutDir=$build_dir /p:Configuration=$Configuration }
}

task Test -depends Compile {
    $test_assemblies = Get-ChildItem $build_dir -Include *Tests.dll -Name

    Push-Location -Path $build_dir

    exec { nunit-console.exe $test_assemblies }

    Pop-Location
}
