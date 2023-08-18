properties {
    $root = $psake.build_script_dir;
    $build_dir = "$root\build\";
    $version = "6.7.0";
    $is_pre_release = $true;
}

task default -depends Compile

task Clean {
    Remove-Item -Force -Recurse $build_dir\* -ErrorAction SilentlyContinue | Out-Null
}

task Compile -depends Clean {
    $release_version = $version;
    if ($is_pre_release) {
        $release_version = "$version-pre"
    }

    # Unsigned
    Write-Output "Building unsigned packages"
    build-package .\Mindscape.Raygun4Net.NetCore.Common\
    build-package .\Mindscape.Raygun4Net.NetCore\
    build-package .\Mindscape.Raygun4Net.AspNetCore\

    # Signed
    Write-Output "Building signed packages"
    build-package .\Mindscape.Raygun4Net.NetCore.Common\ Sign
    build-package .\Mindscape.Raygun4Net.NetCore\ Sign
    build-package .\Mindscape.Raygun4Net.AspNetCore\ Sign
}

function build-package {
    param (
        $package,
        $configuration = "Release"
    )
    
    dotnet pack $package --output $build_dir --configuration $configuration -p:FileVersion=$version -p:PackageVersion=$release_version -p:Version=$release_version -v quiet
}