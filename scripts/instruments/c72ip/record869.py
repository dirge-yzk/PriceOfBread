# (869) c72ip（(863) の record869.py の写し・計算は 1 字も変えない）記録の台本（門ではない）：同じ盤面の新÷旧＝植民地格の輸入の和・工場の産出・資本家の在庫の中央（(852) の 0.25・0.59・0.72 と並べる）
#   python -I -X utf8 record869.py <旧の ins> <新の ins> [種 1 10]
#   輸入＝cearn_s の植民地格の outImportPerQuarter の和／工場＝cearn_s の植民地格の factoryOutputRefValuePerMonth の和／資本家＝cpop_s の className 資本家・人数 ＞1e-12 の行の savings の中央。
import sys, os, statistics as st
do, dn = sys.argv[1], sys.argv[2]
s0 = int(sys.argv[3]) if len(sys.argv) > 3 else 1; s1 = int(sys.argv[4]) if len(sys.argv) > 4 else 10
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
def read(d):
    imp = fac = 0.0; cap = []; col = 0
    for s in range(s0, s1 + 1):
        tier = {}
        with open(os.path.join(d, f'cearn_s{s}.tsv'), encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            iq, inat, ireg, it, io, ifo = ix['quarter'], ix['nation'], ix['region'], ix['tier'], ix['outImportPerQuarter'], ix['factoryOutputRefValuePerMonth']
            for line in f:
                x = line.rstrip('\n').split('\t')
                if x[it] != 'Colonial': continue
                tier[(x[iq], x[inat], x[ireg])] = 1; col += 1
                a, b = fl(x[io]), fl(x[ifo])
                if a == a: imp += a
                if b == b: fac += b
        with open(os.path.join(d, f'cpop_s{s}.tsv'), encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            iq, inat, ireg, icl, ipk, isv = ix['quarter'], ix['nation'], ix['region'], ix['className'], ix['popK'], ix['savings']
            for line in f:
                x = line.rstrip('\n').split('\t')
                if x[icl] != '資本家' or (x[iq], x[inat], x[ireg]) not in tier: continue
                if fl(x[ipk]) > 1e-12:
                    v = fl(x[isv])
                    if v == v: cap.append(v)
    return imp, fac, (st.median(cap) if cap else float('nan')), len(cap), col
o = read(do); n = read(dn)
print(f'# record869 旧={do} 新={dn} 種 {s0}-{s1}')
print(f'植民地格の州×四半期\t旧 {o[4]}\t新 {n[4]}\t新÷旧 {n[4] / o[4] if o[4] else float("nan"):.4f}')
print(f'植民地格の輸入の和\t旧 {o[0]:.6g}\t新 {n[0]:.6g}\t新÷旧 {n[0] / o[0] if o[0] else float("nan"):.4f}\t（(852) の M÷Z 0.25）')
print(f'植民地格の工場の産出の和\t旧 {o[1]:.6g}\t新 {n[1]:.6g}\t新÷旧 {n[1] / o[1] if o[1] else float("nan"):.4f}\t（(852) の 0.59）')
print(f'資本家の在庫の中央（人の居る行）\t旧 {o[2]:.6g}（{o[3]} 行）\t新 {n[2]:.6g}（{n[3]} 行）\t新÷旧 {n[2] / o[2] if o[2] else float("nan"):.4f}\t（(852) の 0.72）')
