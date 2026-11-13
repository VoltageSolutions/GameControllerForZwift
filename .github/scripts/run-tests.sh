#!/usr/bin/env bash
# Runs the given test projects (already built in Release) and fails if any fails.
# Usage: run-tests.sh <results-dir> <project>...
set -u
out="$1"; shift
rc=0
for p in "$@"; do
  name="$(basename "$p")"
  dotnet test "$p" -c Release --no-build --verbosity normal \
    --logger "trx;LogFileName=${name}.trx" \
    --results-directory "$out/${name}" \
    --collect:"XPlat Code Coverage" || rc=1
done
exit $rc
