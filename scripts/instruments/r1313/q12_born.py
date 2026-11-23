# (1313) c136ak 問い 1・2：途中で生まれた国の〈民間の貨幣÷国庫 < 1/1000〉と〈1 人あたり名目 GDP < 同じ四半期の中央値の 1/20〉の国×四半期、
# 該当の国の最初の 20 四半期の国庫の科目ごとの出入り（treasledger_s）。読むだけ。
# 使い方：python -X utf8 -I q12_born.py <出力フォルダ> <種 a-b> 名前=<ins> ...
import sys, os, csv, statistics, collections
out = sys.argv[1]; a, b = map(int, sys.argv[2].split('-'))
arms = [x.split('=', 1) for x in sys.argv[3:]]
os.makedirs(out, exist_ok=True)
POPMIN_MED = 100.0  # 中央値の母集合＝人口 100 千人以上（m1292 と同じ）
F_M = 1e-3; F_G = 1.0 / 20

def fl(x):
    try: return float(x)
    except: return float('nan')

summ = open(os.path.join(out, 'q1_counts.tsv'), 'w', encoding='utf-8', newline='\n')
summ.write('arm\tseed\tbornNations\tbornPop100k\thitM_nq\thitG_nq\thitAny_nq\thitNations\n')
lst = open(os.path.join(out, 'q1_born_list.tsv'), 'w', encoding='utf-8', newline='\n')
lst.write('arm\tseed\tnation\tbornQ\tbornYear\tbornTreasury\tprivMoney0\tpriv0_over_T\tpopK0\tgdpPc0_over_med\tquarters\thitM\thitG\tfirstHitQ\tminPrivOverT\n')
led = open(os.path.join(out, 'q2_ledger20.tsv'), 'w', encoding='utf-8', newline='\n')
led.write('arm\tseed\tnation\tk\tquarter\tyear\titem\tvalue\n')
for name, d in arms:
    for s in range(a, b + 1):
        # nation_s：人口と名目 GDP
        pop = {}; gdp = {}; byq = collections.defaultdict(list)
        with open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            for row in r:
                q = int(row[ix['quarter']]); n = row[ix['nation']]
                p = fl(row[ix['totalPopulationK']]); g = fl(row[ix['nominalGdpPerYear']])
                pop[(q, n)] = p; gdp[(q, n)] = g
                if p >= POPMIN_MED and g == g: byq[q].append(g / p)
        med = {q: statistics.median(v) for q, v in byq.items() if v}
        # treasledger_s
        T = {}; PM = {}; born = {}; births = collections.defaultdict(list); rowsq = collections.defaultdict(list)
        with open(os.path.join(d, f'treasledger_s{s}.tsv'), encoding='utf-8') as f:
            r = csv.reader(f, delimiter='\t'); next(r)
            for q, y, n, it, v in r:
                q = int(q)
                if it == '_treasuryEnd': T[(q, n)] = fl(v)
                elif it == '_privateMoney': PM[(q, n)] = fl(v)
                elif it == '_bornTreasury': births[n].append((q, y, fl(v)))
                rowsq[n].append((q, y, it, v))
        bn = [(n, i) for n, L in births.items() for i in range(len(L)) if L[i][0] > 0]
        hm = hg = ha = 0; hitn = []; nb100 = 0
        for n, bi in bn:
            q0, y0, bt = births[n][bi]
            q1 = births[n][bi + 1][0] if bi + 1 < len(births[n]) else 10**9
            qs = sorted(q for (q, m) in T if m == n and q0 <= q < q1)
            cm = cg = 0; fh = ''; mn = float('inf')
            for q in qs:
                t = T[(q, n)]; pm = PM.get((q, n), float('nan')); p = pop.get((q, n), float('nan')); g = gdp.get((q, n), float('nan'))
                if not (p >= 10.0): continue  # 人口 1 万未満の名残は数えない
                ratio = pm / t if t > 0 else float('inf')
                mn = min(mn, ratio)
                isM = t > 0 and ratio < F_M
                isG = (q in med) and g == g and p > 0 and (g / p) < F_G * med[q]
                cm += isM; cg += isG
                if (isM or isG) and fh == '': fh = q
                if isM or isG: ha += 1
            hm += cm; hg += cg
            p0 = pop.get((q0, n), float('nan')); g0 = gdp.get((q0, n), float('nan'))
            if p0 >= POPMIN_MED: nb100 += 1
            pm0 = PM.get((q0, n), float('nan'))
            gm = (g0 / p0) / med[q0] if (q0 in med and p0 > 0 and g0 == g0) else float('nan')
            lst.write(f'{name}\t{s}\t{n}\t{q0}\t{y0}\t{bt:.6g}\t{pm0:.6g}\t{(pm0/bt if bt>0 else float("inf")):.6g}\t{p0:.6g}\t{gm:.6g}\t{len(qs)}\t{cm}\t{cg}\t{fh}\t{mn:.6g}\n')
            if cm or cg:
                hitn.append(f'{n}#{bi}')
                for (q, y, it, v) in rowsq[n]:
                    if not (q0 <= q < min(q1, q0 + 20)): continue
                    led.write(f'{name}\t{s}\t{n}\t{q - q0}\t{q}\t{y}\t{it}\t{v}\n')
        summ.write(f'{name}\t{s}\t{len(bn)}\t{nb100}\t{hm}\t{hg}\t{ha}\t{"|".join(hitn)}\n')
        summ.flush()
summ.close(); lst.close(); led.close()
print('done', file=open(os.path.join(out, 'q12.done'), 'w'))
