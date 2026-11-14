#!/usr/bin/env python3
"""Parse `dotnet list package --vulnerable --include-transitive --format json` output.

Usage: vuln-check.py <json-file>
Prints a markdown table to stdout (for the step summary), emits workflow annotations
to stderr, and exits 1 if any High or Critical advisory is present.
"""
import json
import sys

path = sys.argv[1]
text = open(path, encoding="utf-8-sig").read()
start = text.find("{")
data = json.loads(text[start:]) if start >= 0 else {"projects": []}

rows = set()
for proj in data.get("projects", []):
    pname = proj.get("path", "").replace("\\", "/").rsplit("/", 1)[-1]
    for fw in proj.get("frameworks", []):
        for kind in ("topLevelPackages", "transitivePackages"):
            for pkg in fw.get(kind, []):
                for v in pkg.get("vulnerabilities", []):
                    rows.add((pname, pkg.get("id"), pkg.get("resolvedVersion"),
                              v.get("severity", "Unknown"), v.get("advisoryurl", "")))

order = {"Critical": 0, "High": 1, "Moderate": 2, "Low": 3}
rows = sorted(rows, key=lambda r: (order.get(r[3], 9), r[1] or ""))
print("### NuGet vulnerabilities\n")
if not rows:
    print("No known vulnerable packages.")
    sys.exit(0)
print("| Severity | Package | Version | Project | Advisory |")
print("|---|---|---|---|---|")
for proj, pid, ver, sev, url in rows:
    print(f"| {sev} | {pid} | {ver} | {proj} | {url} |")

blocking = [r for r in rows if r[3] in ("High", "Critical")]
for proj, pid, ver, sev, url in rows:
    level = "error" if sev in ("High", "Critical") else "warning"
    print(f"::{level} title=Vulnerable package::{sev}: {pid} {ver} ({proj}) {url}", file=sys.stderr)
sys.exit(1 if blocking else 0)
