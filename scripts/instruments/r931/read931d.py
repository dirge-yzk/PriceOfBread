# (931) 読むだけ：0 の続きの州×四半期で、採取の持ち主（landbasereg_s の regionRgoCount・regionRgoOwnerEmpty・regionAbsenteeRgoCount）と
#   輸出の受け取り（cearn_s の inExportPerQuarter）と人頭税の請求÷稼ぎを、0 でない植民地格の州（人口 100 以上）と並べる。区間の定義は read931a.py と同じ。
#   使い方: python -X utf8 -I read931d.py <ins フォルダ> <種の起点> <名札>
import sys, os, statistics as st, collections as C
M = sys.argv[1]; SB = int(sys.argv[2]); TAG = sys.argv[3]
POP = 100.0; K = 4
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float('nan')
A = C.defaultdict(list); R = C.defaultdict(list); nz = C.Counter(); nr = C.Counter()
for s in range(SB, SB + 10):
    f = open(os.path.join(M, f'region_s{s}.tsv'), encoding='utf-8'); h = f.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(h)}
    d = C.defaultdict(dict)
    for l in f:
        r = l.rstrip('\n').split('\t')
        if len(r) < len(h): continue
        d[r[ix['region']]][int(r[ix['quarter']])] = (fl(r[ix['factoryOutputRefValuePerMonth']]) + fl(r[ix['extractionOutputRefValuePerMonth']]), fl(r[ix['populationK']]), r[ix['tier']])
    f.close()
    zk = set(); rk = set()
    for reg, dd in d.items():
        run = []
        def close(run):
            if len(run) >= K: zk.update((reg, q) for q in run)
        for q in sorted(dd):
            v = dd[q]
            if v[0] == 0.0 and v[1] >= POP:
                if run and run[-1] == q - 1: run.append(q)
                else: close(run); run = [q]
            else:
                close(run); run = []
                if v[2] == 'Colonial' and v[1] >= POP and v[0] > 0: rk.add((reg, q))
        close(run)
    del d
    p = os.path.join(M, f'landbasereg_s{s}.tsv')
    if os.path.exists(p):
        f = open(p, encoding='utf-8'); h = f.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(h)}
        for l in f:
            r = l.rstrip('\n').split('\t')
            if len(r) < len(h): continue
            k = (r[ix['region']], int(r[ix['quarter']]))
            t = A if k in zk else (R if k in rk else None)
            if t is None: continue
            (nz if t is A else nr)['landbasereg'] += 1
            for c in ['regionRgoCount', 'regionRgoOwnerEmpty', 'regionAbsenteeRgoCount', 'regionZeroBaseFlag', 'regionSubValuePerMonth', 'regionPayersWithRent']:
                t[c].append(fl(r[ix[c]]))
            n_, e_ = fl(r[ix['regionRgoCount']]), fl(r[ix['regionRgoOwnerEmpty']])
            t['ownerEmptyShare'].append(e_ / n_ if n_ > 0 else float('nan'))
            a_ = fl(r[ix['regionAbsenteeRgoCount']]); t['absenteeShare'].append(a_ / n_ if n_ > 0 else float('nan'))
        f.close()
    p = os.path.join(M, f'cearn_s{s}.tsv')
    if os.path.exists(p):
        f = open(p, encoding='utf-8'); h = f.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(h)}
        for l in f:
            r = l.rstrip('\n').split('\t')
            if len(r) < len(h): continue
            k = (r[ix['region']], int(r[ix['quarter']]))
            t = A if k in zk else (R if k in rk else None)
            if t is None: continue
            pop = fl(r[ix['popK']])
            if not pop > 0: continue
            t['inExportPerCapPerQ'].append(fl(r[ix['inExportPerQuarter']]) / pop)
            t['inExportPositive'].append(1.0 if fl(r[ix['inExportPerQuarter']]) > 0 else 0.0)
            e = fl(r[ix['earnedGrossPerMonth']]); pc = fl(r[ix['pollClaimPerMonth']])
            t['pollClaimOverEarned'].append(pc / e if e > 0 else float('nan'))
            t['pollTaxLevel'].append(fl(r[ix['pollTaxLevel']])); t['taxRate'].append(fl(r[ix['taxRate']]))
            t['pollClaimPerCapPerMonth'].append(pc / pop)
            t['importPaidPerCapPerQ'].append(fl(r[ix['importPaidPerQuarter']]) / pop)
            t['estateDuePerQ'].append(fl(r[ix['estateDuePerQuarter']]))
        f.close()
o = [f'== {TAG}  {M}', f'行の数：0 の続き {dict(nz)}・0 でない植民地格 {dict(nr)}', '欄\t0 の続きの中央 (平均)\t0 でない植民地格の中央 (平均)']
for c in ['regionRgoCount', 'regionRgoOwnerEmpty', 'ownerEmptyShare', 'regionAbsenteeRgoCount', 'absenteeShare', 'regionZeroBaseFlag', 'regionSubValuePerMonth', 'regionPayersWithRent',
          'inExportPerCapPerQ', 'inExportPositive', 'pollClaimOverEarned', 'pollClaimPerCapPerMonth', 'pollTaxLevel', 'taxRate', 'importPaidPerCapPerQ', 'estateDuePerQ']:
    a = [x for x in A.get(c, []) if x == x]; b = [x for x in R.get(c, []) if x == x]
    o.append(f'{c}\t{med(a):.4g} ({(sum(a) / len(a)) if a else float("nan"):.4g}, n={len(a)})\t{med(b):.4g} ({(sum(b) / len(b)) if b else float("nan"):.4g}, n={len(b)})')
open(os.path.join(os.path.dirname(os.path.abspath(__file__)), f'read931d_{TAG}_out.txt'), 'w', encoding='utf-8').write('\n'.join(o) + '\n')
print('\n'.join(o))
