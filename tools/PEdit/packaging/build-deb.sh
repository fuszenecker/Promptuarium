#!/usr/bin/env bash
# Builds the Debian package of pedit: a self-contained, single-file executable (no .NET is needed on the target)
# with its man page.
#
# Usage: tools/PEdit/packaging/build-deb.sh [--version 8.0.0] [--runtime linux-x64|linux-arm64] [--output DIR]

set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "${SCRIPT_DIR}/../../.." && pwd)"

VERSION=""
RUNTIME="linux-x64"
OUTPUT_DIR="${REPO_ROOT}/artifacts"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --version) VERSION="$2"; shift 2 ;;
    --runtime) RUNTIME="$2"; shift 2 ;;
    --output) OUTPUT_DIR="$2"; shift 2 ;;
    -h|--help) sed -n '2,5p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'; exit 0 ;;
    *) echo "Unknown option: $1" >&2; exit 1 ;;
  esac
done

# The version of the tools is the version of the library
if [[ -z "${VERSION}" ]]; then
  VERSION="$(dotnet msbuild "${REPO_ROOT}/src/Promptuarium.csproj" -getProperty:Version)"
fi

case "${RUNTIME}" in
  linux-x64) ARCH="amd64" ;;
  linux-arm64) ARCH="arm64" ;;
  *) echo "Unsupported runtime: ${RUNTIME} (linux-x64 or linux-arm64)" >&2; exit 1 ;;
esac

command -v dpkg-deb >/dev/null || { echo "dpkg-deb is needed (package dpkg)." >&2; exit 1; }

PACKAGE="pedit"
WORK_DIR="${OUTPUT_DIR}/deb-${PACKAGE}-${RUNTIME}"
ROOT="${WORK_DIR}/${PACKAGE}_${VERSION}_${ARCH}"
DEB="${OUTPUT_DIR}/${PACKAGE}_${VERSION}_${ARCH}.deb"

echo "==> pedit ${VERSION}, ${RUNTIME} (${ARCH})"
rm -rf "${WORK_DIR}"
mkdir -p "${ROOT}/DEBIAN" \
  "${ROOT}/usr/bin" \
  "${ROOT}/usr/share/man/man1" \
  "${ROOT}/usr/share/doc/${PACKAGE}" \
  "${ROOT}/usr/share/lintian/overrides"

echo "==> Publishing"
dotnet publish "${REPO_ROOT}/tools/PEdit/PEdit.csproj" \
  -c Release \
  -r "${RUNTIME}" \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:EnableCompressionInSingleFile=true \
  -p:Version="${VERSION}" \
  -p:DebugType=none \
  -o "${WORK_DIR}/publish"

install -m 0755 "${WORK_DIR}/publish/${PACKAGE}" "${ROOT}/usr/bin/${PACKAGE}"
gzip -9n -c "${SCRIPT_DIR}/pedit.1" > "${ROOT}/usr/share/man/man1/${PACKAGE}.1.gz"
install -m 0644 "${REPO_ROOT}/LICENSE" "${ROOT}/usr/share/doc/${PACKAGE}/copyright"
install -m 0644 "${SCRIPT_DIR}/lintian-overrides" "${ROOT}/usr/share/lintian/overrides/${PACKAGE}"

cat > "${WORK_DIR}/changelog" <<EOF
${PACKAGE} (${VERSION}) unstable; urgency=medium

  * Release ${VERSION}, see https://github.com/fuszenecker/Promptuarium/releases

 -- Fuszenecker Róbert <robert.fuszenecker@gmail.com>  $(date -R)
EOF
gzip -9n -c "${WORK_DIR}/changelog" > "${ROOT}/usr/share/doc/${PACKAGE}/changelog.gz"

find "${ROOT}" -type d -exec chmod 0755 {} +
find "${ROOT}/usr/share" -type f -exec chmod 0644 {} +

cat > "${ROOT}/DEBIAN/control" <<EOF
Package: ${PACKAGE}
Version: ${VERSION}
Section: devel
Priority: optional
Architecture: ${ARCH}
Maintainer: Fuszenecker Róbert <robert.fuszenecker@gmail.com>
Depends: libc6, libstdc++6, libgcc-s1, zlib1g
Installed-Size: $(du -sk "${ROOT}/usr" | cut -f1)
Homepage: https://github.com/fuszenecker/Promptuarium
Description: Command line tool to create, dump and edit Promptuarium trees
 pedit creates, dumps and edits Promptuarium containers from the command line.
 Every command is a single, non-interactive call; with --json the output is
 machine-readable, so it can be driven by scripts and AI agents. The chunks
 command decodes a file directly from the specification of the format.
EOF

echo "==> Building ${DEB}"
dpkg-deb --build --root-owner-group "${ROOT}" "${DEB}"
echo "${DEB}"
