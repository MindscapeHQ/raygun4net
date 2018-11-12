#!/bin/bash
rm -rf Build/Xamarin/iOS
# iOS
"/Applications/Xamarin Studio.app/Contents/MacOS/mdtool" -v build "--target:Clean" "--configuration:Release|iPhone" "Mindscape.Raygun4Net.Xamarin.iOS.sln"
"/Applications/Xamarin Studio.app/Contents/MacOS/mdtool" -v build "--configuration:Release|iPhone" "Mindscape.Raygun4Net.Xamarin.iOS.sln"
mkdir -p Build/Xamarin/iOS
cp -v Mindscape.Raygun4Net.Xamarin.iOS.Unified/bin/Release/Mindscape.Raygun4Net.Xamarin.iOS.Unified.dll Build/Xamarin/iOS/Mindscape.Raygun4Net.Xamarin.iOS.Unified.dll