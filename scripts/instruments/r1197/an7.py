# (1197) c119t2：鉱脈の 3 つの形（案 S・2 番目の案・第 3 の形）を同じ物差しで静的に推す。
# 読むだけ・エンジンの関数を呼ばない。生＝prodcap_s（州×財×生産者の年末の断面）・site_s（開いた月）・mintreg_s（エンジンの州の品位＝突き合わせ）。
# 静的＝産出の道は基線（品位がほぼ 0.15 の世界）のまま＝品位が上がると産出が増える反作用は入れない（床に届くのは実際にはもっと早い側〈推〉）。
# 呼び方：python -X utf8 -I an7.py <生のフォルダ> <種の始め> <種の終わり> <出力>
import csv, collections, sys, math
csv.field_size_limit(10**9)
MIN = ['鉄鉱石','石炭','銅鉱','鉛鉱','金','硫黄','石灰石','珪砂','粘土','原油','クロム','錫鉱','硝石','銀']
UBI = {'石灰石','珪砂','粘土'}
FLOOR, KNEE, BON = 0.15, 0.5, 60.0
YS = [1820, 1850, 1880, 1913, 1950]
GOODS = ['石炭','鉄鉱石','銅鉱','錫鉱','鉛鉱','金','銀']

def gk(f): return 1.0 if f >= KNEE else FLOOR + (1-FLOOR)*max(0.0, f)/KNEE
def gp(f): return FLOOR + (1-FLOOR)*min(1.0, max(0.0, f))

def load(raw, s):
    snaps = collections.OrderedDict(); owner = {}; qof = {}
    with open(f'{raw}/prodcap_s{s}.tsv', encoding='utf-8', newline='') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k:i for i,k in enumerate(h)}
        for r in rd:
            g = r[ix['good']]
            if g not in MIN: continue
            y = int(r[ix['year']]); reg = r[ix['region']]; site = int(r[ix['siteRow']]) >= 0
            try: o = float(r[ix['output']])
            except ValueError: o = 0.0
            if not (o > 0): o = 0.0
            snaps.setdefault(y, collections.Counter())[(reg, g, site)] += o
            owner[(y, reg)] = r[ix['ownerNation']]; qof[y] = int(r[ix['q']])
    opens = collections.defaultdict(list)
    with open(f'{raw}/site_s{s}.tsv', encoding='utf-8', newline='') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k:i for i,k in enumerate(h)}
        for r in rd:
            if r[ix['kind']] == 'O' and r[ix['siteKind']] == 'D':
                opens[r[ix['region']]].append(1783 + int(r[ix['quarter']])/4.0)
    return snaps, owner, qof, opens

def run(snaps, opens, unit, years, gf, site_years=None, exclude_ubi=False):
    """unit='region'（州に 1 本・合算）か 'prod'（州×財×産地の別＝生産者ごと）。site_years＝産地の生産者だけの年数（None＝同じ年数）。
    戻り値：year -> key(reg,g,site) -> 品位（その鍵の生産者に掛かる品位）"""
    ys = list(snaps.keys()); st = {}; res = {}
    for k, y in enumerate(ys):
        cur = snaps[y]; prev = snaps[ys[k-1]] if k else {}
        months = (y - ys[k-1])*12 if k else 0
        keys = set(cur) | set(prev)
        def ukey(key):
            reg, g, site = key
            if exclude_ubi and g in UBI: return None
            return reg if unit == 'region' else key
        # 減り
        if months > 0:
            for key in keys:
                u = ukey(key)
                if u is None or u not in st: continue
                d = 0.5*(prev.get(key, 0.0) + cur.get(key, 0.0))*months
                st[u]['rem'] = max(0.0, st[u]['rem'] - d)
            if unit == 'region':
                for reg, ql in opens.items():
                    for yq in ql:
                        if ys[k-1] < yq <= y and reg in st:
                            st[reg]['rem'] += BON*st[reg]['init']/years
        # 捕捉
        tot = collections.Counter()
        for key in keys:
            u = ukey(key)
            if u is None: continue
            tot[u] += cur.get(key, 0.0)
        for u, t in tot.items():
            if u in st or not (t > 0): continue
            yrs = years
            if unit == 'prod' and u[2] and site_years is not None: yrs = site_years
            if unit == 'prod' and u[2] is False and site_years == 0: continue   # 案 S：ふつうは在庫なし
            st[u] = {'init': 12*yrs*t, 'rem': 12*yrs*t}
        out = {}
        for key in cur:
            u = ukey(key)
            if u is None or u not in st: out[key] = 1.0; continue
            s = st[u]; out[key] = gf(s['rem']/s['init']) if s['init'] > 0 else 1.0
        res[y] = out
    return res

def wq(pairs, q):
    pairs = sorted(pairs); tot = sum(w for _, w in pairs)
    if tot <= 0: return float('nan')
    acc = 0.0
    for v, w in pairs:
        acc += w
        if acc >= q*tot: return v
    return pairs[-1][0]

def main():
    raw, s0, s1, outp = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
    schemes = {
        '現行(州・膝・100)': dict(unit='region', years=100.0, gf=gk),
        '案S(産地60・比例)': dict(unit='prod', years=1.0, gf=gp, site_years=0),
        '2番目(生産者・膝・100)': dict(unit='prod', years=100.0, gf=gk),
        '2番目(生産者・膝・4300)': dict(unit='prod', years=4300.0, gf=gk),
        '3a(生産者・比例・300)': dict(unit='prod', years=300.0, gf=gp),
        '3a(生産者・比例・1000)': dict(unit='prod', years=1000.0, gf=gp),
        '3a(生産者・比例・3000)': dict(unit='prod', years=3000.0, gf=gp),
        '3b(生産者・比例・1000＋産地60)': dict(unit='prod', years=1000.0, gf=gp, site_years=60.0),
        '3c(州・遍在外し・比例・1000)': dict(unit='region', years=1000.0, gf=gp, exclude_ubi=True),
        '3c(州・遍在外し・比例・3000)': dict(unit='region', years=3000.0, gf=gp, exclude_ubi=True),
    }
    # 案 S の産地：site_years=60 を産地に・ふつうは 0（在庫なし）＝下で特別に扱う
    schemes['案S(産地60・比例)'] = dict(unit='prod', years=60.0, gf=gp, site_years='S')
    acc = collections.defaultdict(list)       # (scheme, year, good, cls) -> [(grade, out)]
    uk = collections.defaultdict(lambda: [0.0, 0.0])   # (scheme, year, good, isUK) -> [Σout×g, Σout]
    sites = collections.defaultdict(list)     # (scheme, lag) -> grades
    match = collections.Counter()
    for s in range(s0, s1+1):
        snaps, owner, qof, opens = load(raw, s)
        # 鍵ごとの開いた年
        res_all = {}
        for name, kw in schemes.items():
            kw = dict(kw)
            if kw.get('site_years') == 'S':
                r60 = run(snaps, opens, 'prod', 60.0, gp)
                res = {y: {key: (v if key[2] else 1.0) for key, v in d.items()} for y, d in r60.items()}
            else:
                res = run(snaps, opens, **kw)
            res_all[name] = res
            for y in YS:
                if y not in snaps: continue
                for key, o in snaps[y].items():
                    reg, g, site = key
                    if not (o > 0) or g not in GOODS: continue
                    gr = res[y].get(key, 1.0)
                    acc[(name, y, g, 'site' if site else 'ord')].append((gr, o))
                    if not site:
                        a = uk[(name, y, g, owner.get((y, reg)) == 'イギリス')]; a[0] += o*gr; a[1] += o
            # 産地の品位（開いた後の最初の断面から 5 年後・30 年後の最も近い断面）
            ys = list(snaps.keys())
            first = {}
            for y in ys:
                for key, o in snaps[y].items():
                    if key[2] and o > 0 and key not in first: first[key] = y
            for key, y0 in first.items():
                for lag in (10, 30, 60):
                    yy = [y for y in ys if y >= y0 + lag]
                    if yy: sites[(name, lag)].append(res[yy[0]].get(key, 1.0))
        # エンジンとの突き合わせ（現行の推しの床か否か vs mintreg_s の grade）
        eng = {}
        qset = {qof[y]: y for y in YS if y in qof}
        with open(f'{raw}/mintreg_s{s}.tsv', encoding='utf-8', newline='') as f:
            rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k:i for i,k in enumerate(h)}
            for r in rd:
                q = int(r[ix['q']])
                if q not in qset: continue
                try: eng[(qset[q], r[ix['region']])] = float(r[ix['grade']])
                except ValueError: pass
        cur = res_all['現行(州・膝・100)']
        for y in YS:
            if y not in snaps: continue
            regs = {key[0] for key, o in snaps[y].items() if o > 0}
            for reg in regs:
                if (y, reg) not in eng: match['無'] += 1; continue
                gs = [v for key, v in cur[y].items() if key[0] == reg]
                if not gs: continue
                gr = gs[0]; e = eng[(y, reg)]
                match[(y, 'n')] += 1
                if (gr <= FLOOR + 1e-9) == (e <= FLOOR + 1e-9): match[(y, '床の一致')] += 1
                if abs(gr - e) <= 0.02: match[(y, '差0.02以内')] += 1
    with open(outp, 'w', encoding='utf-8', newline='\n') as w:
        w.write(f'# an7 種 {s0}〜{s1}・静的な推し（産出の道は基線のまま）\n')
        w.write('## 現行の推しとエンジン（mintreg_s grade）の突き合わせ（州×年）\n')
        for y in YS:
            n = match[(y, 'n')]
            if n: w.write(f'{y}\tn={n}\t床の判定の一致={match[(y,"床の一致")]/n:.3f}\t|差|≦0.02={match[(y,"差0.02以内")]/n:.3f}\n')
        w.write(f'mintreg に無い州×年 {match["無"]}\n')
        w.write('## ふつうの生産者：産出の重みの品位の平均・品位＜0.5 の産出の割合・重みつき P10/P50/P90\n')
        w.write('scheme\tyear\tgood\tmean\tlt05\tp10\tp50\tp90\n')
        for name in schemes:
            for y in YS:
                for g in GOODS:
                    L = acc.get((name, y, g, 'ord'))
                    if not L: continue
                    tot = sum(o for _, o in L)
                    mean = sum(gr*o for gr, o in L)/tot
                    lt = sum(o for gr, o in L if gr < 0.5)/tot
                    w.write(f'{name}\t{y}\t{g}\t{mean:.3f}\t{lt:.3f}\t{wq(L,.1):.3f}\t{wq(L,.5):.3f}\t{wq(L,.9):.3f}\n')
        w.write('## 英（ownerNation イギリス）とそれ以外の品位（ふつうの生産者・産出の重み）\n')
        w.write('scheme\tyear\tgood\tUK\trest\n')
        for name in schemes:
            for y in (1880, 1913):
                for g in GOODS:
                    a = uk.get((name, y, g, True)); b = uk.get((name, y, g, False))
                    if a and b and a[1] > 0 and b[1] > 0:
                        w.write(f'{name}\t{y}\t{g}\t{a[0]/a[1]:.3f}\t{b[0]/b[1]:.3f}\n')
        w.write('## 産地の生産者の品位（最初に産出のあった断面から 10・30・60 年後の最初の断面）：件数・中央・≧0.5 の割合\n')
        for name in schemes:
            row = [name]
            for lag in (10, 30, 60):
                L = sorted(sites.get((name, lag), []))
                if L: row.append(f'+{lag}y n={len(L)} 中央={L[len(L)//2]:.3f} ≧0.5={sum(1 for v in L if v>=0.5)/len(L):.2f}')
            w.write('\t'.join(row) + '\n')
main()
