properties {
    $root =          $psake.build_script_dir
    $solution_file = "$root/Mindscape.Raygun4Net.Xamarin.Android.sln"
    $configuration = "Release"
    $build_dir =     "$root\build\"
}

task default -depends CompileXamarinAndroid

task CompileXamarinAndroid {
    exec { msbuild "$solution_file" /m /p:OutDir=$build_dir /p:Configuration=$Configuration }
}
