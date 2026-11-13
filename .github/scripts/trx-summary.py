#!/usr/bin/env python3
"""Summarise .trx test results as Markdown (for $GITHUB_STEP_SUMMARY). Usage: trx-summary.py <title> <dir>"""
import sys
import pathlib
import xml.etree.ElementTree as ET

title, root = sys.argv[1], pathlib.Path(sys.argv[2])
ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
rows, failures = [], []
for trx in sorted(root.rglob("*.trx")):
    tree = ET.parse(trx)
    c = tree.find(".//t:ResultSummary/t:Counters", ns)
    if c is None:
        continue
    rows.append((trx.stem, int(c.get("total", 0)), int(c.get("passed", 0)),
                 int(c.get("failed", 0)), int(c.get("notExecuted", 0))))
    for r in tree.iterfind(".//t:UnitTestResult[@outcome='Failed']", ns):
        msg = r.find(".//t:ErrorInfo/t:Message", ns)
        failures.append((r.get("testName"), (msg.text or "").strip()[:500] if msg is not None else ""))

print(f"### {title}\n")
if not rows:
    print("No .trx files found.")
else:
    print("| Result file | Total | Passed | Failed | Skipped |\n|---|---:|---:|---:|---:|")
    for n, t, p, f, s in rows:
        print(f"| {n} | {t} | {p} | {f} | {s} |")
    print(f"\n**Total: {sum(r[1] for r in rows)}, passed: {sum(r[2] for r in rows)}, "
          f"failed: {sum(r[3] for r in rows)}, skipped: {sum(r[4] for r in rows)}**")
if failures:
    print("\n<details open><summary>Failed tests</summary>\n")
    for name, msg in failures:
        print(f"- `{name}`\n\n  ```\n  {msg}\n  ```")
    print("\n</details>")
print()
