#!/usr/bin/env python3
"""Append a markdown summary of SARIF results to $GITHUB_STEP_SUMMARY. Never fails."""
import collections
import glob
import json
import os
import sys


def main():
    root = sys.argv[1] if len(sys.argv) > 1 else "sarif-results"
    out = os.environ.get("GITHUB_STEP_SUMMARY")
    rows = []
    for path in glob.glob(os.path.join(root, "**", "*.sarif"), recursive=True):
        with open(path, encoding="utf-8") as f:
            data = json.load(f)
        for run in data.get("runs", []):
            rules = {}
            for r in run.get("tool", {}).get("driver", {}).get("rules", []) or []:
                rules[r.get("id")] = r
            for res in run.get("results", []) or []:
                rid = res.get("ruleId", "?")
                level = res.get("level") or (
                    rules.get(rid, {}).get("defaultConfiguration", {}).get("level")) or "warning"
                msg = (res.get("message", {}).get("text") or "").replace("\n", " ").replace("|", "\\|")
                if len(msg) > 120:
                    msg = msg[:117] + "..."
                loc = "?"
                locs = res.get("locations") or []
                if locs:
                    pl = locs[0].get("physicalLocation", {})
                    uri = pl.get("artifactLocation", {}).get("uri", "?")
                    loc = "%s:%s" % (uri, pl.get("region", {}).get("startLine", "?"))
                rows.append((rid, level, msg, loc))
    lines = ["## CodeQL alerts (SARIF): %d total" % len(rows), ""]
    counts = collections.Counter((r[0], r[1]) for r in rows)
    lines += ["| Rule | Level | Count |", "|---|---|---|"]
    for (rid, level), n in sorted(counts.items(), key=lambda kv: (-kv[1], kv[0])):
        lines.append("| %s | %s | %d |" % (rid, level, n))
    lines += ["", "### Alerts (first 100)", "", "| Rule | Level | Message | Location |", "|---|---|---|---|"]
    for rid, level, msg, loc in rows[:100]:
        lines.append("| %s | %s | %s | `%s` |" % (rid, level, msg, loc))
    text = "\n".join(lines) + "\n"
    if out:
        with open(out, "a", encoding="utf-8") as f:
            f.write(text)
    else:
        print(text)


if __name__ == "__main__":
    try:
        main()
    except Exception as e:  # never fail the job
        print("sarif-summary failed: %s" % e)
    sys.exit(0)
