#!/bin/bash
rm -rf Build/Xamarin/Mac
# Mac
"/Applications/Xamarin Studio.app/Contents/MacOS/mdtool" -v build "--target:Clean" "--configuration:Release" "Mindscape.Raygun4Net.Xamarin.Mac.sln"
"/Applications/Xamarin Studio.app/Contents/MacOS/mdtool" -v build "--configuration:Release" "Mindscape.Raygun4Net.Xamarin.Mac.sln"
mkdir -p Build/Xamarin/Mac
cp -v Mindscape.Raygun4Net.Xamarin.Mac.Unified/bin/Release/*.dll Build/Xamarin/Mac/
cp -v Mindscape.Raygun4Net.Xamarin.Mac.Unified/bin/Release/*.nupkg Build/Xamarin/Mac/