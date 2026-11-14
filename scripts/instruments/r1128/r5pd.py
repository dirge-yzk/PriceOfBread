# (1128) 紙の台本の入力：輸入した州×財の行（sellr_s・lastDemand>0・prevImportAll>0）で
#   (a) 写しの時点の確かめ：(S-TS)/D が 1-P/D に一致する行（xNeed＝TS+D-P-S が縛った直後の形）の割合（数と D の重み）
#   (b) P/D の分布：D の重みで P/D>=0.26・>=0.5・>=0.9 の割合（M4 の超過 E=1-√r+Q/D が正になる条件の下見）
# 使い方: python -X utf8 -I r5pd.py <dir> [年...]
import os, re, sys, math


def fl(x):
    try:
        v = float(x); return v if math.isfinite(v) else float('nan')
    except Exception:
        return float('nan')


d = sys.argv[1]; years = set(sys.argv[2:]) or {'1870', '1913'}
acc = {y: [0, 0.0, 0, 0.0, 0.0, 0.0, 0.0] for y in years}   # 行, D, 一致の行, 一致の D, D(P/D>=.26), D(>=.5), D(>=.9)
for f in sorted(os.listdir(d)):
    if not re.fullmatch(r'sellr_s\d+\.tsv', f): continue
    with open(os.path.join(d, f), encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in fh:
            v = ln.rstrip('\n').split('\t')
            if v[0] not in years: continue
            D = fl(v[ix['lastDemand']]); I = fl(v[ix['prevImportAll']])
            if not (D > 0) or not (I > 0): continue
            S = fl(v[ix['stock']]); T = fl(v[ix['targetStock']]); P = fl(v[ix['lastProduced']])
            r = (S - T) / D; pd = P / D
            a = acc[v[0]]; a[0] += 1; a[1] += D
            if abs(r - (1 - pd)) <= 1e-6 * max(1.0, abs(r)): a[2] += 1; a[3] += D
            if pd >= 0.26: a[4] += D
            if pd >= 0.5: a[5] += D
            if pd >= 0.9: a[6] += D
for y in sorted(acc):
    a = acc[y]
    print('%s 行 %d  (S-TS)/D=1-P/D の行 %.3f（D の重み %.3f）  D の重みで P/D>=0.26 %.3f  >=0.5 %.3f  >=0.9 %.3f' % (
        y, a[0], a[2] / max(1, a[0]), a[3] / max(1e-300, a[1]), a[4] / a[1], a[5] / a[1], a[6] / a[1]))
