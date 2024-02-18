
$root = Get-Location
$build_dir = "$root\build"
$assembly_version = "9.0.5"
$version = "9.0.5"
$is_pre_release = $true
$pre_release_counter = 9

function CleanAndBuild {
    Remove-Item -Force -Recurse $build_dir -ErrorAction SilentlyContinue | Out-Null

    if (-not (Test-Path $build_dir)) {
        New-Item -ItemType Directory -Path $build_dir | Out-Null
    }

    & dotnet clean $root\Raygun.CrashReporting.sln -c Release
    & dotnet build $root\Raygun.CrashReporting.sln -c Release -p:FileVersion=$version -p:Version=$version -p:assembly_version=$assembly_version
}

function PackAll {

    $package_version = $version
    if ($is_pre_release) {
        $package_version = "$version-pre-$pre_release_counter"
    }

    # Pack .NET SDK Projects
    PackProject -project_path "$root\Mindscape.Raygun4Net.AspNetCore\Mindscape.Raygun4Net.AspNetCore.csproj" -package_version $package_version
    PackProject -project_path "$root\Mindscape.Raygun4Net.NetCore\Mindscape.Raygun4Net.NetCore.csproj" -package_version $package_version
    PackProject -project_path "$root\Mindscape.Raygun4Net.NetCore.Common\Mindscape.Raygun4Net.NetCore.Common.csproj" -package_version $package_version
    PackProject -project_path "$root\Mindscape.Raygun4Net.Core\Mindscape.Raygun4Net.Core.csproj" -package_version $package_version
    PackProject -project_path "$root\Mindscape.Raygun4Net.Mvc\Mindscape.Raygun4Net.Mvc.csproj" -package_version $package_version
    PackProject -project_path "$root\Mindscape.Raygun4Net.WebApi\Mindscape.Raygun4Net.WebApi.csproj" -package_version $package_version
    PackProject -project_path "$root\Mindscape.Raygun4Net4\Mindscape.Raygun4Net4.csproj" -package_version $package_version
    PackProject -project_path "$root\Mindscape.Raygun4Net.Azure.WebJob\Mindscape.Raygun4Net.Azure.WebJob.csproj" -package_version $package_version

    # count the number of packages produced and write out
    $packageCount = (Get-ChildItem -Path $build_dir -Filter *.nupkg).Count
    Write-Output "Created $packageCount packages"
    Write-Output "Version: $package_version"

}

function PackProject([string]$project_path, [string]$package_version) {
    Write-Output "Packing $project_path"
    & dotnet pack $project_path -c Release -o $build_dir -p:PackageVersion=$package_version -p:FileVersion=$version -p:Version=$version -p:assembly_version=$assembly_version
}

& CleanAndBuild
& PackAll