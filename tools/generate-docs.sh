#!/usr/bin/env bash
# Generates the API reference (docs/) from the XML comments of the library, with DefaultDocumentation
# (a local tool, see dotnet-tools.json; the settings are in defaultdocumentation.json).
# CI runs it too, and fails if docs/ is not up to date.

set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."

dotnet build src/Promptuarium/Promptuarium.csproj -c Release -v quiet -nologo
dotnet tool restore > /dev/null
rm -rf docs
dotnet tool run defaultdocumentation -- --ConfigurationFilePath "$PWD/defaultdocumentation.json" --LogLevel Warning
