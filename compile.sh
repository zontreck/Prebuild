#!/bin/bash

# Use the prebuild bootstrap to compile prebuild
chmod +x runprebuild.sh
./runprebuild.sh
dotnet build -c Release