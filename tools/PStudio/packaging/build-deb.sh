#!/usr/bin/env bash
# Builds the Debian package of PStudio and pedit (its command line counterpart): a self-contained build of both in one
# folder (no .NET is needed on the target, and they share the runtime), the desktop entry, the MIME type of the
# Promptuarium containers (so that PStudio opens them), the rules of "file", and the man page of pedit.
#
# Usage: tools/PStudio/packaging/build-deb.sh [--version 8.0.0] [--runtime linux-x64|linux-arm64] [--output DIR]

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
    -h|--help) sed -n '2,6p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'; exit 0 ;;
    *) echo "Unknown option: $1" >&2; exit 1 ;;
  esac
done

# The version of the tools is the version of the library
if [[ -z "${VERSION}" ]]; then
  VERSION="$(dotnet msbuild "${REPO_ROOT}/src/Promptuarium/Promptuarium.csproj" -getProperty:Version)"
fi

case "${RUNTIME}" in
  linux-x64) ARCH="amd64" ;;
  linux-arm64) ARCH="arm64" ;;
  *) echo "Unsupported runtime: ${RUNTIME} (linux-x64 or linux-arm64)" >&2; exit 1 ;;
esac

command -v dpkg-deb >/dev/null || { echo "dpkg-deb is needed (package dpkg)." >&2; exit 1; }

PACKAGE="pstudio"
WORK_DIR="${OUTPUT_DIR}/deb-${RUNTIME}"
ROOT="${WORK_DIR}/${PACKAGE}_${VERSION}_${ARCH}"
DEB="${OUTPUT_DIR}/${PACKAGE}_${VERSION}_${ARCH}.deb"

echo "==> PStudio ${VERSION}, ${RUNTIME} (${ARCH})"
rm -rf "${WORK_DIR}"
mkdir -p "${ROOT}/DEBIAN" \
  "${ROOT}/usr/bin" \
  "${ROOT}/usr/lib/${PACKAGE}" \
  "${ROOT}/usr/share/${PACKAGE}" \
  "${ROOT}/usr/share/applications" \
  "${ROOT}/usr/share/mime/packages" \
  "${ROOT}/usr/share/icons/hicolor/scalable/apps" \
  "${ROOT}/usr/share/icons/hicolor/scalable/mimetypes" \
  "${ROOT}/usr/share/man/man1" \
  "${ROOT}/usr/share/doc/${PACKAGE}" \
  "${ROOT}/usr/share/lintian/overrides"

# PStudio and pedit in the same folder: the files of the runtime are the same for both
for project in PStudio/PStudio.csproj PEdit/PEdit.csproj; do
  echo "==> Publishing ${project}"
  dotnet publish "${REPO_ROOT}/tools/${project}" \
    -c Release \
    -r "${RUNTIME}" \
    --self-contained true \
    -p:Version="${VERSION}" \
    -p:DebugType=none \
    -o "${ROOT}/usr/lib/${PACKAGE}"
done

# The native libraries bring their debug symbols, which the package does not need
find "${ROOT}/usr/lib/${PACKAGE}" -name '*.pdb' -delete

for command in pstudio pedit; do
  cat > "${ROOT}/usr/bin/${command}" <<EOF
#!/bin/sh
exec /usr/lib/${PACKAGE}/${command} "\$@"
EOF
done

PACKAGING="${SCRIPT_DIR}/debian"
install -m 0644 "${PACKAGING}/pstudio.desktop" "${ROOT}/usr/share/applications/${PACKAGE}.desktop"
install -m 0644 "${PACKAGING}/pstudio-mime.xml" "${ROOT}/usr/share/mime/packages/${PACKAGE}.xml"
install -m 0644 "${SCRIPT_DIR}/pstudio.svg" "${ROOT}/usr/share/icons/hicolor/scalable/apps/${PACKAGE}.svg"
install -m 0644 "${SCRIPT_DIR}/pstudio.svg" "${ROOT}/usr/share/icons/hicolor/scalable/mimetypes/application-x-promptuarium.svg"
install -m 0644 "${REPO_ROOT}/misc/promptuarium.magic" "${ROOT}/usr/share/${PACKAGE}/promptuarium.magic"
gzip -9n -c "${REPO_ROOT}/tools/PEdit/packaging/pedit.1" > "${ROOT}/usr/share/man/man1/pedit.1.gz"
install -m 0644 "${REPO_ROOT}/LICENSE" "${ROOT}/usr/share/doc/${PACKAGE}/copyright"
install -m 0644 "${PACKAGING}/lintian-overrides" "${ROOT}/usr/share/lintian/overrides/${PACKAGE}"
install -m 0755 "${PACKAGING}/postinst" "${PACKAGING}/postrm" "${ROOT}/DEBIAN/"

cat > "${WORK_DIR}/changelog" <<EOF
${PACKAGE} (${VERSION}) unstable; urgency=medium

  * Release ${VERSION}, see https://github.com/fuszenecker/Promptuarium/releases

 -- Fuszenecker Róbert <robert.fuszenecker@gmail.com>  $(date -R)
EOF
gzip -9n -c "${WORK_DIR}/changelog" > "${ROOT}/usr/share/doc/${PACKAGE}/changelog.gz"

# dpkg-deb wants the usual permissions, whatever the umask of the build was
find "${ROOT}" -type d -exec chmod 0755 {} +
find "${ROOT}/usr" -type f -exec chmod 0644 {} +
chmod 0755 "${ROOT}/usr/bin/pstudio" "${ROOT}/usr/bin/pedit" "${ROOT}/usr/lib/${PACKAGE}/pstudio" "${ROOT}/usr/lib/${PACKAGE}/pedit"

cat > "${ROOT}/DEBIAN/control" <<EOF
Package: ${PACKAGE}
Version: ${VERSION}
Section: devel
Priority: optional
Architecture: ${ARCH}
Maintainer: Fuszenecker Róbert <robert.fuszenecker@gmail.com>
Depends: libc6, libstdc++6, libgcc-s1, zlib1g, libx11-6, libice6, libsm6, libfontconfig1
Recommends: shared-mime-info, desktop-file-utils, file
Provides: pedit
Replaces: pedit
Conflicts: pedit
Installed-Size: $(du -sk "${ROOT}/usr" | cut -f1)
Homepage: https://github.com/fuszenecker/Promptuarium
Description: Graphical editor and command line tool of Promptuarium trees
 PStudio views and edits Promptuarium containers: a tree view with drag and
 drop, the contents in every form of the Data class (hex, text, numbers, dates,
 GUIDs), import and export, the chunks of the saved form, and nested trees.
 .
 pedit, its command line counterpart, creates, dumps and edits the containers
 with single, non-interactive calls; with --json its output is machine-readable,
 so it can be driven by scripts and AI agents (it replaces the pedit package).
 .
 The package makes PStudio the application of the Promptuarium containers
 (MIME type application/x-promptuarium, *.prmt and the "PRMT" signature), and
 adds the rules of the signature to /etc/magic, so that "file" recognizes them.
EOF

echo "==> Building ${DEB}"
dpkg-deb --build --root-owner-group "${ROOT}" "${DEB}"
echo "${DEB}"
