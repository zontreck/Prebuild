#!/bin/bash
dotnet bootstrap/prebuild.dll /target vs2022 /excludedir = "obj | bin" /file prebuild.xml
