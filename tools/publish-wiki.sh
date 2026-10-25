#!/usr/bin/env bash
# Copies the API reference (docs/) into a clone of the GitHub wiki: docs/README.md becomes the page API-Reference,
# the links lose their .md (the wiki links to page names), and _Sidebar.md lists the pages. The other pages of the
# wiki (Home, Examples, ...) are not touched; the pages written here are listed in .api-pages, so pages of types
# that were removed are removed too.
#
# Usage: tools/publish-wiki.sh WIKI_DIR

set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
DOCS="${SCRIPT_DIR}/../docs"
WIKI="${1:?Usage: publish-wiki.sh WIKI_DIR}"
MANIFEST="${WIKI}/.api-pages"

# The pages of the previous run
if [[ -f "${MANIFEST}" ]]; then
  while IFS= read -r page; do
    rm -f -- "${WIKI:?}/${page}"
  done < "${MANIFEST}"
fi

: > "${MANIFEST}"

page_of() { [[ "$1" == README ]] && echo API-Reference || echo "$1"; }

for source in "${DOCS}"/*.md; do
  name="$(basename "${source}" .md)"
  page="$(page_of "${name}").md"
  # Local links only: ](Data.md 'title') -> ](Data 'title'), ](README.md -> ](API-Reference
  perl -pe 's/\]\(README\.md(?=[ #)])/](API-Reference/g; s/\]\((?!https?:)([^ #]*?)\.md(?=[ #)])/](\1/g' "${source}" > "${WIKI}/${page}"
  echo "${page}" >> "${MANIFEST}"
done

{
  echo "* [Home](Home)"
  [[ -f "${WIKI}/Examples.md" ]] && echo "* [Examples](Examples)"
  echo "* [API Reference](API-Reference)"
  for source in "${DOCS}"/*.md; do
    name="$(basename "${source}" .md)"
    [[ "${name}" == README || "${name}" == *"("* ]] && continue
    echo "  * [${name}](${name})"
  done
} > "${WIKI}/_Sidebar.md"
echo "_Sidebar.md" >> "${MANIFEST}"
