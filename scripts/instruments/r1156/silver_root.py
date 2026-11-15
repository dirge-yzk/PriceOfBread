# (1156) B-1 の根：銀の産地 2 州の四半期の列と世界の銀の産出（読むだけ・コミットしない）
# 使い方: python -X utf8 -I silver_root.py <出力フォルダ>
import sys, os, statistics
OUT = sys.argv[1]
ARM = 'D:/PoB_work/keep_1150_rej/c112t1_arms/out_c112t1{}_10/ins'
ARMS = {'Aa': ARM.format('Aa'), 'Ab': ARM.format('Ab'), 'B': ARM.format('B'),
        'base': 'D:/PoB_work/keep_1150/base_ins', 'n1': 'D:/PoB_work/keep_1150/n1', 'n2': 'D:/PoB_work/keep_1150/n2'}
REG = ['ペルー', 'ヌエバ・エスパーニャ銀地帯']
COLS = ['populationK', 'employmentShare', 'migrationOutKPerQuarter', 'crossBorderOutKPerQuarter',
        'extractionOutputRefValuePerMonth', 'landFactor', 'wageFactor', 'earnedPerMonth', 'extractionActiveCount', 'extractionBankruptCount']
SEEDS = range(1, 11)

def ins_dir(a):
    d = ARMS[a]
    return d if os.path.exists(os.path.join(d, 'region_s1.tsv')) else os.path.join(d, 'ins')

def read_region(a, s):
    p = os.path.join(ins_dir(a), f'region_s{s}.tsv')
    res = {}
    dup = 0
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip('\n').split('\t')
            r = c[ix['region']]
            if r not in REG: continue
            q = int(c[ix['quarter']])
            key = (r, q)
            if key in res: dup += 1; continue
            row = {'year': int(c[ix['year']]), 'nation': c[ix['nation']], 'tier': c[ix['tier']]}
            for k in COLS:
                v = c[ix[k]]
                try: row[k] = float(v)
                except ValueError: row[k] = float('nan')
            res[key] = row
    return res, dup

def read_silver(a, s):
    p = os.path.join(ins_dir(a), f'nationgood_s{s}.tsv')
    prod = {}; px = {}; owner = {}
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip('\n').split('\t')
            if c[ix['good']] != '銀': continue
            y = int(c[ix['year']]); n = c[ix['nation']]
            q = float(c[ix['producedQty']]); pm = float(c[ix['priceMeanGold']])
            prod[y] = prod.get(y, 0.0) + q
            owner[(y, n)] = (q, pm)
    return prod, owner

reg = {}; sil = {}; dups = {}
for a in ARMS:
    for s in SEEDS:
        reg[(a, s)], dups[(a, s)] = read_region(a, s)
        sil[(a, s)] = read_silver(a, s)

L = []
L.append('# (1156) 銀の産地 2 州の列（region_s）と世界の銀の産出（nationgood_s の銀の producedQty の全ての国の和）')
L.append('# 重複の行（州の名×四半期が 2 回以上）: ' + str(sum(dups.values())))
# A) 雇用 0.3 未満の四半期の数・人口の最小÷ピーク
L.append('\n[A] 州×種：雇用 0.3 未満の四半期の数（行の在る四半期だけ・書かない行は数えない）・人口のピークと最後・最小÷ピーク')
L.append('arm\tseed\tregion\tnQ\tempLt03Q\tpopPeak\tpopPeakQ\tpopMin\tpopMinAfterPeakRatio\tpopLast\townerFirst\townerLast')
cnt40 = {}
for a in ARMS:
    for s in SEEDS:
        for r in REG:
            rows = sorted((q, v) for (rr, q), v in reg[(a, s)].items() if rr == r)
            if not rows: L.append(f'{a}\t{s}\t{r}\t0'); continue
            lt = sum(1 for q, v in rows if v['employmentShare'] < 0.3)
            pk_q, pk = max(((q, v['populationK']) for q, v in rows), key=lambda x: x[1])
            mn = min(v['populationK'] for q, v in rows if q >= pk_q)
            L.append(f"{a}\t{s}\t{r}\t{len(rows)}\t{lt}\t{pk:.1f}\t{pk_q}\t{mn:.1f}\t{mn/pk if pk>0 else float('nan'):.4f}\t{rows[-1][1]['populationK']:.1f}\t{rows[0][1]['nation']}\t{rows[-1][1]['nation']}")
            if lt >= 40: cnt40[a] = cnt40.get(a, 0) + 1
L.append('\n[A2] 雇用 0.3 未満が 40 四半期以上の州×種の数（v2 の数＝腕 B 8・Aa 11・基線とヌル腕 0 と並べる）')
for a in ARMS: L.append(f'{a}\t{cnt40.get(a, 0)}')

# B) 世界の銀の産出 腕÷旗 0（10 種の和）
years = [1790, 1795, 1800, 1810, 1830, 1850, 1870, 1890, 1913, 1940]
L.append('\n[B] 世界の銀の産出（10 種の和）と腕÷基線・腕÷ヌル腕の幅')
L.append('year\t' + '\t'.join(f'{a}' for a in ARMS) + '\tAa/base\tAb/base\tB/base\tn1/base\tn2/base')
for y in years:
    tot = {a: sum(sil[(a, s)][0].get(y, 0.0) for s in SEEDS) for a in ARMS}
    b = tot['base'] or float('nan')
    L.append(f'{y}\t' + '\t'.join(f'{tot[a]:.1f}' for a in ARMS) + f"\t{tot['Aa']/b:.3f}\t{tot['Ab']/b:.3f}\t{tot['B']/b:.3f}\t{tot['n1']/b:.3f}\t{tot['n2']/b:.3f}")

# B2) 銀の値段（金建て）の国の中央値の 10 種の中央
L.append('\n[B2] 銀の値段 priceMeanGold（その年の全ての国の中央値・10 種の中央）')
L.append('year\t' + '\t'.join(ARMS))
for y in years:
    vals = []
    for a in ARMS:
        med = []
        for s in SEEDS:
            ps = [pm for (yy, n), (q, pm) in sil[(a, s)][1].items() if yy == y and pm == pm and pm > 0]
            if ps: med.append(statistics.median(ps))
        vals.append(f'{statistics.median(med):.4f}' if med else 'nan')
    L.append(f'{y}\t' + '\t'.join(vals))

# B3) スペインの銀の産出（所有国＝開幕のスペイン）と世界の取り分
L.append('\n[B3] スペインの銀の産出（10 種の和）と世界の取り分')
L.append('year\t' + '\t'.join(f'{a}_qty\t{a}_share' for a in ARMS))
for y in years:
    parts = []
    for a in ARMS:
        sp = sum(sil[(a, s)][1].get((y, 'スペイン'), (0.0, 0))[0] for s in SEEDS)
        tot = sum(sil[(a, s)][0].get(y, 0.0) for s in SEEDS)
        parts.append(f'{sp:.1f}\t{sp/tot if tot>0 else float("nan"):.3f}')
    L.append(f'{y}\t' + '\t'.join(parts))

# C) 外れの最初の時点（旗 0 の帯＝同じ種の base・n1・n2 の最小〜最大の外に 4 四半期続けて）
def band_first(a, s, r, k, run=4):
    qs = sorted(q for (rr, q) in reg[(a, s)] if rr == r)
    streak = 0; first = None
    for q in qs:
        v = reg[(a, s)][(r, q)][k]
        ref = [reg[(b, s)].get((r, q), {}).get(k) for b in ('base', 'n1', 'n2')]
        ref = [x for x in ref if x is not None and x == x]
        if len(ref) < 3 or v != v: streak = 0; continue
        lo, hi = min(ref), max(ref)
        out = v < lo or v > hi
        if out:
            streak += 1
            if streak == run: first = q - run + 1; return first, ('上' if v > hi else '下')
        else: streak = 0
    return None, ''

def silver_first(a, s, run=1):
    for y in sorted(sil[(a, s)][0]):
        v = sil[(a, s)][0][y]
        ref = [sil[(b, s)][0].get(y) for b in ('base', 'n1', 'n2')]
        if any(x is None for x in ref): continue
        if v < min(ref) or v > max(ref): return y, ('上' if v > max(ref) else '下')
    return None, ''

def spain_px_first(a, s):
    for y in sorted(sil[(a, s)][0]):
        v = sil[(a, s)][1].get((y, 'スペイン'))
        ref = [sil[(b, s)][1].get((y, 'スペイン')) for b in ('base', 'n1', 'n2')]
        if v is None or any(x is None for x in ref): continue
        v = v[1]; ref = [x[1] for x in ref]
        if v < min(ref) or v > max(ref): return y, ('上' if v > max(ref) else '下')
    return None, ''

L.append('\n[C] 外れの最初の時点（四半期の列は 4 四半期続けて帯の外の最初の四半期とその年・年の列は帯の外の最初の年）')
L.append('arm\tseed\tregion\tsilverWorldY\tspainSilverPxY\t' + '\t'.join(f'{k}Q' for k in ['employmentShare', 'extractionOutputRefValuePerMonth', 'populationK', 'migrationOutKPerQuarter', 'wageFactor', 'landFactor']))
for a in ('Aa', 'Ab', 'B'):
    for s in SEEDS:
        sy, sd = silver_first(a, s); py, pd = spain_px_first(a, s)
        for r in REG:
            parts = []
            for k in ['employmentShare', 'extractionOutputRefValuePerMonth', 'populationK', 'migrationOutKPerQuarter', 'wageFactor', 'landFactor']:
                q, d = band_first(a, s, r, k)
                if q is None: parts.append('-')
                else:
                    yr = reg[(a, s)][(r, q)]['year']
                    parts.append(f'{q}({yr}){d}')
            L.append(f'{a}\t{s}\t{r}\t{sy}{sd}\t{py}{pd}\t' + '\t'.join(parts))

# D) 開幕の 12 四半期の州の列（種 8 の Aa と base）
L.append('\n[D] 開幕の 12 四半期（種 4 と 8・Aa と base と n1）：emp・pop・extractionOutputRef・migrationOut')
for s in (4, 8):
    for r in REG:
        for a in ('Aa', 'base', 'n1', 'B', 'Ab'):
            seq = []
            for q in range(0, 12):
                v = reg[(a, s)].get((r, q))
                if v is None: seq.append('-'); continue
                seq.append(f"{v['employmentShare']:.3f}/{v['populationK']:.0f}/{v['extractionOutputRefValuePerMonth']:.1f}/{v['migrationOutKPerQuarter']:.2f}")
            L.append(f's{s}\t{r}\t{a}\t' + ' '.join(seq))

with open(os.path.join(OUT, 'silver_root.txt'), 'w', encoding='utf-8', newline='\n') as f:
    f.write('\n'.join(L) + '\n')
print('ok', len(L))
