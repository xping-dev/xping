#!/usr/bin/env bash
# Imports the reference pages and schemas generated in xping-dev/xping at a release tag.
# Usage: scripts/import-reference.sh v2.0.0-alpha.1
set -euo pipefail

tag="${1:?usage: $0 <tag>}"
root="$(cd "$(dirname "$0")/.." && pwd)"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

git clone --quiet --depth 1 --branch "$tag" --filter=blob:none --sparse https://github.com/xping-dev/xping.git "$work/xping"
git -C "$work/xping" sparse-checkout set docs/reference schema

# Reference pages: replaced wholesale, plus a toc listing them.
find "$root/reference" -name '*.md' ! -name index.md -delete
cp "$work/xping/docs/reference/"*.md "$root/reference/"
{
  echo "- name: Overview"
  echo "  href: index.md"
  for page in "$root/reference/"*.md; do
    name="$(basename "$page")"
    [ "$name" = index.md ] && continue
    title="$(grep -m1 '^# ' "$page" | sed 's/^# //')"
    echo "- name: ${title:-$name}"
    echo "  href: $name"
  done
} > "$root/reference/toc.yml"

mkdir -p "$root/schema"
cp "$work/xping/schema/"*.json "$root/schema/"

echo "Imported docs/reference and schema from xping-dev/xping@$tag"
