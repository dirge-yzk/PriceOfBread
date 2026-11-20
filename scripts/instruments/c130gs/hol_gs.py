# (1271) c130gs 空洞化 T1 の件ごとの振り分け（事前登録〈門 1 空洞化 T1〉(a)(b)(c) のまま）。
#   an_h2.py -v4 の出力から〈ピークの 1/10 未満〉の件（名簿の外）を拾い、件ごとに
#   (a) 基準の同じ種・同じ州が（ピークの後に）ピークの 1/4 未満 →〈元からの弱さ〉
#   (b) 腕のピークの四半期を含む前の 8 四半期の garrisonInK の和 ≧（ピーク−8 四半期前の人口）×1/2 かつ 腕のピークの後の最小 ≧ 8 四半期前の人口×1/4 →〈膨らんでから抜ける〉
#   (c) それ以外 →〈線が原因〉
#   州の人口は region_s の populationK（国の名で分けず州の名で合わせる＝持ち主が変わっても同じ州）。
#   使い方: python -X utf8 -I hol_gs.py <腕の hol_v4.txt> <腕の ins> <基準の ins>
import sys, os, re
from collections import defaultdict
HOL, ARM, BASE = sys.argv[1], sys.argv[2], sys.argv[3]
cases = []
for line in open(HOL, encoding='utf-8'):
    m = re.match(r'\s*\[([^\]]*)\]\s+s(\d+)\s+(\S+?)（', line)
    if not m: continue
    tag = m.group(1)
    if '1/10' not in tag or '例外の名簿' in tag: continue
    cases.append((int(m.group(2)), m.group(3), tag))

def series(ins, s, region):
    p = os.path.join(ins, f'region_s{s}.tsv'); out = defaultdict(float)
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip('\n').split('\t')
            if c[ix['region']] != region: continue
            try: out[int(c[ix['quarter']])] += float(c[ix['populationK']])
            except ValueError: pass
    return dict(sorted(out.items()))

def garr(ins, s, region):
    p = os.path.join(ins, f'popclose_s{s}.tsv'); out = defaultdict(float)
    if not os.path.exists(p): return out
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip('\n').split('\t')
            if c[ix['region']] != region: continue
            try: out[int(c[ix['quarter']])] += float(c[ix['garrisonInK']])
            except ValueError: pass
    return out

print(f'件 {len(cases)}（名簿の外の 1/10 未満）')
for s, reg, tag in cases:
    a = series(ARM, s, reg); b = series(BASE, s, reg)
    if not a: print(f'  s{s} {reg}: 腕の系列なし'); continue
    qp = max(a, key=lambda q: a[q]); peak = a[qp]
    low = min(v for q, v in a.items() if q >= qp)
    kind = None
    if b:
        qb = max(b, key=lambda q: b[q]); lb = min(v for q, v in b.items() if q >= qb)
        if b[qb] > 0 and lb < b[qb] / 4: kind = f'(a) 元からの弱さ（基準 {b[qb]:.1f}→{lb:.1f}）'
    if kind is None:
        g = garr(ARM, s, reg); q0 = qp - 8
        pre = a.get(q0, next(iter(a.values())))
        gin = sum(v for q, v in g.items() if q0 < q <= qp)
        if gin >= (peak - pre) / 2 and peak > pre and low >= pre / 4:
            kind = f'(b) 膨らんでから抜ける（8 四半期前 {pre:.1f}・garrisonIn {gin:.1f}）'
        else:
            kind = f'(c) 線が原因（8 四半期前 {pre:.1f}・garrisonIn {gin:.1f}）'
    print(f'  s{s} {reg} [{tag}] 腕 ピーク {peak:.1f}(q{qp})→最小 {low:.1f}：{kind}')
