# (1245) c126s28 読むだけ：S28 の行を keep_1233/base_ins（m1233・計器つき 10 種）で読み直す。
#  使い方: python -X utf8 -I s28_reread.py <生のフォルダ> <種の始め> <種の終わり> <prodcap の種の終わり> <repo の SimCore> <出力>
#  計器が書かない行（NaN・空）は数えない。母集合は表ごとに名乗る（fd＝計器の 9 国・epi／minecap／region／site＝全ての国）。
import sys, os, csv, math, json, collections
raw, s0, s1, ps1, simcore, outp = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), int(sys.argv[4]), sys.argv[5], sys.argv[6]
csv.field_size_limit(10**9)
O = open(outp, 'w', encoding='utf-8', newline='\n')
def P(*a): O.write(' '.join(str(x) for x in a) + '\n')
def fl(x):
    try:
        v = float(x); return v if math.isfinite(v) else None
    except Exception: return None
def rows(t, s):
    p = os.path.join(raw, f'{t}_s{s}.tsv')
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for x in r: yield ix, x
def q(v, p):
    if not v: return None
    v = sorted(v); return v[min(len(v) - 1, int(p * len(v)))]
def skew(v):
    n = len(v); m = sum(v) / n; s = math.sqrt(sum((x - m) ** 2 for x in v) / n)
    return sum((x - m) ** 3 for x in v) / n / s ** 3 if s > 0 else float('nan')
def corr(a, b):
    n = len(a)
    if n < 3: return None
    ma, mb = sum(a) / n, sum(b) / n
    sa = math.sqrt(sum((x - ma) ** 2 for x in a)); sb = math.sqrt(sum((x - mb) ** 2 for x in b))
    return sum((x - ma) * (y - mb) for x, y in zip(a, b)) / sa / sb if sa > 0 and sb > 0 else None
# 首都の緯度（world.json の 1 番目の州の city を places.json で引く・無ければ国の latitude）＝帯 floor(lat/15)
W = json.load(open(os.path.join(simcore, 'world.json'), encoding='utf-8'))
PL = {p['name']: p for p in json.load(open(os.path.join(simcore, 'places.json'), encoding='utf-8'))}
band = {}
for n in W['nations']:
    c = n['provinces'][0]['city'] if n.get('provinces') else None
    lat = PL[c]['lat'] if c in PL else n.get('latitude')
    if lat is not None: band[n['name']] = math.floor(lat / 15.0)
P('# 生', raw, '種', s0, '〜', s1, '（prodcap は種', s0, '〜', ps1, '）')
P('## A 天候（fd_s・計器の 9 国・weatherHarvestMean＝州の収穫係数×帯×作物の健全度〔土地係数 1〕の主食の需要加重・国×年＝州×四半期の平均）')
for s in range(s0, s1 + 1):
    ny = collections.defaultdict(list); spread = collections.defaultdict(list)
    for ix, x in rows('fd', s):
        v = fl(x[ix['weatherHarvestMean']])
        if v is None: continue
        ny[(x[ix['nation']], int(x[ix['year']]))].append(v); spread[(x[ix['nation']], int(x[ix['quarter']]))].append(v)
    vals = {k: sum(v) / len(v) for k, v in ny.items()}
    allv = list(vals.values())
    sd = math.sqrt(sum((x - 1) ** 2 for x in allv) / len(allv))
    zero = sum(1 for v in spread.values() if max(v) - min(v) < 1e-9) / len(spread)
    # 前年との相関（国ごとに平均を引いて溜める）
    a, b = [], []
    byn = collections.defaultdict(dict)
    for (n, y), v in vals.items(): byn[n][y] = v
    for n, d in byn.items():
        m = sum(d.values()) / len(d)
        for y, v in d.items():
            if y - 1 in d: a.append(d[y - 1] - m); b.append(v - m)
    ac = corr(a, b)
    same, diff = [], []
    ns = sorted(byn)
    for i in range(len(ns)):
        for j in range(i + 1, len(ns)):
            ys = sorted(set(byn[ns[i]]) & set(byn[ns[j]]))
            c = corr([byn[ns[i]][y] for y in ys], [byn[ns[j]][y] for y in ys])
            if c is None or ns[i] not in band or ns[j] not in band: continue
            (same if band[ns[i]] == band[ns[j]] else diff).append(c)
    f = lambda v: f'{sum(v)/len(v):.3f}（{len(v)} 対）' if v else '—'
    P(f's{s} 国×年 {len(allv)}・平均 {sum(allv)/len(allv):.4f}・1 からの標準偏差 {sd:.3f}・<0.8 {sum(1 for x in allv if x < 0.8)/len(allv):.3%}・>1.2 {sum(1 for x in allv if x > 1.2)/len(allv):.3%}・歪度 {skew(allv):.3f}・最小 {min(allv):.3f}・最大 {max(allv):.3f}・前年との相関 {ac:.3f}・同じ帯の相関 {f(same)}・違う帯 {f(diff)}・国×四半期で州の幅 0 の割合 {zero:.3%}')
P('## B 疫病（epi_s・全ての国・origin 0＝内部の発生・1＝国際の伝播・2＝国内の二次の波）')
for s in range(s0, s1 + 1):
    cnt = collections.Counter(); sh = collections.defaultdict(list); nat = collections.Counter(); yrs = collections.defaultdict(list)
    for ix, x in rows('epi', s):
        o = x[ix['origin']]; cnt[o] += 1
        d, pb = fl(x[ix['deadK']]), fl(x[ix['regionPopKBefore']])
        if d is not None and pb and pb > 0: sh[o].append(d / pb)
        if o in ('0', '1'): nat[x[ix['nation']]] += 1; yrs[x[ix['nation']]].append(int(x[ix['year']]))
    a = sh['0'] + sh['1']
    big = sum(1 for v in a if v > 0.05); mid = sum(1 for v in a if v > 0.02)
    P(f's{s} 発生 {cnt["0"]}・伝播 {cnt["1"]}・二次 {cnt["2"]}・二次÷(発生＋伝播) {cnt["2"]/max(1,cnt["0"]+cnt["1"]):.3f}・国 {len(nat)}・死の割合（州の人口比・発生＋伝播）中央 {q(a,0.5):.4f}・90% {q(a,0.9):.4f}・最大 {max(a):.4f}・>2% {mid/len(a):.3%}・>5% {big/len(a):.3%}・国あたりの件数 中央 {q(list(nat.values()),0.5)}・最大 {max(nat.values())}')
P('## C 鉱脈の品位（minecap_s・全ての国・年末・鉱物の RGO の産出加重・gradeBin0＝品位が床〔0.15〕以下の産出の割合）')
UBI = {'粘土', '珪砂', '石灰石'}
for s in range(s0, s1 + 1):
    acc = collections.defaultdict(lambda: [0.0, 0.0, 0.0, 0.0])
    for ix, x in rows('minecap', s):
        y = int(x[ix['year']])
        if y not in (1800, 1825, 1850, 1900, 1949): continue
        o, g0, gw = fl(x[ix['outputSum']]), fl(x[ix['gradeBin0']]), fl(x[ix['gradeOutW']])
        if o is None or o <= 0 or g0 is None or gw is None: continue
        k = (y, 'U' if x[ix['good']] in UBI else 'R')
        a = acc[k]; a[0] += o; a[1] += o * g0; a[2] += o * gw
    P(f's{s} ' + '・'.join(f'{y}{"遍在" if c=="U" else "他"} 床 {acc[(y,c)][1]/acc[(y,c)][0]:.3f} 品位 {acc[(y,c)][2]/acc[(y,c)][0]:.3f}' for y in (1800, 1825, 1850, 1900, 1949) for c in ('R', 'U') if acc[(y, c)][0] > 0))
P('## D 産地の発見（site_s・kind O＝開いた行・全ての国）')
for s in range(s0, s1 + 1):
    o = [(x[ix['site']], int(x[ix['year']]), x[ix['nation']]) for ix, x in rows('site', s) if x[ix['kind']] == 'O']
    P(f's{s} ' + '・'.join(f'{a} {y}（{n}）' for a, y, n in o))
P('## E 土地係数（region_s・全ての国・活きた州×四半期・landFactor＜1 の割合）')
for s in range(s0, s1 + 1):
    tot = collections.Counter(); lo = collections.Counter(); lo9 = collections.Counter()
    for ix, x in rows('region', s):
        if x[ix['activeFlag']] != '1': continue
        v = fl(x[ix['landFactor']])
        if v is None: continue
        y = int(x[ix['year']]); per = '1783-1849' if y < 1850 else '1850-1924' if y < 1925 else '1925-1950'
        for k in (per, '全'):
            tot[k] += 1; lo[k] += v < 1 - 1e-9; lo9[k] += v < 0.9
    P(f's{s} ' + '・'.join(f'{k} ＜1 {lo[k]/tot[k]:.2%}・＜0.9 {lo9[k]/tot[k]:.2%}（{tot[k]}）' for k in ('全', '1783-1849', '1850-1924', '1925-1950') if tot[k]))
P('## F 鉱山と畑の規模（prodcap_s・RGO の行・scale の上端 ScaleMax 3.0 に張り付く割合）')
for s in range(s0, ps1 + 1):
    n = collections.Counter(); top = collections.Counter(); over = collections.Counter()
    for ix, x in rows('prodcap', s):
        if x[ix['isRgo']] != '1': continue
        v = fl(x[ix['scale']])
        if v is None: continue
        k = x[ix['class']]
        n[k] += 1; top[k] += v >= 3.0 - 1e-6; over[k] += v > 1.5
    P(f's{s} ' + '・'.join(f'{k} ≥3.0 {top[k]/n[k]:.3%}・>1.5 {over[k]/n[k]:.2%}（{n[k]}）' for k in sorted(n)))
O.close(); print('ok', outp)
