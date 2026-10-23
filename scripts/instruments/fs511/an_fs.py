# (511) 線 fs：表 fs_s の読み手（事前登録 measurements/fs511_20261023_prereg.txt の [5] と 追記 2）。
# 使い方: python -I an_fs.py <腕 B の計器の出力フォルダ> [比べる側=D:/PoB_work/keep_508/m508_ins] [ヌル腕1=D:/PoB_work/keep_508/n1] [ヌル腕2=D:/PoB_work/keep_508/n2]
#   （fs_s<種>.tsv を読む。比べる側・ヌル腕は pop_s／region_s／pxgoods_s を読む。種の組は腕 B の fs_s の種に合わせる）
# --- 配線の確かめ（式から決まる＝機構が働いた証拠に数えない・追記 2）
#   M1 ownPerMonth≦unmetPerMonth×(1+1e-9)+1e-12・ownPerMonth≦ownCapPerMonth×(1+1e-9)+1e-12・fillNew≧fillBase−1e-12・fillNew≦1+1e-9 の違反の行 0
#   M2 unmet≧0.02×need かつ idleShare≧0.02 の行のうち own＞0 の割合≧0.99
#   M3 own＞0 の行を idleShare の 3 分位（州をまたぐ）に分け、最上位の fillNew−fillBase の中央値≧最下位の中央値（1783〜1850・1851〜1950 を別々に）
#   M4 停滞の行（Colonial・landFactor≧0.999・fillBase≦S×h＋0.05・unmet≧0.05×need）で fillNew≧fillBase＋0.05 の割合≧0.9 かつ寄り方の中央値≧0.5（停滞の行が 30 に満たなければ記録）
# --- 機構が働いた（働きの検査＝外れうる線・追記 2）
#   F1 凶作で下がる：(国,州) ごとに、凶作の行（harvestMean がその州の全行の中央値の 0.8 倍以下）が 5 行以上ある州のうち、凶作の行の fillNew の中央値≦その州の全行の fillNew の中央値−0.01 となる州の割合≧0.9
#   F2 土地が詰まると細る：unmet＞0 の行で、own÷need（ownPerMonth÷peasantNeedPerMonth）の中央値は landFactor＜0.9 の行が landFactor≧0.999 の行より小さい（両側 30 行以上。足りなければ記録）
#   F3 凶作は残る：pop_s の国の行（scope=N）の deathsFamineK の全期間の和（9 国×種・プール）の B÷比べる側≧0.5
# --- 壊す候補（AI の推奨・確証なし）
#   P1 人口の押し上げ：国ごとに 1850・1900 年の popK（pop_s の scope=N）の B÷比べる側の種の中央値。上へ外れた国＝(比−1)＞2×max(|N1の比−1|,|N2の比−1|,0.02) かつ 比≧1.10。1900 年で上へ外れた国が 9 国のうち 3 国以上なら〈壊す候補〉
#   S2 発散（価格）：pxgoods_s の主食の priceOverRefTimesLevelMedianRatio＞100 の行数（B と比べる側）。人口は scripts/instruments/m508/s2_region.py で別に読む
# 限界：四半期の末の断面・fillBase は旗①の締めと同じ式での反事実（翌月以降の違いは載らない）・中央値は行（州×四半期）の単純な中央値・F3／P1 は軌道が割れた 2 つの世界の比（比べる側は旗 0 の同じ種）。
import csv, sys, glob, os, math, statistics, collections
csv.field_size_limit(1 << 30)
sys.stdout.reconfigure(encoding='utf-8')
d = sys.argv[1]
base = sys.argv[2] if len(sys.argv) > 2 else 'D:/PoB_work/keep_508/m508_ins'
n1d = sys.argv[3] if len(sys.argv) > 3 else 'D:/PoB_work/keep_508/n1'
n2d = sys.argv[4] if len(sys.argv) > 4 else 'D:/PoB_work/keep_508/n2'
nullbase = sys.argv[5] if len(sys.argv) > 5 else base   # ヌル腕（種 1〜10）の比べる側＝旗 0 の種 1〜10。腕 B が種 11〜20 のときは 'D:/PoB_work/keep_508/m508_ins' を渡す
def fl(x):
    try: return float(x)
    except: return float('nan')
def load(folder, prefix, strcols=('nation', 'region', 'tier', 'scope', 'goodName', 'time'), seeds=None):
    rows = []
    for p in sorted(glob.glob(os.path.join(folder, prefix + '*.tsv'))):
        tail = os.path.basename(p)[len(prefix):-4]
        if not tail.isdigit(): continue
        sd = int(tail)
        if seeds is not None and sd not in seeds: continue
        with open(p, encoding='utf-8', newline='') as f:
            for r in csv.DictReader(f, delimiter='\t'):
                o = {k: (v if k in strcols else fl(v)) for k, v in r.items()}
                o['seed'] = sd
                rows.append(o)
    return rows
def med(xs):
    xs = sorted(x for x in xs if x == x)
    return statistics.median(xs) if xs else float('nan')
def wmed(vals):
    vals = sorted((v, w) for v, w in vals if v == v and w > 0)
    if not vals: return float('nan')
    tot = sum(w for _, w in vals); acc = 0
    for v, w in vals:
        acc += w
        if acc >= tot / 2: return v
rows = load(d, 'fs_s')
seeds = set(r['seed'] for r in rows)
print('(511) fs_s 読み：行数 %d・種 %d（フォルダ %s）・比べる側 %s' % (len(rows), len(seeds), d, base))
if not rows:
    print('行が無い（旗 0 の腕か計器が点いていない）'); sys.exit(0)
print('\n--- 配線の確かめ（機構が働いた証拠に数えない）')
viol = [r for r in rows if r['ownPerMonth'] > r['unmetPerMonth'] * (1 + 1e-9) + 1e-12 or r['ownPerMonth'] > r['ownCapPerMonth'] * (1 + 1e-9) + 1e-12
        or r['fillNew'] < r['fillBase'] - 1e-12 or r['fillNew'] > 1 + 1e-9]
print('M1 配線の違反の行 %d／%d %s' % (len(viol), len(rows), '→ OK' if not viol else '→ FAIL'))
over = sum(r['overNeedPopK'] for r in rows)
print('S1 観測 overNeedPopK の和 %.6g %s' % (over, '→ OK' if over == 0 else '→ FAIL'))
tgt = [r for r in rows if r['unmetPerMonth'] >= 0.02 * r['peasantNeedPerMonth'] and r['idleShare'] >= 0.02]
fire = sum(1 for r in tgt if r['ownPerMonth'] > 0) / len(tgt) if tgt else float('nan')
print('M2 不足と遊びが同時にある行 %d・そのうち own＞0 の割合 %.4f %s' % (len(tgt), fire, '→ OK' if fire >= 0.99 else '→ FAIL（対象 0 なら前提が無い）'))
for lab, lo, hi in (('1783-1850', 0, 1850), ('1851-1950', 1851, 9999)):
    sub = [r for r in rows if lo <= r['year'] <= hi and r['ownPerMonth'] > 0]
    if len(sub) < 9: print('M3 %s：own＞0 の行 %d（少なすぎ）' % (lab, len(sub))); continue
    sub.sort(key=lambda r: r['idleShare']); n = len(sub) // 3
    lowm = med([r['fillNew'] - r['fillBase'] for r in sub[:n]]); him = med([r['fillNew'] - r['fillBase'] for r in sub[-n:]])
    print('M3 %s：own＞0 の行 %d・idle の最下位 3 分位の lift 中央値 %.4f・最上位 %.4f %s' % (lab, len(sub), lowm, him, '→ OK' if him >= lowm - 1e-12 else '→ FAIL'))
st = [r for r in rows if r['tier'] == 'Colonial' and r['landFactor'] >= 0.999 and r['fillBase'] <= r['subsistenceMean'] * r['harvestMean'] + 0.05 and r['unmetPerMonth'] >= 0.05 * r['peasantNeedPerMonth']]
if len(st) < 30: print('M4 停滞の行 %d＜30＝前提が成り立たない（記録・採否に使わない）' % len(st))
else:
    sh = sum(1 for r in st if r['fillNew'] >= r['fillBase'] + 0.05) / len(st)
    mm = med([(r['fillNew'] - r['subsistenceMean'] * r['harvestMean']) / (r['harvestMean'] - r['subsistenceMean'] * r['harvestMean']) for r in st if r['harvestMean'] > r['subsistenceMean'] * r['harvestMean']])
    print('M4 停滞の行 %d・fillNew≧fillBase＋0.05 の割合 %.3f・寄り方の中央値 %.3f %s' % (len(st), sh, mm, '→ OK' if sh >= 0.9 and mm >= 0.5 else '→ FAIL'))

print('\n--- 機構が働いた（働きの検査）')
grp = collections.defaultdict(list)
for r in rows: grp[(r['nation'], r['region'])].append(r)
qual = 0; ok1 = 0; fails = []
for k, g in grp.items():
    hm = med([r['harvestMean'] for r in g])
    dr = [r for r in g if r['harvestMean'] <= 0.8 * hm]
    if len(dr) < 5: continue
    qual += 1
    if med([r['fillNew'] for r in dr]) <= med([r['fillNew'] for r in g]) - 0.01: ok1 += 1
    else: fails.append(k)
if qual == 0: print('F1 凶作の行が 5 行以上ある州が 0（前提なし・記録）')
else: print('F1 凶作で下がる：対象の州 %d・下がった州 %d（割合 %.3f）%s' % (qual, ok1, ok1 / qual, '→ OK' if ok1 / qual >= 0.9 else '→ FAIL'))
if fails: print('   下がらなかった州（先頭 10）：' + '、'.join('%s/%s' % k for k in fails[:10]))
# 追記 3：F1′（凶作の感度が保たれる）。F1 の字面は seeds 1〜10 で旗なしの反事実(fillBase)自身が通らなかった（0.427）＝判別力が無い線だったため、新しい種(11〜20)の判定は F1′ を使う。F1 は記録に残す。
#   (国,州) のうち凶作の行（harvestMean がその州の全行の中央値の 0.8 倍以下）が 5 行以上ある州の行を集め、農民の人口(peasantPopK)加重の平均の充足を 非凶作−凶作 で出す（dropBase：fillBase・dropNew：fillNew）。前提 dropBase≧0.01 のとき、dropNew≧0.5×dropBase なら通る。
sw = collections.defaultdict(float); sv = collections.defaultdict(float)
for k, g in grp.items():
    hm = med([r['harvestMean'] for r in g])
    if sum(1 for r in g if r['harvestMean'] <= 0.8 * hm) < 5: continue
    for r in g:
        c = 'd' if r['harvestMean'] <= 0.8 * hm else 'n'
        w_ = r['peasantPopK']
        sw[c] += w_; sv[(c, 'b')] += w_ * r['fillBase']; sv[(c, 'n')] += w_ * r['fillNew']
if sw['d'] > 0 and sw['n'] > 0:
    dropB = sv[('n', 'b')] / sw['n'] - sv[('d', 'b')] / sw['d']; dropN = sv[('n', 'n')] / sw['n'] - sv[('d', 'n')] / sw['d']
    if dropB < 0.01: print('F1′ 前提が成り立たない（旗なしの凶作の落ち込み dropBase %.4f＜0.01・記録）' % dropB)
    else: print('F1′ 凶作の感度：dropBase %.4f・dropNew %.4f・比 %.3f %s' % (dropB, dropN, dropN / dropB, '→ OK' if dropN >= 0.5 * dropB else '→ FAIL'))
else: print('F1′ 凶作の行が無い（記録）')
lt = [r['ownPerMonth'] / r['peasantNeedPerMonth'] for r in rows if r['unmetPerMonth'] > 0 and r['peasantNeedPerMonth'] > 0 and r['landFactor'] < 0.9]
gt = [r['ownPerMonth'] / r['peasantNeedPerMonth'] for r in rows if r['unmetPerMonth'] > 0 and r['peasantNeedPerMonth'] > 0 and r['landFactor'] >= 0.999]
if len(lt) < 30 or len(gt) < 30: print('F2 土地が詰まった行 %d・詰まっていない行 %d（30 に満たない＝記録）' % (len(lt), len(gt)))
else: print('F2 土地が詰まると細る：own÷need の中央値 landFactor＜0.9 の行 %.4f（%d 行）／≧0.999 の行 %.4f（%d 行）%s' % (med(lt), len(lt), med(gt), len(gt), '→ OK' if med(lt) < med(gt) else '→ FAIL'))

NINE = ('清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本', 'アメリカ')   # 事前登録の 9 国（追記 2 ③）＝判定はこの 9 国。pop_s の全国は記録で並べる
pb = load(base, 'pop_s', seeds=seeds)
def famine(rr, only=NINE): return sum(r['deathsFamineK'] for r in rr if r['scope'] == 'N' and (only is None or r['nation'] in only) and r['deathsFamineK'] == r['deathsFamineK'])
pa = load(d, 'pop_s', seeds=seeds)
nullseeds = set(range(1, 11))
pbn = load(nullbase, 'pop_s', seeds=nullseeds) if nullbase != base else pb   # ヌル腕は種 1〜10・比べる側も種 1〜10 の旗 0（種が違う腕でも幅は同じ物差しで読む）
p1 = load(n1d, 'pop_s', seeds=nullseeds); p2 = load(n2d, 'pop_s', seeds=nullseeds)
if pa and pb:
    fa, fb = famine(pa), famine(pb)
    fbn = famine(pbn)
    print('F3 凶作は残る：飢饉の死者の全期間の和（9 国×%d 種）B %.1f 千人・比べる側 %.1f 千人・B÷比べる側 %.3f（ヌル腕の比 N1 %.3f・N2 %.3f〔種 1〜10〕）%s' % (len(seeds), fa, fb, fa / fb if fb > 0 else float('nan'),
          famine(p1) / fbn if fbn > 0 and p1 else float('nan'), famine(p2) / fbn if fbn > 0 and p2 else float('nan'), '→ OK' if fb > 0 and fa / fb >= 0.5 else '→ FAIL'))
    print('F3 記録（pop_s の全国）：B %.1f・比べる側 %.1f・比 %.3f' % (famine(pa, None), famine(pb, None), famine(pa, None) / famine(pb, None) if famine(pb, None) > 0 else float('nan')))
else:
    print('F3 pop_s が読めない（腕 B か比べる側）＝読まない')

print('\n--- 壊す候補（AI の推奨・確証なし）')
def natpop(rr, y):
    out = {}
    for r in rr:
        if r['scope'] == 'N' and int(r['year']) == y and r['popK'] == r['popK']:
            out[(r['seed'], r['nation'])] = r['popK']   # 年内の最後の四半期の値が残る
    return out
flagged = {1850: [], 1900: []}
flagged_all = {1850: [], 1900: []}
for y in (1850, 1900):
    nb, na, nbn, n1p, n2p = natpop(pb, y), natpop(pa, y), natpop(pbn, y), natpop(p1, y), natpop(p2, y)
    rat = collections.defaultdict(lambda: ([], [], []))
    for k, v in na.items():
        if k in nb and nb[k] > 0:
            rat[k[1]][0].append(v / nb[k])
    for k, v in n1p.items():
        if k in nbn and nbn[k] > 0: rat[k[1]][1].append(v / nbn[k])
    for k, v in n2p.items():
        if k in nbn and nbn[k] > 0: rat[k[1]][2].append(v / nbn[k])
    print('P1 %d 年の人口 B÷比べる側（国別の種の中央値・[N1・N2]）：' % y)
    for n in sorted(rat):
        b_, a_, c_ = (med(x) for x in rat[n])
        spread = max(abs(a_ - 1) if a_ == a_ else 0, abs(c_ - 1) if c_ == c_ else 0, 0.02)
        up = (b_ - 1) > 2 * spread and b_ >= 1.10
        if up:
            flagged_all[y].append(n)
            if n in NINE: flagged[y].append(n)
        print('   %s%s\t%.3f\t[%.3f・%.3f]\t幅 %.3f%s' % ('★' if n in NINE else '', n, b_, a_, c_, spread, '\t上へ外れた' if up else ('\t下へ外れた' if (1 - b_) > 2 * spread else '')))
print('P1 判定（★の 9 国）：1900 年に上へ外れた国 %d（%s）%s' % (len(flagged[1900]), '、'.join(flagged[1900]), '→ 〈壊す候補〉（3 国以上）' if len(flagged[1900]) >= 3 else '→ 壊す候補に当たらない'))
print('P1 記録（pop_s の全国）：1850 年に上へ外れた国 %d・1900 年に上へ外れた国 %d（%s）' % (len(flagged_all[1850]), len(flagged_all[1900]), '、'.join(flagged_all[1900])))

def px(rr):
    return [r for r in rr if r.get('tier') == 'Staple' and r['priceOverRefTimesLevelMedianRatio'] == r['priceOverRefTimesLevelMedianRatio']]
xb, xa = px(load(base, 'pxgoods_s', seeds=seeds)), px(load(d, 'pxgoods_s', seeds=seeds))
if xa and xb:
    print('S2 価格：主食の priceOverRefTimesLevelMedianRatio＞100 の行 B %d／%d・比べる側 %d／%d・最大 B %.2f・比べる側 %.2f' % (
        sum(1 for r in xa if r['priceOverRefTimesLevelMedianRatio'] > 100), len(xa), sum(1 for r in xb if r['priceOverRefTimesLevelMedianRatio'] > 100), len(xb),
        max(r['priceOverRefTimesLevelMedianRatio'] for r in xa), max(r['priceOverRefTimesLevelMedianRatio'] for r in xb)))

print('\n--- 記録（採否に使わない）')
for tier in sorted(set(r['tier'] for r in rows)):
    for lab, lo, hi in (('1783-1850', 0, 1850), ('1851-1950', 1851, 9999)):
        sub = [r for r in rows if r['tier'] == tier and lo <= r['year'] <= hi]
        if not sub: continue
        pk = max(1e-9, sum(r['peasantPopK'] for r in sub))
        w = lambda k: wmed([(r[k], r['peasantPopK']) for r in sub])
        sticky_b = sum(r['peasantPopK'] for r in sub if r['fillBase'] >= 0.999) / pk
        sticky_n = sum(r['peasantPopK'] for r in sub if r['fillNew'] >= 0.999) / pk
        print('%s %s 行%d：農民の充足 base %.3f → new %.3f・idle %.3f・自給率 %.3f・収穫 %.3f・充足<0.8の農民の割合 base %.3f → new %.3f・充足≧0.999 の農民の割合 base %.3f → new %.3f'
              % (tier, lab, len(sub), w('fillBase'), w('fillNew'), w('idleShare'), w('subsistenceMean'), w('harvestMean'),
                 sum(r['belowBaseK'] for r in sub) / pk, sum(r['belowNewK'] for r in sub) / pk, sticky_b, sticky_n))
print('idle の分解（追記 2・行の中央値）：boost＝farmCap÷farmSizeProd・demand＝farmOut÷farmSizeProd（Output÷(Size×Productivity)）・O÷C')
def corr(xs, ys):
    pr = [(x, y) for x, y in zip(xs, ys) if x == x and y == y]
    if len(pr) < 10: return float('nan')
    mx = sum(p[0] for p in pr) / len(pr); my = sum(p[1] for p in pr) / len(pr)
    sxx = sum((p[0] - mx) ** 2 for p in pr); syy = sum((p[1] - my) ** 2 for p in pr); sxy = sum((p[0] - mx) * (p[1] - my) for p in pr)
    return sxy / math.sqrt(sxx * syy) if sxx > 0 and syy > 0 else float('nan')
for tier in sorted(set(r['tier'] for r in rows)):
    for lab, lo, hi in (('1783-1850', 0, 1850), ('1851-1950', 1851, 9999)):
        sub = [r for r in rows if r['tier'] == tier and lo <= r['year'] <= hi and r.get('farmSizeProdK', float('nan')) > 0 and r['farmCapPerMonth'] > 0]
        if not sub: continue
        bo = [r['farmCapPerMonth'] / r['farmSizeProdK'] for r in sub]; de = [r['farmOutPerMonth'] / r['farmSizeProdK'] for r in sub]
        oc = [r['farmOutPerMonth'] / r['farmCapPerMonth'] for r in sub]; idl = [r['idleShare'] for r in sub]
        print('%s %s 行%d：boost 中央値 %.3f・demand %.3f・O÷C %.3f・idle %.3f・相関 idle×log(boost) %.3f・idle×log(demand) %.3f' % (
            tier, lab, len(sub), med(bo), med(de), med(oc), med(idl), corr(idl, [math.log(x) if x > 0 else float('nan') for x in bo]), corr(idl, [math.log(x) if x > 0 else float('nan') for x in de])))
rb = load(base, 'region_s', seeds=seeds); ra = load(d, 'region_s', seeds=seeds)
def ff(rr):
    v = sorted(r['foodFillShare'] for r in rr if r['foodFillShare'] == r['foodFillShare'] and r.get('activeFlag', 1) == 1)
    if not v: return None
    m = sum(v) / len(v)
    return (len(v), m, math.sqrt(sum((x - m) ** 2 for x in v) / len(v)), v[int(0.05 * len(v))], v[len(v) // 2])
fb_, fa_ = ff(rb), ff(ra)
if fb_ and fa_:
    print('FoodFill（region_s の foodFillShare・全州×四半期）：比べる側 n=%d 平均 %.4f 標準偏差 %.4f p5 %.4f 中央値 %.4f／B n=%d 平均 %.4f 標準偏差 %.4f p5 %.4f 中央値 %.4f' % (fb_ + fa_))
