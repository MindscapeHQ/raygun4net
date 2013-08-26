properties {
    $root =        $psake.build_script_dir
    $nugetspec =   "$root/Mindscape.Raygun4Net.nuspec"
    $release_dir = "$root\release\"
	$nuget_dir =   "$root\.nuget"
    $env:Path +=   ";$nuget_dir"
}

task default -depends Package

task Package {
    exec { nuget pack $nugetspec -OutputDirectory $release_dir }
}

task PushNugetPackage -depends Package {
    Push-Location -Path $release_dir

    exec { nuget push "$release_dir*.nupkg" }

    Pop-Location
}