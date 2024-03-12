#!/bin/sh

# Set variables
root=$(pwd)
build_dir="$root/build"
assembly_version="9.0.0"
version="9.0.1"
is_pre_release=true
pre_release_counter=1

# Define CleanAndBuild function
CleanAndBuild() {
    rm -rf $build_dir 2> /dev/null

    if [ ! -d "$build_dir" ]; then
        mkdir -p $build_dir
    fi

    dotnet clean $root/Raygun.CrashReporting.sln -c Release
    dotnet build $root/Raygun.CrashReporting.sln -c Release -p:FileVersion=$version -p:Version=$version -p:assembly_version=$assembly_version
}

# Define PackAll function
PackAll() {
    package_version=$version
    if $is_pre_release; then
        package_version="${version}-pre-${pre_release_counter}"
    fi

    # Pack .NET SDK Projects
    PackProject "$root/Mindscape.Raygun4Net.AspNetCore/Mindscape.Raygun4Net.AspNetCore.csproj" $package_version
    PackProject "$root/Mindscape.Raygun4Net.NetCore/Mindscape.Raygun4Net.NetCore.csproj" $package_version
    PackProject "$root/Mindscape.Raygun4Net.NetCore.Common/Mindscape.Raygun4Net.NetCore.Common.csproj" $package_version
    PackProject "$root/Mindscape.Raygun4Net.Core/Mindscape.Raygun4Net.Core.csproj" $package_version
    PackProject "$root/Mindscape.Raygun4Net.Mvc/Mindscape.Raygun4Net.Mvc.csproj" $package_version
    PackProject "$root/Mindscape.Raygun4Net.WebApi/Mindscape.Raygun4Net.WebApi.csproj" $package_version
    PackProject "$root/Mindscape.Raygun4Net4/Mindscape.Raygun4Net4.csproj" $package_version
    PackProject "$root/Mindscape.Raygun4Net.Azure.WebJob/Mindscape.Raygun4Net.Azure.WebJob.csproj" $package_version

    # Count the number of packages produced and write out
    packageCount=$(find $build_dir -name '*.nupkg' | wc -l)
    echo "Created $packageCount packages"
}

# Define PackProject function
PackProject() {
    project_path=$1
    package_version=$2
    echo "Packing $project_path"
    dotnet pack $project_path -c Release -o $build_dir -p:PackageVersion=$package_version -p:FileVersion=$version -p:Version=$version -p:assembly_version=$assembly_version
}

# Execute functions
CleanAndBuild
PackAll
