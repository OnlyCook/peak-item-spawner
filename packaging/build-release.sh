#!/usr/bin/env bash
#
# Assemble the Nexus Mods release zip. This mod ships on Nexus only.
#
# The version in src/ItemSpawnerPlus/PluginInfo.cs is the single source of truth,
# this script syncs it into the .csproj before building (CHANGELOG.md stays
# hand-maintained, this only warns if the new version has no entry yet).
#
# Output: dist/ItemSpawnerPlus-<version>.zip, everything nested one level under an
# OnlyCook-ItemSpawnerPlus/ folder so extracting it straight into BepInEx/plugins/
# lands correctly for a manual install:
#   OnlyCook-ItemSpawnerPlus/ItemSpawnerPlus.dll
#   OnlyCook-ItemSpawnerPlus/README.md
#   OnlyCook-ItemSpawnerPlus/CHANGELOG.md
#   OnlyCook-ItemSpawnerPlus/LICENSE
#   OnlyCook-ItemSpawnerPlus/NOTICE
#
# Usage:  bash packaging/build-release.sh
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PKG="$REPO_ROOT/packaging"
PROJ="$REPO_ROOT/src/ItemSpawnerPlus"
DIST="$REPO_ROOT/dist"
FOLDER="OnlyCook-ItemSpawnerPlus"

VERSION="$(grep -oE 'Version = "[0-9]+\.[0-9]+\.[0-9]+"' "$PROJ/PluginInfo.cs" | grep -oE '[0-9]+\.[0-9]+\.[0-9]+')"
if [[ -z "$VERSION" ]]; then echo "ERROR: could not read Version from PluginInfo.cs" >&2; exit 1; fi
echo "Packaging ItemSpawnerPlus v$VERSION"

echo "Syncing version $VERSION into csproj..."
sed -i -E "s#(<Version>)[0-9]+\.[0-9]+\.[0-9]+(</Version>)#\1$VERSION\2#" \
  "$PROJ/ItemSpawnerPlus.csproj"

if ! grep -q "^## $VERSION" "$PKG/CHANGELOG.md"; then
  echo "WARNING: packaging/CHANGELOG.md has no '## $VERSION' entry yet, add one before publishing." >&2
fi

# keep the repo-root README.md in sync with the packaged halves
bash "$PKG/gen-readme.sh"

echo "Building..."
dotnet build "$PROJ/ItemSpawnerPlus.csproj" -c Release >/dev/null
DLL="$PROJ/bin/Release/ItemSpawnerPlus.dll"
[[ -f "$DLL" ]] || { echo "ERROR: build output not found: $DLL" >&2; exit 1; }

STAGE="$(mktemp -d)"
trap 'rm -rf "$STAGE"' EXIT
mkdir -p "$STAGE/$FOLDER"
cp "$DLL" "$STAGE/$FOLDER/ItemSpawnerPlus.dll"
cp "$REPO_ROOT/README.md" "$STAGE/$FOLDER/README.md"
cp "$PKG/CHANGELOG.md" "$STAGE/$FOLDER/CHANGELOG.md"
cp "$REPO_ROOT/LICENSE" "$STAGE/$FOLDER/LICENSE"
[[ -f "$REPO_ROOT/NOTICE" ]] && cp "$REPO_ROOT/NOTICE" "$STAGE/$FOLDER/NOTICE"

mkdir -p "$DIST"
OUT="$DIST/ItemSpawnerPlus-$VERSION.zip"
rm -f "$OUT"
( cd "$STAGE" && zip -r -q "$OUT" . )
echo "Wrote $OUT"
unzip -l "$OUT"
