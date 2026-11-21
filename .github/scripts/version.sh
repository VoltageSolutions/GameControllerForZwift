#!/usr/bin/env bash
# Version helpers for the workflows. The app version lives in one place: <Version> in windows/Directory.Build.props.
#
#   version.sh get                 print the version from windows/Directory.Build.props
#   version.sh set X.Y.Z           write X.Y.Z into windows/Directory.Build.props (keeps line endings and BOM)
#   version.sh latest-tag [--local] print the highest vX.Y.Z release tag as X.Y.Z (empty if none);
#                                  reads origin's tags, or local tags with --local
#   version.sh gt A B              exit 0 if version A is higher than version B
#   version.sh from-branch NAME    print X.Y.Z for a branch named release/vX.Y.Z, fail otherwise
#   version.sh next-minor X.Y.Z    print X.(Y+1).0
#
# Versions are plain X.Y.Z (no leading zeros, no suffix). VERSION_PROPS overrides the props file path.
set -euo pipefail

SEMVER_RE='^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$'

die() { echo "version.sh: $*" >&2; exit 1; }

props_path() {
  if [ -n "${VERSION_PROPS:-}" ]; then echo "$VERSION_PROPS"; return; fi
  local top
  if top="$(git rev-parse --show-toplevel 2>/dev/null)"; then
    echo "$top/windows/Directory.Build.props"
  else
    echo "$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)/windows/Directory.Build.props"
  fi
}

is_semver() { [[ "$1" =~ $SEMVER_RE ]]; }

require_semver() { is_semver "$1" || die "'$1' is not a version of the form X.Y.Z"; }

cmd_get() {
  local props version
  props="$(props_path)"
  [ -f "$props" ] || die "$props not found"
  version="$(sed -n 's:.*<Version>\([^<]*\)</Version>.*:\1:p' "$props" | head -n 1 | tr -d '[:space:]')"
  [ -n "$version" ] || die "no <Version> in $props"
  require_semver "$version"
  echo "$version"
}

cmd_set() {
  local new="${1:-}" props count
  require_semver "$new"
  props="$(props_path)"
  [ -f "$props" ] || die "$props not found"
  count="$(grep -c '<Version>[^<]*</Version>' "$props" || true)"
  [ "$count" = 1 ] || die "expected exactly one <Version> line in $props, found $count"
  # Only the text between the tags changes, so a CR at the end of the line and a BOM at the start stay as they are.
  sed -i "s:<Version>[^<]*</Version>:<Version>${new}</Version>:" "$props"
  [ "$(cmd_get)" = "$new" ] || die "writing $new to $props failed"
}

cmd_latest_tag() {
  local tags
  if [ "${1:-}" = "--local" ]; then
    tags="$(git tag --list 'v*')"
  else
    tags="$(git ls-remote --tags --refs origin 'refs/tags/v*' | sed 's:.*refs/tags/::')"
  fi
  printf '%s\n' "$tags" | tr -d '\r' | sed -n 's/^v//p' | grep -E "$SEMVER_RE" \
    | sort -t . -k1,1n -k2,2n -k3,3n | tail -n 1 || true
}

cmd_gt() {
  local a="${1:-}" b="${2:-}" a1 a2 a3 b1 b2 b3
  require_semver "$a"
  require_semver "$b"
  IFS=. read -r a1 a2 a3 <<< "$a"
  IFS=. read -r b1 b2 b3 <<< "$b"
  if ((10#$a1 != 10#$b1)); then ((10#$a1 > 10#$b1)); return; fi
  if ((10#$a2 != 10#$b2)); then ((10#$a2 > 10#$b2)); return; fi
  ((10#$a3 > 10#$b3))
}

cmd_from_branch() {
  local name="${1:-}"
  name="${name#refs/heads/}"
  [[ "$name" == release/v* ]] || die "'$name' is not named release/vX.Y.Z"
  is_semver "${name#release/v}" || die "'$name' is not named release/vX.Y.Z"
  echo "${name#release/v}"
}

cmd_next_minor() {
  local v="${1:-}" major minor patch
  require_semver "$v"
  IFS=. read -r major minor patch <<< "$v"
  echo "${major}.$((10#$minor + 1)).0"
}

cmd="${1:-}"
shift || true
case "$cmd" in
  get) cmd_get ;;
  set) cmd_set "$@" ;;
  latest-tag) cmd_latest_tag "$@" ;;
  gt) cmd_gt "$@" ;;
  from-branch) cmd_from_branch "$@" ;;
  next-minor) cmd_next_minor "$@" ;;
  *) die "usage: version.sh get | set X.Y.Z | latest-tag [--local] | gt A B | from-branch NAME | next-minor X.Y.Z" ;;
esac
