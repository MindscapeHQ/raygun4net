#!/bin/bash
rm -rf Build
# iOS
"/Applications/Xamarin Studio.app/Contents/MacOS/mdtool" -v build "--target:Clean" "--configuration:Release|iPhone" "Mindscape.Raygun4Net.Xamarin.iOS.sln"
"/Applications/Xamarin Studio.app/Contents/MacOS/mdtool" -v build "--configuration:Release|iPhone" "Mindscape.Raygun4Net.Xamarin.iOS.sln"
mkdir Build
cp -v Mindscape.Raygun4Net.Xamarin.iOS/bin/Release/Mindscape.Raygun4Net.Xamarin.iOS.dll Build/Mindscape.Raygun4Net.Xamarin.iOS.dll
cp -v Mindscape.Raygun4Net.Xamarin.iOS.Unified/bin/Release/Mindscape.Raygun4Net.Xamarin.iOS.Unified.dll Build/Mindscape.Raygun4Net.Xamarin.iOS.Unified.dll
# Mac
"/Applications/Xamarin Studio.app/Contents/MacOS/mdtool" -v build "--target:Clean" "--configuration:Release" "Mindscape.Raygun4Net.Xamarin.Mac.sln"
"/Applications/Xamarin Studio.app/Contents/MacOS/mdtool" -v build "--configuration:Release" "Mindscape.Raygun4Net.Xamarin.Mac.sln"
cp -v Mindscape.Raygun4Net.Xamarin.Mac/bin/Release/Mindscape.Raygun4Net.Xamarin.Mac.dll Build/Mindscape.Raygun4Net.Xamarin.Mac.dll
cp -v Mindscape.Raygun4Net.Xamarin.Mac.Unified/bin/Release/Mindscape.Raygun4Net.Xamarin.Mac.Unified.dll Build/Mindscape.Raygun4Net.Xamarin.Mac.Unified.dll