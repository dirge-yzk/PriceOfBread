# (1047) c99ov 門の台本（紙の台本と腕 B の門の読み手を兼ねる・読むだけ）
# 使い方: python -X utf8 gates_ov.py <出力> 名前=insフォルダ ...（ヌル腕の名は base n1 n2・それ以外の最初の名前が腕）
#   例（紙）: python gates_ov.py paper.txt base=D:/PoB_work/keep_1044/base_ins n1=D:/PoB_work/keep_1044/n1 n2=D:/PoB_work/keep_1044/n2
#   例（腕）: python gates_ov.py gates.txt B=<腕>/ins base=... n1=... n2=...
# 読む表：nationgood_s（越境・1913）・sellr_s（州の外の全て・U2・輸入で埋まる州）・fd_s（州の主食の充足 fillNew）。
# 定義（事前登録 measurements/m1047_20261111_prereg_ov.txt [3]）：
#   U1a 越境：6 大国の Σ crossExportQty×px ÷ Σ producedQty×px（1913・px＝nationgood の priceMeanGold）。種ごと→10 種の中央値。
#   U1b 州の外の全て：sellr の 1913 の行を（州・財）で 1 つにし、6 大国の Σ prevExportAll×px ÷ Σ lastProduced×px（px は同じ国・財の nationgood）。
#   U2 同じ盤面：rentR＞0・laborWeight＞0・sellROld＞0 の行で、上がった行（sellRBoth÷sellROld ≧1.5）と下がった行（≦1/1.5）それぞれで、
#      |ln(lw÷(rentR×sellRBoth))| ＜ |ln(lw÷(rentR×sellROld))| の割合（全ての年・全ての州・10 種の和）。
#   U3a 輸入で埋まる州（6 大国・1913・主食 7 財の prevImportAll の和 ≧ 主食の lastDemand の和×0.05）の人口の重みの (1−fillNew)。種ごと。
import sys, os, math, statistics as st
from collections import defaultdict
MAJ = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'アメリカ', '清']
STAPLE = {'ジャガイモ', 'トウモロコシ', 'パン', '米', '野菜', '麦', '小麦粉'}
UP, DN = 1.5, 1 / 1.5
NULLS = ('base', 'n1', 'n2')
OUT = sys.argv[1]; ARMS = [a.split('=', 1) for a in sys.argv[2:]]


def seeds(d, t):
    return [s for s in range(1, 11) if os.path.exists(os.path.join(d, '%s_s%d.tsv' % (t, s)))]


def one(d, s):
    r = {}
    px = {}; P = E = 0.0
    with open(os.path.join(d, 'nationgood_s%d.tsv' % s), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in f:
            v = ln.rstrip('\n').split('\t')
            if v[0] != '1913': continue
            p = float(v[ix['priceMeanGold']])
            if not (p > 0): continue
            px[(v[1], v[2])] = p
            if v[1] in MAJ:
                P += float(v[ix['producedQty']]) * p; E += float(v[ix['crossExportQty']]) * p
    r['U1a'] = E / P if P > 0 else float('nan')
    seen = set(); P2 = E2 = 0.0; q1913 = None
    imp = defaultdict(float); dem = defaultdict(float)
    up = [0, 0]; dn = [0, 0]; nonfin = 0
    with open(os.path.join(d, 'sellr_s%d.tsv' % s), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        iy, iq, ir, inat, ig = ix['year'], ix['q'], ix['region'], ix['ownerNation'], ix['good']
        iL, iE, iI, iP = ix['lastDemand'], ix['prevExportAll'], ix['prevImportAll'], ix['lastProduced']
        iw, irr, iso, isb = ix['laborWeight'], ix['rentR'], ix['sellROld'], ix['sellRBoth']
        for ln in f:
            v = ln.rstrip('\n').split('\t')
            lw = float(v[iw]); rr = float(v[irr]); so = float(v[iso]); sb = float(v[isb])
            if not (math.isfinite(lw) and math.isfinite(so) and math.isfinite(sb)): nonfin += 1
            if rr > 0 and lw > 0 and so > 0 and sb > 0:
                k = sb / so
                if k >= UP or k <= DN:
                    c = abs(math.log(lw / (rr * sb))) < abs(math.log(lw / (rr * so)))
                    t = up if k >= UP else dn
                    t[0] += 1; t[1] += c
            if v[iy] != '1913': continue
            q1913 = v[iq]
            key = (v[ir], v[ig])
            if key in seen: continue
            seen.add(key)
            nat = v[inat]
            if nat in MAJ:
                p = px.get((nat, v[ig]))
                if p:
                    P2 += float(v[iP]) * p; E2 += float(v[iE]) * p
                if v[ig] in STAPLE:
                    imp[(nat, v[ir])] += float(v[iI]); dem[(nat, v[ir])] += float(v[iL])
    r['U1b'] = E2 / P2 if P2 > 0 else float('nan')
    r['U2upN'], r['U2up'] = up[0], up[1]; r['U2dnN'], r['U2dn'] = dn[0], dn[1]; r['nonfin'] = nonfin
    filled = {k for k in imp if dem[k] > 0 and imp[k] >= 0.05 * dem[k]}
    num = den = 0.0; nreg = 0
    with open(os.path.join(d, 'fd_s%d.tsv' % s), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in f:
            v = ln.rstrip('\n').split('\t')
            if v[ix['quarter']] != q1913 or (v[ix['nation']], v[ix['region']]) not in filled: continue
            pk = float(v[ix['popK']]); fl = float(v[ix['fillNew']])
            if pk > 0 and math.isfinite(fl):
                num += pk * max(0.0, 1 - fl); den += pk; nreg += 1
    r['U3a'] = num / den if den > 0 else float('nan'); r['U3aRegions'] = nreg
    return r


res = {}
for name, d in ARMS:
    res[name] = {s: one(d, s) for s in seeds(d, 'sellr')}


def med(name, k):
    L = [x[k] for x in res[name].values() if x[k] == x[k]]
    return st.median(L) if L else float('nan')


o = ['# (1047) c99ov gates_ov.py ' + ' '.join('%s=%s' % (a, b) for a, b in ARMS)]
for name in res:
    o.append('== %s（種 %s）' % (name, sorted(res[name])))
    for s in sorted(res[name]):
        x = res[name][s]
        o.append('  種 %d U1a %.5g U1b %.5g U2up %d/%d=%.4f U2dn %d/%d=%.4f U3a %.5g（州 %d）非有限 %d' % (
            s, x['U1a'], x['U1b'], x['U2up'], x['U2upN'], x['U2up'] / max(1, x['U2upN']), x['U2dn'], x['U2dnN'],
            x['U2dn'] / max(1, x['U2dnN']), x['U3a'], x['U3aRegions'], x['nonfin']))
    tu = sum(x['U2up'] for x in res[name].values()); nu = sum(x['U2upN'] for x in res[name].values())
    td = sum(x['U2dn'] for x in res[name].values()); nd = sum(x['U2dnN'] for x in res[name].values())
    o.append('  中央 U1a %.5g U1b %.5g U3a %.5g ／ U2 和 up %d/%d=%.4f dn %d/%d=%.4f ／ 種ごとの最小 N up %d dn %d' % (
        med(name, 'U1a'), med(name, 'U1b'), med(name, 'U3a'), tu, nu, tu / max(1, nu), td, nd, td / max(1, nd),
        min(x['U2upN'] for x in res[name].values()), min(x['U2dnN'] for x in res[name].values())))
have = all(n in res for n in NULLS)
if have:
    o.append('== ヌル腕の幅（÷base・中央値の比）')
    for k in ('U1a', 'U1b'):
        r1 = med('n1', k) / med('base', k); r2 = med('n2', k) / med('base', k)
        o.append('  %s n1÷base %.4f n2÷base %.4f 幅の上端 %.4f' % (k, r1, r2, max(r1, r2, 1 / r1, 1 / r2)))
    o.append('== U3a 一つ抜き（ヌル腕の 1 本を腕と見なし、同じ種で他の 2 本の最大を超えた種の数）')
    for hname in NULLS:
        oth = [n for n in NULLS if n != hname]
        cnt = sum(1 for s in res[hname] if all(s in res[n] for n in oth) and res[hname][s]['U3a'] > max(res[n][s]['U3a'] for n in oth))
        o.append('  %s を腕に：%d/%d' % (hname, cnt, len(res[hname])))
arms = [n for n in res if n not in NULLS]
if arms and have:
    arm = arms[0]
    o.append('== 門（腕 %s）' % arm)
    for k in ('U1a', 'U1b'):
        r1 = med('n1', k) / med('base', k); r2 = med('n2', k) / med('base', k); wid = max(r1, r2, 1 / r1, 1 / r2)
        ratio = med(arm, k) / med('base', k)
        o.append('  %s 新÷旧 %.4f 下限（幅の上端）%.4f → %s ／ U3b 上限 10 → %s' % (k, ratio, wid, '緑' if ratio > wid else '赤', '緑' if ratio <= 10 else '赤'))
    for t in ('up', 'dn'):
        T = sum(x['U2' + t] for x in res[arm].values()); N = sum(x['U2' + t + 'N'] for x in res[arm].values())
        o.append('  U2%s %d/%d=%.4f（下限 0.5・N の床は事前登録）' % (t, T, N, T / max(1, N)))
    cnt = sum(1 for s in res[arm] if all(s in res[n] for n in NULLS) and res[arm][s]['U3a'] > max(res[n][s]['U3a'] for n in NULLS))
    o.append('  U3a 同じ種で 3 本のヌルの最大を超えた種 %d/%d（上限は事前登録）' % (cnt, len(res[arm])))
with open(OUT, 'w', encoding='utf-8', newline='\n') as f:
    f.write('\n'.join(o) + '\n')
print('\n'.join(o))
