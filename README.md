Raygun4Net
==========

Raygun.io Plugin for .NET Framework


Installation & Usage
====================

Reference the "Mindscape.Raygun4Net.dll"

Add a section to configSections:

```
<section name="RaygunSettings" type="Mindscape.Raygun4Net.RaygunSettings, Mindscape.Raygun4Net"/>
```

Add the Raygun settings configuration block from above:

```
<RaygunSettings apikey="{{apikey for your application}}" />
```

You can then either use the RaygunClient class directly to pass exceptions to Raygun or there is an HttpModule you can add.

```
<modules>
  <add name="RaygunErrorModule" type="Mindscape.Raygun4Net.RaygunHttpModule"/>
</modules>
```
