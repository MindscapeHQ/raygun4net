# Releasing Raygun4Net

This release process is currently under review: https://github.com/MindscapeHQ/raygun4net/issues/561

Raygun4Net is published on nuget.org under the [mindscapehq account](https://www.nuget.org/profiles/mindscapehq).

## Semantic versioning

This package follows semantic versioning.

Given a version number MAJOR.MINOR.PATCH (x.y.z), increment the:

- MAJOR version when you make incompatible changes
- MINOR version when you add functionality in a backward compatible manner
- PATCH version when you make backward compatible bug fixes

To learn more about semantic versioning check: https://semver.org/

## Versioning in Raygun4Net

There are 3 variables for building which represent the version numbering.

- `assemblyVersion`: The assembly version should be set to the major version, because of this it won't be prompted, it
needs to be updated when we do a major version release. When the version being released is `8.1.0` the assembly
version must stay as `8.0.0` and only when we do an API change or have a valid reason to increment the major version
will we change it to `9.0.0`

- `version`: Version is the current version for the release that is signed into the assembly, regardless of the Package
Version. i.e. `8.1.0`

- `packageVersion`: `PackageVersion` is used by NuGet, this should be the same as `version` except you can change it if
you need to, i.e. to include `-pre-1` to indicate that `8.1.0` is prerelease version 1. Enter in `8.1.0-pre-1` when
prompted.

## Preparing for release

To build releases, you will need access to our CD/CI pipeline.

### Building a release

Navigate to _Providers /.NET (Crash Reporting) / Release_. 

Then tap on "Run" and when prompted introduce the value for the `version` and `packageVersion`.

![image](https://github.com/user-attachments/assets/3f6a7138-a505-4494-836e-5848172498c2)

Once the process ends, the packages will be created.

### Publishing to NuGet

Download the built files to your machine.

Open NuGet.org and go to [Upload](https://www.nuget.org/packages/manage/upload).

Then upload the packages there.

### Update CHANGELOG.md

Add a new entry in the `CHANGELOG.md` file.

Obtain a list of changes using the following git command:

```
git log --pretty=format:"- %s (%as)"
```

Then create a PR with the change.

### Tag and create Github Release

Once the changelog has been updated, create a release tag as follows:

Go to https://github.com/MindscapeHQ/raygun4net/releases and create a new Release.

GitHub will create a tag for you, you don't need to create the tag manually.

You can also generate the release notes automatically.
