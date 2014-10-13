properties {
    $root =                          $psake.build_script_dir
    $nugetspec =                     "$root/Mindscape.Raygun4Net.nuspec"
    $nugetspec_signed =              "$root/Mindscape.Raygun4Net.signed.nuspec"
    $nugetspec_core =                "$root/Mindscape.Raygun4Net.Core.nuspec"
    $nugetspec_mvc =                 "$root/Mindscape.Raygun4Net.Mvc.nuspec"
    $nugetspec_webapi =              "$root/Mindscape.Raygun4Net.WebApi.nuspec"
    $build_dir =                     "$root\build\"
    $build_dir2 =                    "$build_dir\Net2"
    $build_dir4 =                    "$build_dir\Net4"
    $build_dir_mvc =                 "$build_dir\Mvc"
    $build_dir_webapi =              "$build_dir\WebApi"
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
}

task Init -depends Clean {
    new-item $release_dir -itemType directory | Out-Null
}

task Package -depends Init {
    exec { nuget pack $nugetspec -OutputDirectory $release_dir }
    exec { nuget pack $nugetspec_signed -OutputDirectory $release_dir }
    exec { nuget pack $nugetspec_core -OutputDirectory $release_dir }
    exec { nuget pack $nugetspec_mvc -OutputDirectory $release_dir }
    exec { nuget pack $nugetspec_webapi -OutputDirectory $release_dir }
}

task Zip -depends Package {
    $release = Get-ChildItem $release_dir | Select-Object -f 1
    $nupkg_name = $release.Name
    $nupkg_name = $nupkg_name -replace "Mindscape.Raygun4Net.", "v"
    $version = $nupkg_name -replace ".nupkg", ""
    
    $outerfolder = $release_dir + $version
    $versionfolder = $outerfolder + "\" + $version
    $versionfolder2 = $versionfolder + "\Net2"
    $versionfolder4 = $versionfolder + "\Net4"
    $versionfoldermvc = $versionfolder + "\Mvc"
    $versionfolderwebapi = $versionfolder + "\WebApi"
    
    $signedfolder = $versionfolder + "\signed"
    $signedfolder2 = $signedfolder + "\Net2"
    $signedfolder4 = $signedfolder + "\Net4"
    
    new-item $versionfolder -itemType directory | Out-Null
    new-item $versionfolder2 -itemType directory | Out-Null
    new-item $versionfolder4 -itemType directory | Out-Null
    new-item $versionfoldermvc -itemType directory | Out-Null
    new-item $versionfolderwebapi -itemType directory | Out-Null
    
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
    # Xamarin.Mac
    copy-item $build_dir/Mindscape.Raygun4Net.Xamarin.Mac.dll $versionfolder
    copy-item $build_dir/Mindscape.Raygun4Net.Xamarin.Mac2Beta.dll $versionfolder
    # Windows Store
    copy-item $build_dir/Mindscape.Raygun4Net.WindowsStore.dll $versionfolder
    copy-item $build_dir/Mindscape.Raygun4Net.WindowsStore.pdb $versionfolder
    # .Net 2.0
    copy-item $build_dir2/Mindscape.Raygun4Net.dll $versionfolder2
    copy-item $build_dir2/Mindscape.Raygun4Net.pdb $versionfolder2
    # .Net 4.0
    copy-item $build_dir4/Mindscape.Raygun4Net.dll $versionfolder4
    copy-item $build_dir4/Mindscape.Raygun4Net.pdb $versionfolder4
    # .Net MVC
    copy-item $build_dir_mvc/Mindscape.Raygun4Net.Mvc.dll $versionfoldermvc
    copy-item $build_dir_mvc/Mindscape.Raygun4Net.Mvc.pdb $versionfoldermvc
    copy-item $build_dir_mvc/Mindscape.Raygun4Net.dll $versionfoldermvc
    copy-item $build_dir_mvc/Mindscape.Raygun4Net.pdb $versionfoldermvc
    # .Net WebApi
    copy-item $build_dir_webapi/Mindscape.Raygun4Net.WebApi.dll $versionfolderwebapi
    copy-item $build_dir_webapi/Mindscape.Raygun4Net.WebApi.pdb $versionfolderwebapi
    copy-item $build_dir_webapi/Mindscape.Raygun4Net.dll $versionfolderwebapi
    copy-item $build_dir_webapi/Mindscape.Raygun4Net.pdb $versionfolderwebapi
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