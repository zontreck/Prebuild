#!/bin/bash

# Use the prebuild bootstrap to compile prebuild
./runprebuild.sh
dotnet build -c Release