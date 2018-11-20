#!/bin/bash
rm -rf Build/Xamarin/Android
# Android
"/Applications/Visual Studio.app/Contents/MacOS/vstool" -v build "--target:Clean" "--configuration:Release" "Mindscape.Raygun4Net.Xamarin.Android.sln"
"/Applications/Visual Studio.app/Contents/MacOS/vstool" -v build "--configuration:Release" "Mindscape.Raygun4Net.Xamarin.Android.sln"
mkdir -p Build/Xamarin/Android
cp -v Mindscape.Raygun4Net.Xamarin.Android/bin/Release/*.dll Build/Xamarin/Android/
cp -v Mindscape.Raygun4Net.Xamarin.Android/bin/Release/*.nupkg Build/Xamarin/Android/