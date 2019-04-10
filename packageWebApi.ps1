properties {
    $root =             $psake.build_script_dir
    $nuget_dir =        "$root\.nuget"
    $release_dir =      "$root\release\"
    $build_dir =        "$root\build\webapi"
    $build_dir_signed = "$root\build\signed\webapi"
    $nuspec =           "$root\Mindscape.Raygun4Net.WebApi.nuspec"
    $nuspec_signed =    "$root\Mindscape.Raygun4Net.WebApi.Signed.nuspec"
    $env:Path +=        ";$nuget_dir"
}

task default -depends Zip

task Clean {
    remove-item -force -recurse $release_dir -ErrorAction SilentlyContinue | Out-Null
}

task Init -depends Clean {
    new-item $release_dir -itemType directory | Out-Null
}

task Package -depends Init {
    exec { nuget pack $nuspec -OutputDirectory $release_dir }
    exec { nuget pack $nuspec_signed -OutputDirectory $release_dir }
}

task Zip -depends Package {
    $release =    Get-ChildItem $release_dir | Select-Object -f 1
    $nupkg_name = $release.Name
    $nupkg_name = $nupkg_name -replace "Mindscape.Raygun4Net.", "v"
    $version =    $nupkg_name -replace ".nupkg", ""
    
    $outerfolder =         $release_dir + $version
    $versionfolder =       $outerfolder + "\" + $version
    $versionfolderwebapi = $versionfolder + "\webapi"
    $signedfolder =        $versionfolder + "\signed"
    $signedfolderwebapi =  $signedfolder + "\webapi"
    
    new-item $versionfolder -itemType directory | Out-Null
    new-item $versionfolderwebapi -itemType directory | Out-Null
    new-item $signedfolder -itemType directory | Out-Null
    new-item $signedfolderwebapi -itemType directory | Out-Null
  
    # .NET Web API
    copy-item $build_dir/Mindscape.Raygun4Net.WebApi.dll $versionfolderwebapi
    copy-item $build_dir/Mindscape.Raygun4Net.WebApi.pdb $versionfolderwebapi
    copy-item $build_dir/Mindscape.Raygun4Net.dll $versionfolderwebapi
    copy-item $build_dir/Mindscape.Raygun4Net.pdb $versionfolderwebapi
    
    # Signed .NET Web API
    copy-item $build_dir_signed/Mindscape.Raygun4Net.WebApi.dll $signedfolderwebapi
    copy-item $build_dir_signed/Mindscape.Raygun4Net.WebApi.pdb $signedfolderwebapi
    copy-item $build_dir_signed/Mindscape.Raygun4Net.dll $signedfolderwebapi
    copy-item $build_dir_signed/Mindscape.Raygun4Net.pdb $signedfolderwebapi
    
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