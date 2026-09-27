"""Phase C analysis: published run vs harness re-run (pre-fix "-old" and fixed "-new" LR models)."""
import csv, sys, statistics as st
from collections import defaultdict

PUB = r"E:\University.Ph.D\4. Paper2\AlgorithmRun\1403-07-17-single feature multi threshold 128\All-summary.txt"
OUT = sys.argv[1]

METRICS = ["NumberOfPoints_Simplified", "Compression", "TlvdPerLength", "TruePositive", "FalsePositive", "FalseNegative", "F1Score"]


def load(path):
    with open(path, encoding="utf-8-sig", newline="") as f:
        rows = list(csv.DictReader(f, delimiter="\t"))
    for r in rows:
        r["FeatureIndex"] = int(r["FeatureIndex"])
        r["Coef"] = float(r["Coef"])
    return rows


pub = load(PUB)
mine = load(OUT + r"\All-summary.txt")

# ---------------------------------------------------------------- 1. fidelity
def key(r, method):
    return (r["FileName"], r["FeatureIndex"], r["Coef"], method)

mine_by_key = {}
for r in mine:
    m = r["MethodName"]
    if m.endswith("-old"):
        m = m[:-4]
    elif m.endswith("-new"):
        continue
    mine_by_key.setdefault(key(r, m), r)

print("=== 1. fidelity: published rows reproduced by the harness (pre-fix models, same features) ===")
per_method = defaultdict(lambda: [0, 0, 0])  # rows, all metrics equal, geometry metrics equal
missing = 0
first_diffs = []
for r in pub:
    m = r["MethodName"]
    k = key(r, m)
    if k not in mine_by_key:
        missing += 1
        continue
    o = mine_by_key[k]
    stats = per_method[m]
    stats[0] += 1
    geom_equal = all(r[c] == o[c] for c in ["NumberOfPoints_Simplified", "Compression", "TlvdPerLength"])
    all_equal = all(r[c] == o[c] for c in METRICS)
    stats[1] += all_equal
    stats[2] += geom_equal
    if not all_equal and len(first_diffs) < 8:
        first_diffs.append((k, {c: (r[c], o[c]) for c in METRICS if r[c] != o[c]}))
print(f"published rows without a harness row: {missing}")
for m, (n, a, g) in sorted(per_method.items()):
    print(f"  {m:6}: {n:5} rows, identical points/compression/TLVD {g / n:7.2%}, identical incl. confusion matrix {a / n:7.2%}")
for k, d in first_diffs:
    print("  diff", k, d)

# ---------------------------------------------------------------- 2. old vs new
# LR rows are written three times per feature (once per coef loop) with Coef = 1; keep one
def unique_rows(rows):
    seen, result = set(), []
    for r in rows:
        k = (r["FileName"], r["FeatureIndex"], r["Coef"], r["MethodName"])
        if k in seen:
            continue
        seen.add(k)
        result.append(r)
    return result

mine_u = unique_rows(mine)

def f(r, c):
    return float(r[c])

groups = defaultdict(list)
for r in mine_u:
    label = r["MethodName"] if r["MethodName"].startswith("LR") else f'{r["MethodName"]} x{r["Coef"]:g}'
    groups[label].append(r)

order = [g for g in ["LR4v23-old", "LR4v23-new", "LR7v23-old", "LR7v23-new", "LR4v23-LF-old", "LR4v23-LF-new"]] + \
        [f"{m} x{c:g}" for m in ["RDP", "RW", "NOPW", "BOPW"] for c in [0.5, 1, 2]]

print()
print("=== 2. means over all features (single feature, 128 px) ===")
print(f'{"method":16} {"rows":>5} {"compression":>11} {"TLVD/len":>9} {"F1":>7} {"accuracy":>9} {"points":>8}')
for g in order:
    rows = groups.get(g, [])
    if not rows:
        continue
    print(f'{g:16} {len(rows):5} {st.mean(f(r, "Compression") for r in rows):11.4f} {st.mean(f(r, "TlvdPerLength") for r in rows):9.4f} '
          f'{st.mean(f(r, "F1Score") for r in rows):7.4f} {st.mean(f(r, "Accuracy") for r in rows):9.4f} {st.mean(f(r, "NumberOfPoints_Simplified") for r in rows):8.1f}')

print()
print("=== 3. by layer type: LR old -> new (compression / TLVD / F1) ===")
types = sorted({r["Type"] for r in mine_u})
for name in ["LR4v23", "LR7v23", "LR4v23-LF"]:
    print(f"  {name}")
    for t in types:
        o = [r for r in groups[name + "-old"] if r["Type"] == t]
        n = [r for r in groups[name + "-new"] if r["Type"] == t]
        if not o or not n:
            continue
        print(f'    {t:10} compression {st.mean(f(r, "Compression") for r in o):.3f} -> {st.mean(f(r, "Compression") for r in n):.3f}   '
              f'TLVD {st.mean(f(r, "TlvdPerLength") for r in o):.4f} -> {st.mean(f(r, "TlvdPerLength") for r in n):.4f}   '
              f'F1 {st.mean(f(r, "F1Score") for r in o):.4f} -> {st.mean(f(r, "F1Score") for r in n):.4f}')

print()
print("=== 4. per feature: LR vs each baseline at the same feature (share of features where LR is better) ===")
by_feature = defaultdict(dict)
for r in mine_u:
    label = r["MethodName"] if r["MethodName"].startswith("LR") else f'{r["MethodName"]} x{r["Coef"]:g}'
    by_feature[(r["FileName"], r["FeatureIndex"])][label] = r

baselines = [f"{m} x{c:g}" for m in ["RDP", "RW", "NOPW", "BOPW"] for c in [0.5, 1, 2]]
for name in ["LR4v23", "LR7v23", "LR4v23-LF"]:
    print(f"  {name}: TLVD lower (old -> new) | F1 higher (old -> new) | compression higher (old -> new)")
    for b in baselines:
        cnt = 0; tl = [0, 0]; f1 = [0, 0]; co = [0, 0]
        for fk, d in by_feature.items():
            if b not in d or name + "-old" not in d or name + "-new" not in d:
                continue
            cnt += 1
            for i, v in enumerate(["-old", "-new"]):
                lr = d[name + v]
                tl[i] += f(lr, "TlvdPerLength") < f(d[b], "TlvdPerLength")
                f1[i] += f(lr, "F1Score") > f(d[b], "F1Score")
                co[i] += f(lr, "Compression") > f(d[b], "Compression")
        if cnt:
            print(f"    vs {b:10} ({cnt:3}): {tl[0] / cnt:6.1%} -> {tl[1] / cnt:6.1%} | {f1[0] / cnt:6.1%} -> {f1[1] / cnt:6.1%} | {co[0] / cnt:6.1%} -> {co[1] / cnt:6.1%}")

print()
print("=== 5. how many features get a different simplification (old vs new) ===")
for name in ["LR4v23", "LR7v23", "LR4v23-LF"]:
    diff = sum(1 for d in by_feature.values() if name + "-old" in d and name + "-new" in d and
               (d[name + "-old"]["NumberOfPoints_Simplified"] != d[name + "-new"]["NumberOfPoints_Simplified"] or d[name + "-old"]["TlvdPerLength"] != d[name + "-new"]["TlvdPerLength"]))
    total = sum(1 for d in by_feature.values() if name + "-old" in d and name + "-new" in d)
    print(f"  {name}: {diff} of {total} features")

print()
print("=== 6. means over the features whose published geometry metrics are reproduced exactly (all methods) ===")
bad = set()
for r in pub:
    o = mine_by_key[key(r, r["MethodName"])]
    if any(r[c] != o[c] for c in ["NumberOfPoints_Simplified", "Compression", "TlvdPerLength"]):
        bad.add((r["FileName"], r["FeatureIndex"]))
print(f"features: {len({(r['FileName'], r['FeatureIndex']) for r in mine_u} - bad)} of {len({(r['FileName'], r['FeatureIndex']) for r in mine_u})}")
print(f'{"method":16} {"rows":>5} {"compression":>11} {"TLVD/len":>9} {"F1":>7} {"points":>8}')
for g in order:
    rows = [r for r in groups.get(g, []) if (r["FileName"], r["FeatureIndex"]) not in bad]
    if not rows:
        continue
    print(f'{g:16} {len(rows):5} {st.mean(f(r, "Compression") for r in rows):11.4f} {st.mean(f(r, "TlvdPerLength") for r in rows):9.4f} '
          f'{st.mean(f(r, "F1Score") for r in rows):7.4f} {st.mean(f(r, "NumberOfPoints_Simplified") for r in rows):8.1f}')
