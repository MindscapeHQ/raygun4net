properties {
    $root =        $psake.build_script_dir
    $nugetspec =   "$root/Mindscape.Raygun4Net.nuspec"
	$build_dir =   "$root\build\"
    $release_dir = "$root\release\"
	$nuget_dir =   "$root\.nuget"
	$folder =      "$root\release\"
    $env:Path +=   ";$nuget_dir"
}

task default -depends Zip

task Clean {
    remove-item -force -recurse $release_dir -ErrorAction SilentlyContinue | Out-Null
}

task Init -depends Clean {
    new-item $release_dir -itemType directory | Out-Null
}

task Package -depends Init {
    exec { nuget pack $nugetspec -OutputDirectory $release_dir }
}

task PushNugetPackage -depends Package {
    Push-Location -Path $release_dir

    exec { nuget push "$release_dir*.nupkg" }

    Pop-Location
}

task Zip -depends Package {
    $release = Get-ChildItem $release_dir
    $nupkg_name = $release.Name
	$nupkg_name = $nupkg_name -replace "Mindscape.Raygun4Net.", "v"
	$version = $nupkg_name -replace ".nupkg", ""
	
    $outerfolder = $release_dir + $version
    $versionfolder = $release_dir + $version + "\" + $version
    new-item $versionfolder -itemType directory | Out-Null
	
	copy-item $build_dir/Mindscape.Raygun4Net.dll $versionfolder
	copy-item $build_dir/Mindscape.Raygun4Net.pdb $versionfolder
	copy-item $build_dir/Mindscape.Raygun4Net.WindowsPhone.dll $versionfolder
	copy-item $build_dir/Mindscape.Raygun4Net.WindowsPhone.pdb $versionfolder
	copy-item $build_dir/Mindscape.Raygun4Net.WinRT.dll $versionfolder
	copy-item $build_dir/Mindscape.Raygun4Net.WinRT.pdb $versionfolder
	copy-item $build_dir/Mindscape.Raygun4Net.Xamarin.Android.dll $versionfolder
	copy-item $build_dir/Mindscape.Raygun4Net.Xamarin.Android.pdb $versionfolder
	copy-item $build_dir/Mindscape.Raygun4Net.Xamarin.iOS.dll $versionfolder
	
    $zipFullName = $folder + $version + ".zip"
    Get-ChildItem $outerfolder | Add-Zip $zipFullName
}

function Add-Zip # usage: Get-ChildItem $folder | Add-Zip $zipFullName
{
    param([string]$zipfilename)

    if(!(test-path($zipfilename)))
    {
        set-content $zipfilename ("PK" + [char]5 + [char]6 + ("$([char]0)" * 18))
        (dir $zipfilename).IsReadOnly = $false
    }
    $shellApplication = new-object -com shell.application
    $zipPackage = $shellApplication.NameSpace($zipfilename)
    foreach($file in $input)
    { 
        $zipPackage.CopyHere($file.FullName)
        do {
            Start-sleep 2
        } until ( $zipPackage.Items() | select {$_.Name -eq $file.Name} )
    }
}