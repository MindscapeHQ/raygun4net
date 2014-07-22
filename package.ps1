properties {
    $root =                          $psake.build_script_dir
    $nugetspec =                     "$root/Mindscape.Raygun4Net.nuspec"
    $signed_nugetspec =              "$root/Mindscape.Raygun4Net.signed.nuspec"
    $solution_file =                 "$root/Mindscape.Raygun4Net.sln"
    $solution_file2 =                "$root/Mindscape.Raygun4Net2.sln"
    $solution_file4 =                "$root/Mindscape.Raygun4Net4.sln"
    $winrt_solution_file =           "$root/Mindscape.Raygun4Net.WinRT.sln"
    $configuration =                 "Sign"
    $build_dir =                     "$root\build\"
    $build_dir2 =                    "$root\build\Net2"
    $build_dir4 =                    "$root\build\Net4"
    $signed_build_dir =              "$build_dir\signed"
    $signed_build_dir2 =             "$build_dir\signed\Net2"
    $signed_build_dir4 =             "$build_dir\signed\Net4"
    $release_dir =                   "$root\release\"
    $nuget_dir =                     "$root\.nuget"
    $env:Path +=                     ";$nuget_dir"
}

task default -depends Zip

task Clean {
    remove-item -force -recurse $release_dir -ErrorAction SilentlyContinue | Out-Null
    remove-item -force -recurse $signed_build_dir -ErrorAction SilentlyContinue | Out-Null
}

task Init -depends Clean {
    new-item $release_dir -itemType directory | Out-Null
    new-item $signed_build_dir -itemType directory | Out-Null
}

task Compile -depends Init {
    exec { msbuild "$solution_file" /m /p:OutDir=$signed_build_dir /p:Configuration=$configuration }
    exec { msbuild "$solution_file2" /m /p:OutDir=$signed_build_dir2 /p:Configuration=$configuration }
    exec { msbuild "$solution_file4" /m /p:OutDir=$signed_build_dir4 /p:Configuration=$configuration }
}

task CompileWinRT -depends Init {
    exec { msbuild "$winrt_solution_file" /m /p:OutDir=$signed_build_dir /p:Configuration=$configuration }
    move-item $signed_build_dir/Mindscape.Raygun4Net.WinRT/Mindscape.Raygun4Net.WinRT.dll $signed_build_dir
    move-item $signed_build_dir/Mindscape.Raygun4Net.WinRT/Mindscape.Raygun4Net.WinRT.pdb $signed_build_dir
    move-item $signed_build_dir/Mindscape.Raygun4Net.WinRT.Tests/Mindscape.Raygun4Net.WinRT.Tests.dll $signed_build_dir
    move-item $signed_build_dir/Mindscape.Raygun4Net.WinRT.Tests/Mindscape.Raygun4Net.WinRT.Tests.pdb $signed_build_dir
    remove-item -force -recurse $signed_build_dir/Mindscape.Raygun4Net.WinRT -ErrorAction SilentlyContinue | Out-Null
    remove-item -force -recurse $signed_build_dir/Mindscape.Raygun4Net.WinRT.Tests -ErrorAction SilentlyContinue | Out-Null
}

task Package -depends Compile, CompileWinRT {
    exec { nuget pack $nugetspec -OutputDirectory $release_dir }
    exec { nuget pack $signed_nugetspec -OutputDirectory $release_dir }
}

task PushNugetPackage -depends Package {
    Push-Location -Path $release_dir

    exec { nuget push "$release_dir*.nupkg" }

    Pop-Location
}

task Zip -depends Package {
    $release = Get-ChildItem $release_dir | Select-Object -f 1
    $nupkg_name = $release.Name
    $nupkg_name = $nupkg_name -replace "Mindscape.Raygun4Net.", "v"
    $version = $nupkg_name -replace ".nupkg", ""
    
    $outerfolder = $release_dir + $version
    $versionfolder = $release_dir + $version + "\" + $version
    $versionfolder2 = $release_dir + $version + "\" + $version + "\Net2"
    $versionfolder4 = $release_dir + $version + "\" + $version + "\Net4"
    $signedfolder = $versionfolder + "\signed"
    $signedfolder2 = $versionfolder + "\signed\Net2"
    $signedfolder4 = $versionfolder + "\signed\Net4"
    new-item $versionfolder -itemType directory | Out-Null
    new-item $versionfolder2 -itemType directory | Out-Null
    new-item $versionfolder4 -itemType directory | Out-Null
    new-item $signedfolder -itemType directory | Out-Null
    new-item $signedfolder2 -itemType directory | Out-Null
    new-item $signedfolder4 -itemType directory | Out-Null
  
    # .Net 3.5
    copy-item $build_dir/Mindscape.Raygun4Net.dll $versionfolder
    copy-item $build_dir/Mindscape.Raygun4Net.pdb $versionfolder
	# Windows Phone
    copy-item $build_dir/Mindscape.Raygun4Net.WindowsPhone.dll $versionfolder
    copy-item $build_dir/Mindscape.Raygun4Net.WindowsPhone.pdb $versionfolder
	# WinRT
    copy-item $build_dir/Mindscape.Raygun4Net.WinRT.dll $versionfolder
    copy-item $build_dir/Mindscape.Raygun4Net.WinRT.pdb $versionfolder
	# Xamarin.Android
    copy-item $build_dir/Mindscape.Raygun4Net.Xamarin.Android.dll $versionfolder
    copy-item $build_dir/Mindscape.Raygun4Net.Xamarin.Android.pdb $versionfolder
	# Xamarin.iOS
    copy-item $build_dir/Mindscape.Raygun4Net.Xamarin.iOS.dll $versionfolder
	# Windows Store
	copy-item $build_dir/Mindscape.Raygun4Net.WindowsStore.dll $versionfolder
    copy-item $build_dir/Mindscape.Raygun4Net.WindowsStore.pdb $versionfolder
	# .Net 2.0
    copy-item $build_dir2/Mindscape.Raygun4Net.dll $versionfolder2
    copy-item $build_dir2/Mindscape.Raygun4Net.pdb $versionfolder2
	# .Net 4.0
    copy-item $build_dir4/Mindscape.Raygun4Net.dll $versionfolder4
    copy-item $build_dir4/Mindscape.Raygun4Net.pdb $versionfolder4
	# Signed
    copy-item $signed_build_dir/Mindscape.Raygun4Net.dll $signedfolder
    copy-item $signed_build_dir/Mindscape.Raygun4Net.WinRT.dll $signedfolder
	copy-item $signed_build_dir/Mindscape.Raygun4Net.WindowsStore.dll $signedfolder
    copy-item $signed_build_dir2/Mindscape.Raygun4Net.dll $signedfolder2
    copy-item $signed_build_dir4/Mindscape.Raygun4Net.dll $signedfolder4
	
    $zipFullName = $release_dir + $version + ".zip"
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