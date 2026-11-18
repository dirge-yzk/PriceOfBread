# (1204) c120mb 設計の材料：旗 0 の生の mintreg_s（州×四半期）と metal_s・money_s を読む。エンジンを回さない。
# 使い方: python -X utf8 -I mr1204.py <ins フォルダ> <出力>
import sys, os, glob
src, out = sys.argv[1], sys.argv[2]
def era(y):
    return '1783-1829' if y < 1830 else '1830-1869' if y < 1870 else '1870-1913' if y < 1914 else '1914-1950'
ERAS = ['1783-1829', '1830-1869', '1870-1913', '1914-1950']
L = [f'# src {src}']
acc = {}  # (era, metal, 置き場) -> qty
pay = {e: [0, 0, 0] for e in ERAS}  # wouldBuyM, boughtM, notPaidM
regs = {}  # (seed, nation) -> set of regions that bought
def A(k, v): acc[k] = acc.get(k, 0.0) + v
for f in sorted(glob.glob(os.path.join(src, 'mintcap_s*.tsv'))):
    seed = f.rsplit('_s', 1)[1].split('.')[0]
    cap = {}
    with open(f, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fh:
            c = line.rstrip('\n').split('\t')
            cap[(c[ix['year']], c[ix['q']], c[ix['nation']])] = c[ix['capRegion']]
    fr = os.path.join(src, f'mintreg_s{seed}.tsv')
    with open(fr, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fh:
            c = line.rstrip('\n').split('\t')
            e = era(int(c[ix['year']]))
            for k, j in (('wouldBuyM', 0), ('boughtM', 1), ('notPaidM', 2)): pay[e][j] += int(c[ix[k]])
            qa, qu = float(c[ix['qtyAg']]), float(c[ix['qtyAu']])
            if not (qa > 0 or qu > 0): continue
            isCap = cap.get((c[ix['year']], c[ix['q']], c[ix['nation']])) == c[ix['region']]
            for metal, qq, pk in (('Ag', qa, 'prodAg'), ('Au', qu, 'prodAu')):
                if not (qq > 0): continue
                p = float(c[ix[pk]])
                where = 'cap' if isCap else ('prod' if p > 0 else 'nonprod')
                A((e, metal, where), qq); A(('all', metal, where), qq)
                regs.setdefault((seed, c[ix['nation']]), set()).add(c[ix['region']])
for metal in ('Ag', 'Au'):
    for e in ERAS + ['all']:
        t = sum(acc.get((e, metal, w), 0.0) for w in ('cap', 'prod', 'nonprod'))
        if t <= 0: continue
        L.append(f"{metal} {e} 買い {t:.4g} 首都 {acc.get((e,metal,'cap'),0)/t:.4f} 産地(首都の外) {acc.get((e,metal,'prod'),0)/t:.4f} 産まない州(首都の外) {acc.get((e,metal,'nonprod'),0)/t:.4f}")
for e in ERAS:
    w, b, n = pay[e]
    L.append(f"支払い {e} wouldBuyM {w} boughtM {b} notPaidM {n} notPaid/wouldBuy {n/max(1,w):.4f}")
cnt = sorted(len(s) for s in regs.values())
L.append(f"国×種ごとの買った州の数 n {len(cnt)} p50 {cnt[len(cnt)//2]} p90 {cnt[int(.9*len(cnt))]} max {cnt[-1]}")
# 世界の産出と造幣の比（metal_s：四半期の行・mintcap の買いの四半期の和と同じ刻み）
prod = {}; mint = {}
for f in sorted(glob.glob(os.path.join(src, 'metal_s*.tsv'))):
    with open(f, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fh:
            c = line.rstrip('\n').split('\t'); e = era(int(c[ix['year']]))
            for k in ('goldProduced', 'silverProduced'):
                v = float(c[ix[k]]); prod[(e, k)] = prod.get((e, k), 0.0) + (v if v == v else 0.0)
for metal, k in (('Ag', 'silverProduced'), ('Au', 'goldProduced')):
    for e in ERAS:
        t = sum(acc.get((e, metal, w), 0.0) for w in ('cap', 'prod', 'nonprod'))
        L.append(f"造幣÷世界の産出 {metal} {e} 造幣 {t:.4g} 産出 {prod.get((e,k),0):.4g} 比 {t/prod[(e,k)] if prod.get((e,k),0)>0 else float('nan'):.3f}")
# money_s：造幣の累計÷広義の貨幣（主要国・最後の行）
for f in sorted(glob.glob(os.path.join(src, 'money_s*.tsv')))[:3]:
    last = {}
    with open(f, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fh:
            c = line.rstrip('\n').split('\t')
            y = int(c[ix['year']])
            if y in (1829, 1869, 1913, 1950): last[(c[ix['nation']], y)] = (float(c[ix['cumulativeGoldMintedStock']]), float(c[ix['broadMoneyStock']]), float(c[ix['baseMoneyStock']]), float(c[ix['outstandingLoansStock']]), float(c[ix['treasuryStock']]))
    for (n, y), (cm, bm, base, ln, tr) in sorted(last.items()):
        L.append(f"money {os.path.basename(f)} {n} {y} 造幣の累計÷広義 {cm/bm if bm>0 else float('nan'):.4f} 被覆 {base/bm if bm>0 else float('nan'):.4f} 貸出÷広義 {ln/bm if bm>0 else float('nan'):.4f} 国庫÷広義 {tr/bm if bm>0 else float('nan'):.4f}")
with open(out, 'w', encoding='utf-8', newline='\n') as w: w.write('\n'.join(L) + '\n')
print('\n'.join(L[:40]))
