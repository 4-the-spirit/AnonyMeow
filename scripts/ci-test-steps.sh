#!/usr/bin/env bash
# Runs inside the mcr.microsoft.com/dotnet/sdk:10.0 container launched by test-like-ci.ps1.
# Mirrors the restore/build/test steps in .github/workflows/backend-ci.yml.
set -e

dotnet restore "AnonyMeow.sln"
dotnet build "AnonyMeow.sln" --no-restore -c Release

# `dotnet test` against the .sln silently exits 0 with zero output in this SDK container
# (a reproducible quirk of the container image, not a code issue) -- `dotnet vstest` on the
# built assemblies is a verified working equivalent.
dotnet vstest "Tests/AnonyMeow.UnitTests/bin/Release/net10.0/AnonyMeow.UnitTests.dll"
dotnet vstest "Tests/AnonyMeow.IntegrationTests/bin/Release/net10.0/AnonyMeow.IntegrationTests.dll"
