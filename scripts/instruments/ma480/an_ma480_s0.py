#!/usr/bin/env python
# (480) 線 ma 段 0 の読み手＝labor_s*.tsv（計器つき 10 種）から、最低賃金法の AI の判断の〈今〉の姿を読む（読むだけ）。
# 使い方：python an_ma480_s0.py <ins のフォルダ> [種の数=10]    例）python an_ma480_s0.py D:/PoB_work/keep_475/m475_ins
# 失業の定義は 1 本＝plainUnemploymentShare（担い手 POP の失業率を人数で重み付け）。縛る行＝enactedNearFloorShare>0.5。
import csv, sys, collections, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
D = sys.argv[1].rstrip('/') + '/'
N = int(sys.argv[2]) if len(sys.argv) > 2 else 10
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
def frac(v, t):
    v = [x for x in v if x == x]
    return (sum(1 for x in v if x > t) / len(v) if v else float('nan'), len(v))
tot = collections.Counter()
for s in range(1, N + 1):
    rows = list(csv.DictReader(open(D + f'labor_s{s}.tsv', encoding='utf-8'), delimiter='\t'))
    enc = [r for r in rows if int(r['lawCode']) > 0]
    bind = [r for r in enc if fl(r['enactedNearFloorShare']) > 0.5]
    u = lambda r: fl(r['plainUnemploymentShare'])
    nat = collections.defaultdict(list)
    for r in rows: nat[r['nation']].append((int(r['quarter']), int(r['lawCode']), int(r['year'])))
    down = up = 0; first = {}; maxlv = 0; pre1800 = 0
    for n, l in nat.items():
        l.sort()
        prev = 0
        for q, lc, y in l:
            if lc < prev: down += 1
            if lc > prev: up += 1
            if lc > 0 and n not in first: first[n] = y
            prev = lc; maxlv = max(maxlv, lc)
    pre1800 = sum(1 for y in first.values() if y < 1800)
    a = frac([u(r) for r in bind], 0.5); b = frac([u(r) for r in enc], 0.5)
    print(f'seed{s}: rows {len(rows)} enactedRows {len(enc)} bindRows {len(bind)} bindU>0.5 {a[0]:.3f} enactedU>0.5 {b[0]:.3f} nationsEnacted {len(first)} firstBefore1800 {pre1800} upSteps {up} downSteps {down} maxLevel {maxlv}')
    tot['rows'] += len(rows); tot['enc'] += len(enc); tot['bind'] += len(bind)
    tot['bindU'] += sum(1 for r in bind if u(r) > 0.5); tot['encU'] += sum(1 for r in enc if u(r) > 0.5)
    tot['nat'] += len(first); tot['down'] += down; tot['up'] += up; tot['pre1800'] += pre1800
print(f"pooled: enactedRows {tot['enc']} bindRows {tot['bind']} bindU>0.5 {tot['bindU']/max(1,tot['bind']):.3f} ({tot['bindU']}/{tot['bind']}) enactedU>0.5 {tot['encU']/max(1,tot['enc']):.3f} nationsEnacted(sum) {tot['nat']} upSteps {tot['up']} downSteps {tot['down']} firstBefore1800 {tot['pre1800']}")
