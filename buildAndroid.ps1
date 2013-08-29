properties {
    $root =                        $psake.build_script_dir
    $xamarin_android_solution_file = "$root/Mindscape.Raygun4Net.Xamarin.Android.sln"
	$configuration =                 "Release"
	$build_dir =                     "$root\build\"
}

task default -depends CompileXamarinAndroid

task CompileXamarinAndroid {
    exec { msbuild "$xamarin_android_solution_file" /m /p:OutDir=$build_dir /p:Configuration=$Configuration }
}
