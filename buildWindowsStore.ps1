properties {
    $root =                        $psake.build_script_dir
    $windows_store_solution_file =   "$root/Mindscape.Raygun4Net.WindowsStore.sln"
    $configuration =                 "Release"
    $signed_configuration =          "Sign"
    $build_dir =                     "$root\build\uwp"
    $signed_build_dir =              "$root\build\signed\uwp"
    $msbuild12 =                     "${env:ProgramFiles(x86)}\msbuild\12.0\bin\msbuild.exe"
}

task default -depends CompileWindowsStore, CompileWindowsStoreSigned

task CompileWindowsStore {
    & $msbuild12 $windows_store_solution_file /m /p:OutDir=$build_dir /p:Configuration=$Configuration
    move-item $build_dir/Mindscape.Raygun4Net.WindowsStore/Mindscape.Raygun4Net.WindowsStore.dll $build_dir
    move-item $build_dir/Mindscape.Raygun4Net.WindowsStore/Mindscape.Raygun4Net.WindowsStore.pdb $build_dir
    move-item $build_dir/Mindscape.Raygun4Net.WindowsStore.Tests/Mindscape.Raygun4Net.WindowsStore.Tests.dll $build_dir
    move-item $build_dir/Mindscape.Raygun4Net.WindowsStore.Tests/Mindscape.Raygun4Net.WindowsStore.Tests.pdb $build_dir
    remove-item -force -recurse $build_dir/Mindscape.Raygun4Net.WindowsStore -ErrorAction SilentlyContinue | Out-Null
    remove-item -force -recurse $build_dir/Mindscape.Raygun4Net.WindowsStore.Tests -ErrorAction SilentlyContinue | Out-Null
}

task CompileWindowsStoreSigned {
    & $msbuild12 $windows_store_solution_file /m /p:OutDir=$signed_build_dir /p:Configuration=$signed_configuration
    move-item $signed_build_dir/Mindscape.Raygun4Net.WindowsStore/Mindscape.Raygun4Net.WindowsStore.dll $signed_build_dir
    move-item $signed_build_dir/Mindscape.Raygun4Net.WindowsStore/Mindscape.Raygun4Net.WindowsStore.pdb $signed_build_dir
    move-item $signed_build_dir/Mindscape.Raygun4Net.WindowsStore.Tests/Mindscape.Raygun4Net.WindowsStore.Tests.dll $signed_build_dir
    move-item $signed_build_dir/Mindscape.Raygun4Net.WindowsStore.Tests/Mindscape.Raygun4Net.WindowsStore.Tests.pdb $signed_build_dir
    remove-item -force -recurse $signed_build_dir/Mindscape.Raygun4Net.WindowsStore -ErrorAction SilentlyContinue | Out-Null
    remove-item -force -recurse $signed_build_dir/Mindscape.Raygun4Net.WindowsStore.Tests -ErrorAction SilentlyContinue | Out-Null
}
