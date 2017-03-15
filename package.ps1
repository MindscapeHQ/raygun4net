properties {
    $root =                             $psake.build_script_dir
    $nugetspec =                        "$root\Mindscape.Raygun4Net.nuspec"
    $nugetspec_signed =                 "$root\Mindscape.Raygun4Net.signed.nuspec"
    $nugetspec_core =                   "$root\Mindscape.Raygun4Net.Core.nuspec"
    $nugetspec_mvc =                    "$root\Mindscape.Raygun4Net.Mvc.nuspec"
    $nugetspec_webjob =                 "$root\Mindscape.Raygun4Net.Azure.WebJob.nuspec"
    $nugetspec_webapi =                 "$root\Mindscape.Raygun4Net.WebApi.nuspec"
    $nugetspec_signed_core =            "$root\Mindscape.Raygun4Net.Core.Signed.nuspec"
    $nugetspec_signed_mvc =             "$root\Mindscape.Raygun4Net.Mvc.Signed.nuspec"
    $nugetspec_signed_webapi =          "$root\Mindscape.Raygun4Net.WebApi.Signed.nuspec"
    $build_dir =                        "$root\build\"
    $build_dir2 =                       "$build_dir\Net2"
    $build_dir3_client_profile =        "$build_dir\Net3.ClientProfile"
    $build_dir4 =                       "$build_dir\Net4"
    $build_dir4_client_profile =        "$build_dir\Net4.ClientProfile"
    $build_dir_mvc =                    "$build_dir\Mvc"
    $build_dir_webjob =                 "$build_dir\WebJob"
    $build_dir_webapi =                 "$build_dir\WebApi"
    $signed_build_dir =                 "$build_dir\signed"
    $signed_build_dir2 =                "$build_dir\signed\Net2"
    $signed_build_dir3_client_profile = "$build_dir\signed\Net3.ClientProfile"
    $signed_build_dir4 =                "$build_dir\signed\Net4"
    $signed_build_dir4_client_profile = "$build_dir\signed\Net4.ClientProfile"
    $signed_build_dir_mvc =             "$build_dir\signed\Mvc"
    $signed_build_dir_webapi =          "$build_dir\signed\WebApi"
    $release_dir =                      "$root\release\"
    $nuget_dir =                        "$root\.nuget"
    $env:Path +=                        ";$nuget_dir"
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
    exec { nuget pack $nugetspec_webjob -OutputDirectory $release_dir }
    exec { nuget pack $nugetspec_webapi -OutputDirectory $release_dir }
    exec { nuget pack $nugetspec_signed_core -OutputDirectory $release_dir }
    exec { nuget pack $nugetspec_signed_mvc -OutputDirectory $release_dir }
    exec { nuget pack $nugetspec_signed_webapi -OutputDirectory $release_dir }
}

task Zip -depends Package {
    $release = Get-ChildItem $release_dir | Select-Object -f 1
    $nupkg_name = $release.Name
    $nupkg_name = $nupkg_name -replace "Mindscape.Raygun4Net.", "v"
    $version = $nupkg_name -replace ".nupkg", ""
    
    $outerfolder = $release_dir + $version
    $versionfolder = $outerfolder + "\" + $version
    $versionfolder2 = $versionfolder + "\Net2"
    $versionfolder3 = $versionfolder + "\Net3"
    $versionfolder3clientprofile = $versionfolder + "\Net3.ClientProfile"
    $versionfolder4 = $versionfolder + "\Net4"
    $versionfolder4clientprofile = $versionfolder + "\Net4.ClientProfile"
    $versionfoldermvc = $versionfolder + "\Mvc"
    $versionfolderwebjob = $versionfolder + "\WebJob"
    $versionfolderwebapi = $versionfolder + "\WebApi"
    $versionfolderwindowsphone = $versionfolder + "\WindowsPhone"
    $versionfolderwindowsstore = $versionfolder + "\WindowsStore"
    $versionfolderwinrt = $versionfolder + "\WinRT"
    $versionfolderxamarinandroid = $versionfolder + "\Xamarin.Android"
    $versionfolderxamarinios = $versionfolder + "\Xamarin.iOS"
    $versionfolderxamarinmac = $versionfolder + "\Xamarin.Mac"
    
    $signedfolder = $versionfolder + "\signed"
    $signedfolder2 = $signedfolder + "\Net2"
    $signedfolder3 = $signedfolder + "\Net3"
    $signedfolder3clientprofile = $signedfolder + "\Net3.ClientProfile"
    $signedfolder4 = $signedfolder + "\Net4"
    $signedfolder4clientprofile = $signedfolder + "\Net4.ClientProfile"
    $signedfolderwindowsstore = $signedfolder + "\WindowsStore"
    $signedfolderwinrt = $signedfolder + "\WinRT"
    $signedfoldermvc = $signedfolder + "\Mvc"
    $signedfolderwebapi = $signedfolder + "\WebApi"
    
    new-item $versionfolder -itemType directory | Out-Null
    new-item $versionfolder2 -itemType directory | Out-Null
    new-item $versionfolder3 -itemType directory | Out-Null
    new-item $versionfolder3clientprofile -itemType directory | Out-Null
    new-item $versionfolder4 -itemType directory | Out-Null
    new-item $versionfolder4clientprofile -itemType directory | Out-Null
    new-item $versionfoldermvc -itemType directory | Out-Null
    new-item $versionfolderwebjob -itemType directory | Out-Null
    new-item $versionfolderwebapi -itemType directory | Out-Null
    new-item $versionfolderwindowsphone -itemType directory | Out-Null
    new-item $versionfolderwindowsstore -itemType directory | Out-Null
    new-item $versionfolderwinrt -itemType directory | Out-Null
    new-item $versionfolderxamarinandroid -itemType directory | Out-Null
    new-item $versionfolderxamarinios -itemType directory | Out-Null
    new-item $versionfolderxamarinmac -itemType directory | Out-Null
    
    new-item $signedfolder -itemType directory | Out-Null
    new-item $signedfolder2 -itemType directory | Out-Null
    new-item $signedfolder3 -itemType directory | Out-Null
    new-item $signedfolder3clientprofile -itemType directory | Out-Null
    new-item $signedfolder4 -itemType directory | Out-Null
    new-item $signedfolder4clientprofile -itemType directory | Out-Null
    new-item $signedfolderwindowsstore -itemType directory | Out-Null
    new-item $signedfolderwinrt -itemType directory | Out-Null
    new-item $signedfoldermvc -itemType directory | Out-Null
    new-item $signedfolderwebapi -itemType directory | Out-Null
  
    # .Net 3.5
    copy-item $build_dir/Mindscape.Raygun4Net.dll $versionfolder3
    copy-item $build_dir/Mindscape.Raygun4Net.pdb $versionfolder3
    # Windows Phone
    copy-item $build_dir/Mindscape.Raygun4Net.WindowsPhone.dll $versionfolderwindowsphone
    copy-item $build_dir/Mindscape.Raygun4Net.WindowsPhone.pdb $versionfolderwindowsphone
    # WinRT
    copy-item $build_dir/Mindscape.Raygun4Net.WinRT.dll $versionfolderwinrt
    copy-item $build_dir/Mindscape.Raygun4Net.WinRT.pdb $versionfolderwinrt
    # Xamarin.Android
    copy-item $build_dir/Mindscape.Raygun4Net.Xamarin.Android.dll $versionfolderxamarinandroid
    copy-item $build_dir/Mindscape.Raygun4Net.Xamarin.Android.pdb $versionfolderxamarinandroid
    # Xamarin.iOS
    copy-item $build_dir/Mindscape.Raygun4Net.Xamarin.iOS.dll $versionfolderxamarinios
    copy-item $build_dir/Mindscape.Raygun4Net.Xamarin.iOS.Unified.dll $versionfolderxamarinios
    # Xamarin.Mac
    copy-item $build_dir/Mindscape.Raygun4Net.Xamarin.Mac.dll $versionfolderxamarinmac
    copy-item $build_dir/Mindscape.Raygun4Net.Xamarin.Mac.Unified.dll $versionfolderxamarinmac
    # Windows Store
    copy-item $build_dir/Mindscape.Raygun4Net.WindowsStore.dll $versionfolderwindowsstore
    copy-item $build_dir/Mindscape.Raygun4Net.WindowsStore.pdb $versionfolderwindowsstore
    # .Net 2.0
    copy-item $build_dir2/Mindscape.Raygun4Net.dll $versionfolder2
    copy-item $build_dir2/Mindscape.Raygun4Net.pdb $versionfolder2
    # .Net 3.5 Client Profile
    copy-item $build_dir3_client_profile/Mindscape.Raygun4Net.dll $versionfolder3clientprofile
    copy-item $build_dir3_client_profile/Mindscape.Raygun4Net.pdb $versionfolder3clientprofile
    # .Net 4.0
    copy-item $build_dir4/Mindscape.Raygun4Net.dll $versionfolder4
    copy-item $build_dir4/Mindscape.Raygun4Net.pdb $versionfolder4
    copy-item $build_dir4/Mindscape.Raygun4Net4.dll $versionfolder4
    copy-item $build_dir4/Mindscape.Raygun4Net4.pdb $versionfolder4
    # .Net 4.0 Client Profile
    copy-item $build_dir4_client_profile/Mindscape.Raygun4Net.dll $versionfolder4clientprofile
    copy-item $build_dir4_client_profile/Mindscape.Raygun4Net.pdb $versionfolder4clientprofile
    # .Net MVC
    copy-item $build_dir_mvc/Mindscape.Raygun4Net.dll $versionfoldermvc
    copy-item $build_dir_mvc/Mindscape.Raygun4Net.pdb $versionfoldermvc
    copy-item $build_dir_mvc/Mindscape.Raygun4Net.Mvc.dll $versionfoldermvc
    copy-item $build_dir_mvc/Mindscape.Raygun4Net.Mvc.pdb $versionfoldermvc
    copy-item $build_dir_mvc/Mindscape.Raygun4Net4.dll $versionfoldermvc
    copy-item $build_dir_mvc/Mindscape.Raygun4Net4.pdb $versionfoldermvc
    # Azure WebJob
    copy-item $build_dir_webjob/Mindscape.Raygun4Net.dll $versionfolderwebjob
    copy-item $build_dir_webjob/Mindscape.Raygun4Net.pdb $versionfolderwebjob
    copy-item $build_dir_webjob/Mindscape.Raygun4Net.Azure.WebJob.dll $versionfolderwebjob
    copy-item $build_dir_webjob/Mindscape.Raygun4Net.Azure.WebJob.pdb $versionfolderwebjob
    # .Net WebApi
    copy-item $build_dir_webapi/Mindscape.Raygun4Net.WebApi.dll $versionfolderwebapi
    copy-item $build_dir_webapi/Mindscape.Raygun4Net.WebApi.pdb $versionfolderwebapi
    copy-item $build_dir_webapi/Mindscape.Raygun4Net.dll $versionfolderwebapi
    copy-item $build_dir_webapi/Mindscape.Raygun4Net.pdb $versionfolderwebapi
    # Signed
    copy-item $signed_build_dir/Mindscape.Raygun4Net.dll $signedfolder3
    copy-item $signed_build_dir/Mindscape.Raygun4Net.WinRT.dll $signedfolderwinrt
    copy-item $signed_build_dir/Mindscape.Raygun4Net.WindowsStore.dll $signedfolderwindowsstore
    copy-item $signed_build_dir2/Mindscape.Raygun4Net.dll $signedfolder2
    copy-item $signed_build_dir3_client_profile/Mindscape.Raygun4Net.dll $signedfolder3clientprofile
    copy-item $signed_build_dir4/Mindscape.Raygun4Net.dll $signedfolder4
    copy-item $signed_build_dir4/Mindscape.Raygun4Net4.dll $signedfolder4
    copy-item $signed_build_dir4_client_profile/Mindscape.Raygun4Net.dll $signedfolder4clientprofile
    # Signed MVC
    copy-item $signed_build_dir_mvc/Mindscape.Raygun4Net.dll $signedfoldermvc
    copy-item $signed_build_dir_mvc/Mindscape.Raygun4Net.pdb $signedfoldermvc
    copy-item $signed_build_dir_mvc/Mindscape.Raygun4Net.Mvc.dll $signedfoldermvc
    copy-item $signed_build_dir_mvc/Mindscape.Raygun4Net.Mvc.pdb $signedfoldermvc
    copy-item $signed_build_dir_mvc/Mindscape.Raygun4Net4.dll $signedfoldermvc
    copy-item $signed_build_dir_mvc/Mindscape.Raygun4Net4.pdb $signedfoldermvc
    #Signed WebApi
    copy-item $signed_build_dir_webapi/Mindscape.Raygun4Net.WebApi.dll $signedfolderwebapi
    copy-item $signed_build_dir_webapi/Mindscape.Raygun4Net.WebApi.pdb $signedfolderwebapi
    copy-item $signed_build_dir_webapi/Mindscape.Raygun4Net.dll $signedfolderwebapi
    copy-item $signed_build_dir_webapi/Mindscape.Raygun4Net.pdb $signedfolderwebapi
	
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