#!/bin/bash
rm -rf Build/Xamarin/iOS
# iOS
"/Applications/Visual Studio.app/Contents/MacOS/vstool" -v build "--target:Clean" "--configuration:Release|iPhone" "Mindscape.Raygun4Net.Xamarin.iOS.sln"
"/Applications/Visual Studio.app/Contents/MacOS/vstool" -v build "--configuration:Release|iPhone" "Mindscape.Raygun4Net.Xamarin.iOS.sln"
mkdir -p Build/Xamarin/iOS
cp -v Mindscape.Raygun4Net.Xamarin.iOS.Unified/bin/Release/*.dll Build/Xamarin/iOS/
cp -v Mindscape.Raygun4Net.Xamarin.iOS.Unified/bin/Release/*.nupkg Build/Xamarin/iOS/