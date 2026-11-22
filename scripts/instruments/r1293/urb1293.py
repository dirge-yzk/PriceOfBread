# (1293) c133fr 設計の材料の読み手（読むだけ・腕なし）：都市と農村の往復と飢えの門
#   使い方: python -X utf8 -I urb1293.py <出力フォルダ> <名:ins のフォルダ:種の起点> ...
#   例: urb1293.py D:/PoB_work/r1293_work base:D:/PoB_work/keep_1287/base_ins:1 n1:D:/PoB_work/keep_1287/n1:1 a11:D:/PoB_work/keep_1287/a11:11
#   ① urb_<名>.tsv：国ごとの都市化率＝1−Σ農村÷Σ州の人口（region_s・その年の最初の四半期・種の中央値）。世界＝全ての国の和。
#   ② pd_<名>.tsv：urxpd_s（計器の 9 国・本国格の州）を 5 つの年代で：門の州×月の割合・シグモイドの目標・対の都市の割合・州の都市の割合・
#      名目の都市÷農村・g の重みつき平均・向きの反転の割合・都市へ／農村への流れ（種の中央値）。
#   ③ rt_<名>.tsv：往復の代理（国×四半期）：門の掛かった四半期（famineGated＞0）の農村への流れ B と、続く 4 四半期の都市への流れ R。
#      対照＝門の掛からない四半期で農村への流れ＞0 の物。州×月では読めない（urxpd_s は国×四半期の和）＝上限の代理と名乗る。
#   ④ mig_<名>.tsv：州の間の移民の出（region_s の migrationOutKPerQuarter）÷人口（‰・四半期あたり）を年代で。
#   ⑤ urxrt_<名>.tsv：urxrt_s の PopDecision の都市への入りと出・往復の総量÷純。
import sys, csv, os, statistics as st, collections

NAT = ['イギリス', 'フランス', 'プロイセン', 'アメリカ', '日本', '清', 'ロシア', 'オスマン', 'オーストリア']
YEARS = [1783, 1820, 1850, 1880, 1913, 1940]
WIN = [(1783, 1814), (1815, 1849), (1850, 1879), (1880, 1913), (1914, 1950)]

def win_of(y):
    for a, b in WIN:
        if a <= y <= b: return f'{a}-{b}'
    return None

def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float('nan')

def rows(p):
    with open(p, encoding='utf-8') as fh:
        yield from csv.DictReader(fh, delimiter='\t')

def f(x):
    return 'nan' if x != x else (f'{x:.4f}' if abs(x) < 1e4 else f'{x:.1f}')

out = sys.argv[1]
for spec in sys.argv[2:]:
    name, d, sb = spec.rsplit(':', 1)[0].split(':', 1)[0], spec.split(':', 1)[1].rsplit(':', 1)[0], int(spec.rsplit(':', 1)[1])
    seeds = [s for s in range(sb, sb + 10) if os.path.exists(os.path.join(d, f'region_s{s}.tsv'))]
    # ① と ④
    urb = collections.defaultdict(list); mig = collections.defaultdict(list)
    for s in seeds:
        firstq = {}
        acc = collections.defaultdict(lambda: [0.0, 0.0])
        macc = collections.defaultdict(lambda: [0.0, 0.0])
        for r in rows(os.path.join(d, f'region_s{s}.tsv')):
            q = int(r['quarter']); y = int(r['year']); n = r['nation']
            if y not in firstq: firstq[y] = q
            pop = float(r['populationK']); rur = float(r['ruralPopulationK'])
            if y in YEARS and q == firstq[y]:
                for k in ((n, y), ('世界', y)):
                    acc[k][0] += pop; acc[k][1] += rur
            w = win_of(y)
            if w and n in NAT:
                macc[(n, w)][0] += float(r['migrationOutKPerQuarter']); macc[(n, w)][1] += pop
        for k, (p, rr) in acc.items():
            if p > 0: urb[k].append(1 - rr / p)
        for k, (o, p) in macc.items():
            if p > 0: mig[k].append(1000 * o / p)
    with open(os.path.join(out, f'urb_{name}.tsv'), 'w', encoding='utf-8', newline='\n') as fo:
        fo.write('nation\t' + '\t'.join(str(y) for y in YEARS) + '\tseeds\n')
        for n in NAT + ['世界']:
            fo.write(n + '\t' + '\t'.join(f(med(urb[(n, y)])) for y in YEARS) + f'\t{len(urb[(n, 1913)])}\n')
    with open(os.path.join(out, f'mig_{name}.tsv'), 'w', encoding='utf-8', newline='\n') as fo:
        fo.write('nation\t' + '\t'.join(f'{a}-{b}' for a, b in WIN) + '\n')
        for n in NAT:
            fo.write(n + '\t' + '\t'.join(f(med(mig[(n, f"{a}-{b}")])) for a, b in WIN) + '\n')
    # ② と ③
    pd = collections.defaultdict(lambda: collections.defaultdict(list))
    rt = collections.defaultdict(lambda: collections.defaultdict(list))
    for s in seeds:
        p = os.path.join(d, f'urxpd_s{s}.tsv')
        if not os.path.exists(p): continue
        acc = collections.defaultdict(lambda: collections.defaultdict(float))
        series = collections.defaultdict(list)
        for r in rows(p):
            n = r['nation']; y = int(r['year']); w = win_of(y)
            if n not in NAT or not w: continue
            a = acc[(n, w)]
            for c in ('regionMonths', 'famineGated', 'sumFlowPos', 'sumFlowNeg', 'sumTotal', 'sumRegionPop', 'sumRegionUrban', 'wTargetPre', 'wCur', 'wIncU', 'wIncR', 'wGsym', 'flowFlipRegionMonths', 'wAbsGsym'):
                a[c] += float(r[c])
            series[n].append((int(r['quarter']), float(r['famineGated']), float(r['sumFlowPos']), float(r['sumFlowNeg']), float(r['flowFlipRegionMonths']), float(r['regionMonths'])))
        for k, a in acc.items():
            rm = a['regionMonths']; tt = a['sumTotal']
            if rm <= 0 or tt <= 0: continue
            v = pd[k]
            v['gatedShare'].append(a['famineGated'] / rm); v['target'].append(a['wTargetPre'] / tt); v['pairUrban'].append(a['wCur'] / tt)
            v['regionUrban'].append(a['sumRegionUrban'] / a['sumRegionPop'] if a['sumRegionPop'] > 0 else float('nan'))
            v['incU/incR'].append(a['wIncU'] / a['wIncR'] if a['wIncR'] > 0 else float('nan'))
            v['gsym'].append(a['wGsym'] / tt); v['absg'].append(a['wAbsGsym'] / tt); v['flip'].append(a['flowFlipRegionMonths'] / rm)
            v['flowPosK'].append(a['sumFlowPos']); v['flowNegK'].append(a['sumFlowNeg'])
        for n, ser in series.items():
            ser.sort()
            idx = {q: i for i, (q, *_) in enumerate(ser)}
            cnt = collections.Counter(); amt = collections.Counter()
            flipG = [0.0, 0.0]; flipN = [0.0, 0.0]
            for i, (q, g, pos, neg, flp, rm) in enumerate(ser):
                if g > 0: flipG[0] += flp; flipG[1] += rm
                else: flipN[0] += flp; flipN[1] += rm
                if neg <= 0: continue
                kind = 'G' if g > 0 else 'N'
                nxt = sum(ser[j][2] for j in range(i + 1, min(i + 5, len(ser))))
                cnt[kind] += 1; amt[kind + 'back'] += neg; amt[kind + 'next'] += nxt
                if nxt >= neg: cnt[kind + 'rt'] += 1
            v = rt[n]
            for kind in 'GN':
                v[kind + 'quarters'].append(cnt[kind]); v[kind + 'rtShare'].append(cnt[kind + 'rt'] / cnt[kind] if cnt[kind] else float('nan'))
                v[kind + 'backK'].append(amt[kind + 'back']); v[kind + 'next4PosK'].append(amt[kind + 'next'])
            v['flipInG'].append(flipG[0] / flipG[1] if flipG[1] else float('nan')); v['flipOutG'].append(flipN[0] / flipN[1] if flipN[1] else float('nan'))
    cols = ['gatedShare', 'target', 'pairUrban', 'regionUrban', 'incU/incR', 'gsym', 'absg', 'flip', 'flowPosK', 'flowNegK']
    with open(os.path.join(out, f'pd_{name}.tsv'), 'w', encoding='utf-8', newline='\n') as fo:
        fo.write('nation\twindow\t' + '\t'.join(cols) + '\n')
        for n in NAT:
            for a, b in WIN:
                v = pd[(n, f'{a}-{b}')]
                fo.write(f'{n}\t{a}-{b}\t' + '\t'.join(f(med(v[c])) for c in cols) + '\n')
    rc = ['Gquarters', 'GrtShare', 'GbackK', 'Gnext4PosK', 'Nquarters', 'NrtShare', 'NbackK', 'Nnext4PosK', 'flipInG', 'flipOutG']
    with open(os.path.join(out, f'rt_{name}.tsv'), 'w', encoding='utf-8', newline='\n') as fo:
        fo.write('nation\t' + '\t'.join(rc) + '\n')
        for n in NAT:
            fo.write(n + '\t' + '\t'.join(f(med(rt[n][c])) for c in rc) + '\n')
    # ⑤
    ur = collections.defaultdict(lambda: collections.defaultdict(list))
    for s in seeds:
        p = os.path.join(d, f'urxrt_s{s}.tsv')
        if not os.path.exists(p): continue
        acc = collections.defaultdict(lambda: [0.0, 0.0])
        for r in rows(p):
            n = r['nation']; w = win_of(int(r['year']))
            if n not in NAT or not w or r['route'] != 'PopDecision': continue
            acc[(n, w)][0] += float(r['urbanInK']); acc[(n, w)][1] += float(r['urbanOutK'])
        for k, (i, o) in acc.items():
            ur[k]['inK'].append(i); ur[k]['outK'].append(o); ur[k]['gross/net'].append((i + o) / abs(i - o) if abs(i - o) > 0 else float('nan'))
    with open(os.path.join(out, f'urxrt_{name}.tsv'), 'w', encoding='utf-8', newline='\n') as fo:
        fo.write('nation\twindow\tinK\toutK\tgross/net\n')
        for n in NAT:
            for a, b in WIN:
                v = ur[(n, f'{a}-{b}')]
                fo.write(f'{n}\t{a}-{b}\t' + '\t'.join(f(med(v[c])) for c in ('inK', 'outK', 'gross/net')) + '\n')
    print(name, 'seeds', seeds)
