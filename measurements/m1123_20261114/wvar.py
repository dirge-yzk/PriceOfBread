# (1123) 紙：Vb の重みの比べ（keep_1122/base_ins の旗 0 の盤面・配りの表の四半期 0,40,…）
# 使い方: python wvar.py <生のフォルダ> <出力>
# 形: Va w=Y*rho*(1-s)*(1-b) F=alloc*rho / Vb w=Y*(1-s)*(1-b) F=alloc*rho / Vw w=Y*rho*(1-s)*(1-b) F=alloc
#     Vd: Vb で候補を ②③ だけ（candKind 1＝取り分の州を外す・近似） / Vn: w=Y*rho_adj*(1-s)*(1-b)（rho_adj＝陸の隣か交戦〔kind 2,3〕だけ 1・他は rho）F=alloc*rho
# 数: (A) 交戦していない持ち主の中核の州で外国の目標 ≧0.25 の州×四半期 (B) 四半期 0 で外国が最大の区分の州（10 種の和）
#     (C) 交戦していない持ち主の中核の州へ流れる配りの割合（O の和に対して）
import sys, os, csv, collections
raw, out = sys.argv[1], sys.argv[2]
V = ['Va', 'Vb', 'Vw', 'Vd', 'Vn']
A = collections.Counter(); B = collections.Counter(); Cn = collections.Counter(); Cd = collections.Counter(); rows = collections.Counter()
def f(x):
    try: return float(x)
    except: return float('nan')
for s in range(1, 11):
    al = os.path.join(raw, f'rulepoweralloc_s{s}.tsv'); rp = os.path.join(raw, f'rulepower_s{s}.tsv')
    if not os.path.exists(al): continue
    cand = collections.defaultdict(list); qs = set()
    with open(al, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in fh:
            c = ln.rstrip('\n').split('\t'); q = int(c[0]); qs.add(q)
            cand[(q, c[ix['nation']])].append((c[ix['region']], int(c[ix['candKind']]), f(c[ix['yIndex']]), f(c[ix['rhoF']]), f(c[ix['sShare']]), f(c[ix['bShare']]), f(c[ix['allocIndex']])))
    reg = {}
    with open(rp, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        need = ['tier', 'wNationIndex', 'fStateIndex', 'fInterIndex', 'fResistIndex', 'fUngovIndex', 'fForeignSumIndex', 'ownerSideShare', 'localSideShare', 'ungovSideShare']
        for ln in fh:
            c = ln.split('\t', 2)
            if int(c[0]) not in qs: continue
            c = ln.rstrip('\n').split('\t')
            reg[(int(c[0]), c[ix['region']])] = {k: (c[ix[k]] if k == 'tier' else f(c[ix[k]])) for k in need} | {'owner': c[ix['nation']]}
    for v in V:
        ff = collections.defaultdict(float)
        for (q, n), lst in cand.items():
            O = sum(x[6] for x in lst)  # Va の配りの和＝O（配り切る）
            ws = []
            for (r, k, y, rho, sh, b, a) in lst:
                if v == 'Va' or v == 'Vw': w = y * rho * (1 - min(1, sh)) * (1 - b)
                elif v == 'Vn': w = y * (1 if k in (2, 3) else rho) * (1 - min(1, sh)) * (1 - b)
                else: w = y * (1 - min(1, sh)) * (1 - b)
                if v == 'Vd' and k == 1: w = 0
                ws.append(max(0, w))
            sw = sum(ws)
            for (r, k, y, rho, sh, b, a), w in zip(lst, ws):
                al_ = O * w / sw if sw > 0 else 0
                F = al_ if v == 'Vw' else al_ * rho
                ff[(q, r)] += F
                g = reg.get((q, r))
                if g and g['tier'] == 'Core' and g['wNationIndex'] == 0: Cn[v] += al_
                Cd[v] += al_
        for (q, r), g in reg.items():
            other = g['fStateIndex'] + g['fInterIndex'] + g['fResistIndex'] + g['fUngovIndex']
            F = ff.get((q, r), 0.0); tot = other + F
            if not (tot > 0): continue
            if v == 'Va': rows[q] += 1
            if g['tier'] == 'Core' and g['wNationIndex'] == 0 and F / tot >= 0.25: A[v] += 1
            if q == 0:
                ts = other + g['fForeignSumIndex']
                own, loc, ung = g['ownerSideShare'] * ts, g['localSideShare'] * ts, g['ungovSideShare'] * ts
                if F > max(own, loc, ung) * (1 + 1e-9): B[v] += 1
with open(out, 'w', encoding='utf-8', newline='\n') as o:
    o.write('# 紙の重みの比べ（keep_1122/base_ins・旗 0 の盤面の静的な目標・配りの表の四半期）\n')
    o.write('形\tA_中核の外国の目標≧0.25（州×四半期）\tB_四半期0で外国が最大（10種の和）\tC_交戦していない中核への配りの割合\n')
    for v in V: o.write(f'{v}\t{A[v]}\t{B[v]}\t{Cn[v]/Cd[v] if Cd[v] else float("nan"):.4f}\n')
    o.write(f'# 四半期ごとの行の数 {dict(sorted(rows.items()))}\n')
print(open(out, encoding='utf-8').read())
