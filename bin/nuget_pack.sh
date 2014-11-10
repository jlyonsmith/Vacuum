#!/bin/bash
rm *.nupkg
mono ~/lib/NuGet/NuGet.exe pack Vacuum.nuspec
