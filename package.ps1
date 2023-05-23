properties {
    $root =                          $psake.build_script_dir
    $nuspec_net35 =                  "$root\Mindscape.Raygun4Net.nuspec"
    $nuspec_net35_signed =           "$root\Mindscape.Raygun4Net.signed.nuspec"
    $nuspec_core =                   "$root\Mindscape.Raygun4Net.Core.nuspec"
    $nuspec_core_signed =            "$root\Mindscape.Raygun4Net.Core.Signed.nuspec"
    $nuspec_mvc =                    "$root\Mindscape.Raygun4Net.Mvc.nuspec"
    $nuspec_mvc_signed =             "$root\Mindscape.Raygun4Net.Mvc.Signed.nuspec"
    $nuspec_webapi =                 "$root\Mindscape.Raygun4Net.WebApi.nuspec"
    $nuspec_webapi_signed =          "$root\Mindscape.Raygun4Net.WebApi.Signed.nuspec"
    $nuspec_webjob =                 "$root\Mindscape.Raygun4Net.Azure.WebJob.nuspec"
    $build_dir =                     "$root\build\"
    $build_dir_net4 =                "$build_dir\net40"
    $build_dir_net4_client =         "$build_dir\net40-client"
    $build_dir_mvc =                 "$build_dir\mvc"
    $build_dir_webapi =              "$build_dir\webapi"
    $build_dir_webjob =              "$build_dir\webjob"
    $build_dir_winrt =               "$build_dir\winrt"
    $build_dir_uwp =                 "$build_dir\uwp"
    $build_dir_signed =              "$build_dir\signed"
    $build_dir_net4_signed =         "$build_dir_signed\net40"
    $build_dir_net4_client_signed =  "$build_dir_signed\net40-client"
    $build_dir_mvc_signed =          "$build_dir_signed\mvc"
    $build_dir_webapi_signed =       "$build_dir_signed\webapi"
    $build_dir_winrt_signed =        "$build_dir_signed\winrt"
    $build_dir_uwp_signed =          "$build_dir_signed\uwp"
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
    exec { nuget pack $nuspec_core -OutputDirectory $release_dir }
    exec { nuget pack $nuspec_core_signed -OutputDirectory $release_dir }
    exec { nuget pack $nuspec_mvc -OutputDirectory $release_dir }
    exec { nuget pack $nuspec_mvc_signed -OutputDirectory $release_dir }
    exec { nuget pack $nuspec_webapi -OutputDirectory $release_dir }
    exec { nuget pack $nuspec_webapi_signed -OutputDirectory $release_dir }
    exec { nuget pack $nuspec_webjob -OutputDirectory $release_dir }
}

task Zip -depends Package {
    $release = Get-ChildItem $release_dir | Select-Object -f 1
    $nupkg_name = $release.Name
    $nupkg_name = $nupkg_name -replace "Mindscape.Raygun4Net.", "v"
    $version = $nupkg_name -replace ".nupkg", ""
    
    $outerfolder =               $release_dir + $version
    $versionfolder =             $outerfolder + "\" + $version
    $versionfolder4 =            $versionfolder + "\net40"
    $versionfolder4client =      $versionfolder + "\net40-client"
    $versionfoldermvc =          $versionfolder + "\mvc"
    $versionfolderwebapi =       $versionfolder + "\webapi"
    $versionfolderwebjob =       $versionfolder + "\webjob"
    $versionfolderwinrt =        $versionfolder + "\winrt"
    $versionfolderuwp =          $versionfolder + "\uwp"
    $versionfolderwindowsphone = $versionfolder + "\windowsphone"
    
    $signedfolder =         $versionfolder + "\signed"
    $signedfolder4 =        $signedfolder + "\net40"
    $signedfolder4client =  $signedfolder + "\net40-client"
    $signedfoldermvc =      $signedfolder + "\mvc"
    $signedfolderwebapi =   $signedfolder + "\webapi"
    $signedfolderwinrt =    $signedfolder + "\winrt"
    $signedfolderuwp =      $signedfolder + "\uwp"
    
    new-item $versionfolder -itemType directory | Out-Null
    new-item $versionfolder2 -itemType directory | Out-Null
    new-item $versionfolder35 -itemType directory | Out-Null
    new-item $versionfolder35client -itemType directory | Out-Null
    new-item $versionfolder4 -itemType directory | Out-Null
    new-item $versionfolder4client -itemType directory | Out-Null
    new-item $versionfoldermvc -itemType directory | Out-Null
    new-item $versionfolderwebapi -itemType directory | Out-Null
    new-item $versionfolderwebjob -itemType directory | Out-Null
    new-item $versionfolderwinrt -itemType directory | Out-Null
    new-item $versionfolderuwp -itemType directory | Out-Null
    new-item $versionfolderwindowsphone -itemType directory | Out-Null
    
    new-item $signedfolder -itemType directory | Out-Null
    new-item $signedfolder2 -itemType directory | Out-Null
    new-item $signedfolder35 -itemType directory | Out-Null
    new-item $signedfolder35client -itemType directory | Out-Null
    new-item $signedfolder4 -itemType directory | Out-Null
    new-item $signedfolder4client -itemType directory | Out-Null
    new-item $signedfoldermvc -itemType directory | Out-Null
    new-item $signedfolderwebapi -itemType directory | Out-Null
    new-item $signedfolderwinrt -itemType directory | Out-Null
    new-item $signedfolderuwp -itemType directory | Out-Null
        
    # .NET 4.0
    copy-item $build_dir_net4/Mindscape.Raygun4Net.dll $versionfolder4
    copy-item $build_dir_net4/Mindscape.Raygun4Net.pdb $versionfolder4
    copy-item $build_dir_net4/Mindscape.Raygun4Net4.dll $versionfolder4
    copy-item $build_dir_net4/Mindscape.Raygun4Net4.pdb $versionfolder4
    # .NET 4.0 Client Profile
    copy-item $build_dir_net4_client/Mindscape.Raygun4Net.dll $versionfolder4client
    copy-item $build_dir_net4_client/Mindscape.Raygun4Net.pdb $versionfolder4client
    # .NET MVC
    copy-item $build_dir_mvc/Mindscape.Raygun4Net.dll $versionfoldermvc
    copy-item $build_dir_mvc/Mindscape.Raygun4Net.pdb $versionfoldermvc
    copy-item $build_dir_mvc/Mindscape.Raygun4Net.Mvc.dll $versionfoldermvc
    copy-item $build_dir_mvc/Mindscape.Raygun4Net.Mvc.pdb $versionfoldermvc
    copy-item $build_dir_mvc/Mindscape.Raygun4Net4.dll $versionfoldermvc
    copy-item $build_dir_mvc/Mindscape.Raygun4Net4.pdb $versionfoldermvc
    # .NET WebApi
    copy-item $build_dir_webapi/Mindscape.Raygun4Net.WebApi.dll $versionfolderwebapi
    copy-item $build_dir_webapi/Mindscape.Raygun4Net.WebApi.pdb $versionfolderwebapi
    copy-item $build_dir_webapi/Mindscape.Raygun4Net.dll $versionfolderwebapi
    copy-item $build_dir_webapi/Mindscape.Raygun4Net.pdb $versionfolderwebapi
    # Azure WebJob
    copy-item $build_dir_webjob/Mindscape.Raygun4Net.dll $versionfolderwebjob
    copy-item $build_dir_webjob/Mindscape.Raygun4Net.pdb $versionfolderwebjob
    copy-item $build_dir_webjob/Mindscape.Raygun4Net.Azure.WebJob.dll $versionfolderwebjob
    copy-item $build_dir_webjob/Mindscape.Raygun4Net.Azure.WebJob.pdb $versionfolderwebjob
    # WinRT
    copy-item $build_dir_winrt/Mindscape.Raygun4Net.WinRT.dll $versionfolderwinrt
    copy-item $build_dir_winrt/Mindscape.Raygun4Net.WinRT.pdb $versionfolderwinrt
    # Windows Store
    copy-item $build_dir_uwp/Mindscape.Raygun4Net.WindowsStore.dll $versionfolderuwp
    copy-item $build_dir_uwp/Mindscape.Raygun4Net.WindowsStore.pdb $versionfolderuwp
    # Signed .NET 4
    copy-item $build_dir_net4_signed/Mindscape.Raygun4Net.dll $signedfolder4
    copy-item $build_dir_net4_signed/Mindscape.Raygun4Net.pdb $signedfolder4
    copy-item $build_dir_net4_signed/Mindscape.Raygun4Net4.dll $signedfolder4
    copy-item $build_dir_net4_signed/Mindscape.Raygun4Net4.pdb $signedfolder4
    # Signed .NET 4 Client Profile
    copy-item $build_dir_net4_client_signed/Mindscape.Raygun4Net.dll $signedfolder4client
    copy-item $build_dir_net4_client_signed/Mindscape.Raygun4Net.pdb $signedfolder4client
    # Signed MVC
    copy-item $build_dir_mvc_signed/Mindscape.Raygun4Net.dll $signedfoldermvc
    copy-item $build_dir_mvc_signed/Mindscape.Raygun4Net.pdb $signedfoldermvc
    copy-item $build_dir_mvc_signed/Mindscape.Raygun4Net.Mvc.dll $signedfoldermvc
    copy-item $build_dir_mvc_signed/Mindscape.Raygun4Net.Mvc.pdb $signedfoldermvc
    copy-item $build_dir_mvc_signed/Mindscape.Raygun4Net4.dll $signedfoldermvc
    copy-item $build_dir_mvc_signed/Mindscape.Raygun4Net4.pdb $signedfoldermvc
    #Signed WebApi
    copy-item $build_dir_webapi_signed/Mindscape.Raygun4Net.WebApi.dll $signedfolderwebapi
    copy-item $build_dir_webapi_signed/Mindscape.Raygun4Net.WebApi.pdb $signedfolderwebapi
    copy-item $build_dir_webapi_signed/Mindscape.Raygun4Net.dll $signedfolderwebapi
    copy-item $build_dir_webapi_signed/Mindscape.Raygun4Net.pdb $signedfolderwebapi
    # Signed WinRT
    copy-item $build_dir_winrt_signed/Mindscape.Raygun4Net.WinRT.dll $signedfolderwinrt
    copy-item $build_dir_winrt_signed/Mindscape.Raygun4Net.WinRT.pdb $signedfolderwinrt
    # Signed UWP
    copy-item $build_dir_uwp_signed/Mindscape.Raygun4Net.WindowsStore.dll $signedfolderuwp
    
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