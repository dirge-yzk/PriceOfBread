"""(721) rdh：名簿の外の空洞化の原因の読み手（読むだけ）。
python -X utf8 rdh_read.py region <計器の出力ディレクトリ> <種> <州> [年の刻み=5]
    州の人口の減りの内訳（四半期の表 region_s・rdh_s・epi_s を年の刻みで足す）：
    Δ人口＝出生−平常の死−飢えの死−疫病の死−(州の間の出−入)−(入植の出−入)−(越境の出−入)＋残り（残り＝割譲の移し・戦死・鎮圧ほか計器に無い物）。
    PopDecision の駆動（g の平均・V農村・V都市・自給÷V農村・家賃・V都市の 0 の切れ・州の中の流れ）と機会の因子（region_s）を並べる。持ち主の替わりを印す。
python -X utf8 rdh_read.py world <計器の出力ディレクトリ> <種...>
    開幕の過渡：O（構築の直後）・W（助走の後）・年ごとの世界の都市の割合・州の間の移民の世界の率（‰/四半期）・g の人口加重の分布。
python -X utf8 rdh_read.py first <出力A> <出力B> <種> <州>
    同じ種の 2 本で、その州の region_s の列が最初に分かれる四半期（列ごと）。
"""
import sys, os, collections, math


def fl(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def table(path):
    with open(path, encoding='utf-8') as fh:
        head = fh.readline().rstrip('\n').split('\t')
        for line in fh:
            c = line.rstrip('\n').split('\t')
            if len(c) != len(head):
                continue   # 書きかけの行（走っている途中の表）は読まない
            yield dict(zip(head, c))


def region_rows(d, sd, reg):
    R = {}
    for r in table(os.path.join(d, f'region_s{sd}.tsv')):
        if r['region'] == reg:
            R[int(r['quarter'])] = r
    return R


def rdh_rows(d, sd, reg=None):
    p = os.path.join(d, f'rdh_s{sd}.tsv')
    out = collections.defaultdict(list)
    if not os.path.exists(p):
        return out
    for r in table(p):
        if reg is None or r['region'] == reg:
            out[r['kind']].append(r)
    return out


def epi_rows(d, sd, reg):
    E = collections.Counter()
    p = os.path.join(d, f'epi_s{sd}.tsv')
    if os.path.exists(p):
        for r in table(p):
            if r['region'] == reg:
                E[int(r['quarter'])] += fl(r['deadK'])
    return E


def cmd_region(d, sd, reg, step):
    R = region_rows(d, sd, reg)
    H = rdh_rows(d, sd, reg)
    Q = {int(r['quarter']): r for r in H.get('Q', [])}
    E = epi_rows(d, sd, reg)
    for k in ('O', 'W'):
        for r in H.get(k, []):
            m = max(1, int(r['months']))
            print(f"## {k} {r['nation']} {r['tier']} 人口 {fl(r['populationK']):.1f} 農村 {fl(r['ruralPopulationK']):.1f} 月 {r['months']} g平均 {fl(r['gSum'])/m:.3f} g＜0 の月 {r['gNegMonths']} "
                  f"V農村 {fl(r['vRuralSum'])/m:.3g} V都市 {fl(r['vUrbanSum'])/m:.3g} 自給 {fl(r['subSum'])/m:.3g} 家賃 {fl(r['housingSum'])/m:.3g} 現金都市 {fl(r['earnUrbanSum'])/m:.3g} V都市0の月 {r['vUrbanClipMonths']} "
                  f"流れ 都市へ {fl(r['flowToUrbanK']):.1f} 農村へ {fl(r['flowToRuralK']):.1f} 出生 {fl(r['birthsK']):.1f} 死 {fl(r['deathsNormalK']):.1f} 飢え {fl(r['deathsFamineK']):.1f}")
    qs = sorted(R)
    if not qs:
        print('## 州の行なし'); return
    peak = max(fl(R[q]['populationK']) for q in qs)
    print(f'## 種 {sd} {reg} ピーク {peak:.1f}')
    cols = ['年', '持ち主', '格', '人口', '÷ピーク', 'Δ', '出生', '平常の死', '飢え', '疫病', '州間の純流出', '入植純', '越境純', '残り', 'g平均', 'V農', 'V都', '自給÷V農', '家賃', 'V都0の月', '州内 都→農', '州内 農→都', '農村', '耕地', 'opp', 'keep', 'land', 'hous', 'wprem', 'emp', 'food']
    print('\t'.join(cols))
    prevPop = None; prevOwner = None
    buckets = collections.OrderedDict()
    for q in qs:
        y = int(R[q]['year']); b = (y - 1783) // step * step + 1783
        buckets.setdefault(b, []).append(q)
    for b, qq in buckets.items():
        r0 = R[qq[0]]; r1 = R[qq[-1]]
        if prevPop is None:
            prevPop = fl(r0['populationK']) - 0.0
        s = collections.Counter()
        own = []
        for q in qq:
            r = R[q]; h = Q.get(q)
            s['mig'] += fl(r['migrationOutKPerQuarter']) - fl(r['migrationInKPerQuarter'])
            s['col'] += fl(r.get('settlerOutKPerQuarter', '0')) - fl(r.get('settlerInKPerQuarter', '0'))
            s['xb'] += fl(r['crossBorderOutKPerQuarter']) - fl(r['crossBorderInKPerQuarter'])
            s['epi'] += E.get(q, 0.0)
            if h:
                for k, c in (('b', 'birthsK'), ('dn', 'deathsNormalK'), ('df', 'deathsFamineK'), ('g', 'gSum'), ('vr', 'vRuralSum'), ('vu', 'vUrbanSum'), ('sub', 'subSum'), ('hs', 'housingSum'), ('fu', 'flowToUrbanK'), ('frr', 'flowToRuralK')):
                    s[k] += fl(h[c])
                s['m'] += int(h['months']); s['clip'] += int(h['vUrbanClipMonths'])
            o = (r['nation'], r['tier'])
            if o != prevOwner:
                own.append(f"{r['year']}:{r['nation']}"); prevOwner = o
        pop1 = fl(r1['populationK'])
        dpop = pop1 - prevPop
        resid = dpop - (s['b'] - s['dn'] - s['df'] - s['epi'] - s['mig'] - s['col'] - s['xb'])
        m = max(1, s['m'])
        vals = [b, '/'.join(own) if own else r1['nation'], r1['tier'], f'{pop1:.1f}', f'{pop1/peak:.3f}', f'{dpop:.1f}', f"{s['b']:.1f}", f"{s['dn']:.1f}", f"{s['df']:.1f}", f"{s['epi']:.1f}",
                f"{s['mig']:.1f}", f"{s['col']:.1f}", f"{s['xb']:.1f}", f'{resid:.1f}', f"{s['g']/m:.3f}", f"{s['vr']/m:.3g}", f"{s['vu']/m:.3g}",
                f"{(s['sub']/s['vr']) if s['vr'] > 0 else float('nan'):.3f}", f"{s['hs']/m:.3g}", int(s['clip']), f"{s['frr']:.1f}", f"{s['fu']:.1f}",
                f"{fl(r1['ruralPopulationK']):.1f}", f"{fl(r1['arableLand']):.0f}"] + [f"{fl(r1[k]):.3f}" for k in ('opportunityIndex', 'holdingFactor', 'landFactor', 'housingFactor', 'wagePremium', 'employmentShare', 'foodFillShare')]
        print('\t'.join(str(v) for v in vals))
        prevPop = pop1


def cmd_world(d, seeds):
    for sd in seeds:
        H = rdh_rows(d, sd)
        print(f'## 種 {sd}')
        for k in ('O', 'W'):
            rows = H.get(k, [])
            P = sum(fl(r['populationK']) for r in rows); U = sum(fl(r['populationK']) - fl(r['ruralPopulationK']) for r in rows)
            gw = 0.0; gp = 0.0; neg = 0.0; big = 0.0; clip = 0.0
            hist = collections.Counter()
            for r in rows:
                m = int(r['months'])
                if m <= 0:
                    continue
                g = fl(r['gSum']) / m; p = fl(r['populationK'])
                if not math.isfinite(g):
                    continue
                gw += g * p; gp += p
                if g < 0: neg += p
                if g < -1: big += p
                clip += p * int(r['vUrbanClipMonths']) / m
                hist[max(-2.0, min(2.0, math.floor(g * 2) / 2))] += p
            fu = sum(fl(r['flowToUrbanK']) for r in rows); fr = sum(fl(r['flowToRuralK']) for r in rows)
            print(f"  {k}: 都市の割合 {U/P:.4f} 人口 {P:.0f} g の人口加重 {gw/max(gp,1e-9):.3f} g＜0 の人口の割合 {neg/max(gp,1e-9):.3f} g＜−1 {big/max(gp,1e-9):.3f} V都市0の切れ（人口×月の割合） {clip/max(gp,1e-9):.3f} "
                  f"州の中の流れ 都市へ {fu:.0f} 農村へ {fr:.0f}（千人）")
            print('     g の分布（人口の割合・0.5 刻みの下端）: ' + '  '.join(f'{b:+.1f} {v/max(gp,1e-9):.3f}' for b, v in sorted(hist.items())))
        # 年ごと（Q の行）
        byY = collections.OrderedDict()
        for r in H.get('Q', []):
            y = int(r['year'])
            a = byY.setdefault(y, collections.Counter())
            m = int(r['months'])
            if m > 0:
                a['gw'] += fl(r['gSum']) / m * fl(r['populationK']); a['gp'] += fl(r['populationK'])
            a['fu'] += fl(r['flowToUrbanK']); a['fr'] += fl(r['flowToRuralK'])
        URB = collections.defaultdict(lambda: [0.0, 0.0]); MIG = collections.defaultdict(lambda: [0.0, 0.0])
        for r in table(os.path.join(d, f'region_s{sd}.tsv')):
            y = int(r['year']); p = fl(r['populationK'])
            if not (p > 0):
                continue
            if int(r['quarter']) % 4 == 3:
                URB[y][0] += p - fl(r['ruralPopulationK']); URB[y][1] += p
            MIG[y][0] += fl(r['migrationOutKPerQuarter']); MIG[y][1] += p
        print('  年\t都市の割合\t州の間の移民‰/四半期\tg の人口加重\t州の中 都市へ\t農村へ')
        for y in sorted(URB):
            if y in (1783, 1784, 1785, 1786, 1788, 1790, 1795, 1800, 1810, 1820, 1830, 1850, 1880, 1913):
                a = byY.get(y, collections.Counter())
                print(f"  {y}\t{URB[y][0]/URB[y][1]:.4f}\t{1000*MIG[y][0]/MIG[y][1]:.3f}\t{a['gw']/max(a['gp'],1e-9):.3f}\t{a['fu']:.0f}\t{a['fr']:.0f}")


def cmd_first(da, db, sd, reg):
    A = region_rows(da, sd, reg); B = region_rows(db, sd, reg)
    first = {}
    for q in sorted(set(A) & set(B)):
        for k in A[q]:
            if k in ('quarter', 'year'):
                continue
            if k not in first and A[q][k] != B[q].get(k):
                first[k] = (q, A[q]['year'], A[q][k], B[q].get(k))
    for k, v in sorted(first.items(), key=lambda kv: kv[1][0]):
        print(f'{k}\tq{v[0]}\t{v[1]}\t{v[2]}\t{v[3]}')


if __name__ == '__main__':
    a = sys.argv[1:]
    if a[0] == 'region':
        cmd_region(a[1], int(a[2]), a[3], int(a[4]) if len(a) > 4 else 5)
    elif a[0] == 'world':
        cmd_world(a[1], [int(x) for x in a[2:]])
    elif a[0] == 'first':
        cmd_first(a[1], a[2], int(a[3]), a[4])
