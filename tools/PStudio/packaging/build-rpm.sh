#!/usr/bin/env bash
# Builds the RPM package of PStudio, pedit (its command line counterpart), the configuration editor pcfg and the archivers ppak and ppakz, for Fedora,
# RHEL, openSUSE and the like: the same files as the Debian package, a self-contained build of all of them in one
# folder (no .NET is needed on the target), the desktop entry, the MIME type of the Promptuarium containers, the rules
# of "file", and the man pages.
#
# Usage: tools/PStudio/packaging/build-rpm.sh [--version 8.0.0] [--runtime linux-x64|linux-arm64] [--output DIR]

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

# A pre-release (8.0.0-rc1) is 8.0.0~rc1 for RPM: a version can not have a "-", and "~" sorts before 8.0.0
RPM_VERSION="${VERSION//-/\~}"
DOTNET_VERSION="${VERSION//\~/-}"

case "${RUNTIME}" in
  linux-x64) ARCH="x86_64" ;;
  linux-arm64) ARCH="aarch64" ;;
  *) echo "Unsupported runtime: ${RUNTIME} (linux-x64 or linux-arm64)" >&2; exit 1 ;;
esac

command -v rpmbuild >/dev/null || { echo "rpmbuild is needed (package rpm, or rpm-build)." >&2; exit 1; }

PACKAGE="pstudio"
mkdir -p "${OUTPUT_DIR}"
OUTPUT_DIR="$(cd -- "${OUTPUT_DIR}" && pwd)"
WORK_DIR="${OUTPUT_DIR}/rpm-${RUNTIME}"
ROOT="${WORK_DIR}/root"

echo "==> PStudio ${RPM_VERSION}, ${RUNTIME} (${ARCH})"
rm -rf "${WORK_DIR}"
mkdir -p "${ROOT}/usr/bin" \
  "${ROOT}/usr/lib/${PACKAGE}" \
  "${ROOT}/usr/share/${PACKAGE}" \
  "${ROOT}/usr/share/applications" \
  "${ROOT}/usr/share/mime/packages" \
  "${ROOT}/usr/share/icons/hicolor/scalable/apps" \
  "${ROOT}/usr/share/icons/hicolor/scalable/mimetypes" \
  "${ROOT}/usr/share/man/man1" \
  "${ROOT}/usr/share/licenses/${PACKAGE}"

# PStudio, pedit, pcfg, ppak and ppakz in the same folder: the files of the runtime are the same for all of them
for project in PStudio/PStudio.csproj PEdit/PEdit.csproj PCfg/PCfg.csproj PPak/PPak.csproj PPakZ/PPakZ.csproj; do
  echo "==> Publishing ${project}"
  dotnet publish "${REPO_ROOT}/tools/${project}" \
    -c Release \
    -r "${RUNTIME}" \
    --self-contained true \
    -p:Version="${DOTNET_VERSION}" \
    -p:DebugType=none \
    -o "${ROOT}/usr/lib/${PACKAGE}"
done

find "${ROOT}/usr/lib/${PACKAGE}" -name '*.pdb' -delete

for command in pstudio pedit pcfg ppak ppakz; do
  cat > "${ROOT}/usr/bin/${command}" <<EOF
#!/bin/sh
exec /usr/lib/${PACKAGE}/${command} "\$@"
EOF
done

# The desktop entry and the MIME type are the ones of the Debian package
PACKAGING="${SCRIPT_DIR}/debian"
install -m 0644 "${PACKAGING}/pstudio.desktop" "${ROOT}/usr/share/applications/${PACKAGE}.desktop"
install -m 0644 "${PACKAGING}/pstudio-mime.xml" "${ROOT}/usr/share/mime/packages/${PACKAGE}.xml"
install -m 0644 "${SCRIPT_DIR}/pstudio.svg" "${ROOT}/usr/share/icons/hicolor/scalable/apps/${PACKAGE}.svg"
install -m 0644 "${SCRIPT_DIR}/pstudio.svg" "${ROOT}/usr/share/icons/hicolor/scalable/mimetypes/application-x-promptuarium.svg"
install -m 0644 "${REPO_ROOT}/misc/promptuarium.magic" "${ROOT}/usr/share/${PACKAGE}/promptuarium.magic"
gzip -9n -c "${REPO_ROOT}/tools/PEdit/packaging/pedit.1" > "${ROOT}/usr/share/man/man1/pedit.1.gz"
gzip -9n -c "${REPO_ROOT}/tools/PCfg/packaging/pcfg.1" > "${ROOT}/usr/share/man/man1/pcfg.1.gz"
gzip -9n -c "${REPO_ROOT}/tools/PPak/packaging/ppak.1" > "${ROOT}/usr/share/man/man1/ppak.1.gz"
gzip -9n -c "${REPO_ROOT}/tools/PPakZ/packaging/ppakz.1" > "${ROOT}/usr/share/man/man1/ppakz.1.gz"
install -m 0644 "${REPO_ROOT}/LICENSE" "${ROOT}/usr/share/licenses/${PACKAGE}/LICENSE"

find "${ROOT}" -type d -exec chmod 0755 {} +
find "${ROOT}/usr" -type f -exec chmod 0644 {} +
for command in pstudio pedit pcfg ppak ppakz; do
  chmod 0755 "${ROOT}/usr/bin/${command}" "${ROOT}/usr/lib/${PACKAGE}/${command}"
done

# The scriptlets are the ones of the Debian package: the rules of the containers between markers in /etc/magic
cat > "${WORK_DIR}/${PACKAGE}.spec" <<EOF
# The files are built already (self-contained .NET): no debug package, no stripping, and the dependencies are given
# by hand instead of being found in the libraries of the runtime
%global debug_package %{nil}
%global __os_install_post %{nil}
%global _build_id_links none
%global magic_begin # BEGIN pstudio: Promptuarium container
%global magic_end # END pstudio

Name:           ${PACKAGE}
Version:        ${RPM_VERSION}
Release:        1
Summary:        Graphical editor and command line tools of Promptuarium trees
License:        MIT
URL:            https://github.com/fuszenecker/Promptuarium
AutoReqProv:    no
Requires:       glibc, libstdc++, libgcc, zlib, libX11, libICE, libSM, fontconfig
Recommends:     shared-mime-info, desktop-file-utils, file
Provides:       pedit = %{version}-%{release}

%description
PStudio views and edits Promptuarium containers: a tree view with drag and
drop, the contents in every form of the Data class (hex, text, numbers, dates,
GUIDs), import and export, the chunks of the saved form, and nested trees.

pedit, its command line counterpart, creates, dumps and edits the containers
with single, non-interactive calls; with --json its output is machine-readable,
so it can be driven by scripts and AI agents.

ppak packs a directory tree (files, directories, symbolic links, with sizes,
CRC-32s, permissions, owners and times) into one Promptuarium container, and
unpacks it; ppakz is the same with gzip-compressed contents.

pcfg shows and edits the configuration files of Promptuarium (PRMT:CFG1-LE):
the values by their keys (Logging:LogLevel:Default), with their types and
comments, as a tree or as key = value lines; it imports and exports JSON.

The package makes PStudio the application of the Promptuarium containers
(MIME type application/x-promptuarium, *.prmt and the "PRMT" signature), and
adds the rules of the signature to /etc/magic, so that "file" recognizes them.

%install
cp -a "${ROOT}/." "%{buildroot}/"

%define add_magic_rules \\
if [ -f /etc/magic ] && [ -f /usr/share/pstudio/promptuarium.magic ]; then \\
    sed -i "/^%{magic_begin}\\\$/,/^%{magic_end}\\\$/d" /etc/magic \\
    { echo "%{magic_begin}"; cat /usr/share/pstudio/promptuarium.magic; echo "%{magic_end}"; } >> /etc/magic \\
fi \\
exit 0

%post
%{add_magic_rules}

# /etc/magic comes with "file" (Fedora, RHEL) or "file-magic" (openSUSE), which may be installed after PStudio
%triggerin -- file
%{add_magic_rules}

%triggerin -- file-magic
%{add_magic_rules}

%postun
# Only when the package is removed, not when it is upgraded
if [ "\$1" -eq 0 ] && [ -f /etc/magic ]; then
    sed -i "/^%{magic_begin}\\\$/,/^%{magic_end}\\\$/d" /etc/magic
fi
exit 0

%files
%license /usr/share/licenses/${PACKAGE}/LICENSE
/usr/bin/pstudio
/usr/bin/pedit
/usr/bin/pcfg
/usr/bin/ppak
/usr/bin/ppakz
/usr/lib/${PACKAGE}
/usr/share/${PACKAGE}
/usr/share/applications/${PACKAGE}.desktop
/usr/share/mime/packages/${PACKAGE}.xml
/usr/share/icons/hicolor/scalable/apps/${PACKAGE}.svg
/usr/share/icons/hicolor/scalable/mimetypes/application-x-promptuarium.svg
/usr/share/man/man1/pedit.1.gz
/usr/share/man/man1/pcfg.1.gz
/usr/share/man/man1/ppak.1.gz
/usr/share/man/man1/ppakz.1.gz

%changelog
* $(LC_ALL=C date '+%a %b %d %Y') Fuszenecker Róbert <robert.fuszenecker@gmail.com> - ${RPM_VERSION}-1
- Release ${VERSION}, see https://github.com/fuszenecker/Promptuarium/releases
EOF

echo "==> Building the RPM package"
rpmbuild -bb \
  --target "${ARCH}" \
  --define "_topdir ${WORK_DIR}/rpmbuild" \
  --define "_rpmdir ${OUTPUT_DIR}" \
  --define "_build_name_fmt %%{NAME}-%%{VERSION}-%%{RELEASE}.%%{ARCH}.rpm" \
  "${WORK_DIR}/${PACKAGE}.spec"

RPM="${OUTPUT_DIR}/${PACKAGE}-${RPM_VERSION}-1.${ARCH}.rpm"
echo "${RPM}"
