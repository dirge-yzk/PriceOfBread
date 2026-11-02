# (793) 読み手 r793：プローブ r793_s*.tsv の集計（読むだけ）。python an_r793.py <ins フォルダ> [閾値=3.3]
import sys, glob, os, statistics as st
from collections import Counter, defaultdict
d = sys.argv[1]; TH = float(sys.argv[2]) if len(sys.argv) > 2 else 3.3
G, D = 0.1, 0.02
rows = []
for f in sorted(glob.glob(os.path.join(d, 'r793_s*.tsv'))):
    seed = os.path.basename(f)[6:-4]
    with open(f, encoding='utf-8') as fh:
        hdr = fh.readline().rstrip('\n').split('\t')
        for line in fh:
            v = line.rstrip('\n').split('\t')
            r = dict(zip(hdr, v)); r['seed'] = seed
            for k in hdr[5:]:
                if k not in ('nation', 'region'):
                    try: r[k] = float(r[k])
                    except: pass
            rows.append(r)
print('行', len(rows), '（stockBefore>0.1 か pc>0.05 の州×月・助走は除く）')
nz = Counter()
for r in rows:
    for k in ('inputNeed', 'invest', 'milRebel', 'trickle', 'eff', 'effSold'):
        if r[k] != 0: nz[k] += 1
print('POP 以外の買い手が 0 でない行:', dict(nz) if nz else 'なし（全行 0）')
mis = sum(1 for r in rows if abs(r['sold'] - r['popSold']) > 1e-9 * max(1, r['sold']))
print('sold ≠ popDemand×ration の行:', mis)
def q(xs, p):
    xs = sorted(xs); return xs[min(len(xs)-1, int(p*len(xs)))] if xs else float('nan')
for lab, sel in (('全行', rows), ('在庫>%.1f' % TH, [r for r in rows if r['stockBefore'] > TH]), ('在庫 0.1〜0.2', [r for r in rows if 0.1 < r['stockBefore'] <= 0.2]), ('在庫 0.2〜%.1f' % TH, [r for r in rows if 0.2 < r['stockBefore'] <= TH])):
    if not sel: print(lab, 0); continue
    sig = [r['popSold'] / r['need'] for r in sel if r['need'] > 0]
    bnd = [r['boundShare'] for r in sel]
    rat = [r['ration'] for r in sel]
    pr = [r['usePrice'] / r['refPrice'] for r in sel]
    pf = [r['priceFactor'] for r in sel]
    sp = [r['popSold'] * r['buyerPrice'] / r['earned'] for r in sel if r['earned'] > 0]
    nonpop = [1 - r['popSold'] / r['sold'] for r in sel if r['sold'] > 0]
    grow = sum(1 for r in sel if G * r['pc'] - D * r['stockBefore'] > 0)
    growpop = sum(1 for r in sel if G * r['popSold'] / r['totalPop'] - D * r['stockBefore'] > 0)
    print(f'[{lab}] 行 {len(sel)}')
    print(f'   sigma=POPの買った量/必要  中央 {st.median(sig):.3f} p10 {q(sig,.1):.3f} p90 {q(sig,.9):.3f}  ｜ sigma>0.2 の割合 {sum(1 for x in sig if x>0.2)/len(sig):.3f}')
    print(f'   予算が縛る人口の割合 中央 {st.median(bnd):.3f} p10 {q(bnd,.1):.3f} p90 {q(bnd,.9):.3f}  ｜ 配給 中央 {st.median(rat):.3f} p10 {q(rat,.1):.3f}')
    print(f'   値段/参照 中央 {st.median(pr):.3f} p10 {q(pr,.1):.3f} p90 {q(pr,.9):.3f} ｜ priceFactor 中央 {st.median(pf):.3f}')
    print(f'   アヘンの支出/所得 中央 {st.median(sp) if sp else float("nan"):.3f} p90 {q(sp,.9):.3f} ｜ POP 以外の取り分 最大 {max(nonpop) if nonpop else 0:.3g}')
    print(f'   在庫が増える月（全体の pc） {grow}/{len(sel)} ｜ POP だけの pc でも増える月 {growpop}/{len(sel)}')
hi = [r for r in rows if r['stockBefore'] > TH]
if hi:
    c = Counter(r['nation'] + '/' + r['region'] for r in hi)
    print('在庫>%.1f の州（上位 15）:' % TH, c.most_common(15))
    # 判定の表：(i) POP 以外が無ければ増えない／(ii) POP だけでも増えるか釣り合う
    cls = Counter()
    for r in hi:
        pcpop = r['popSold'] / r['totalPop']
        nonpop = 1 - r['popSold'] / r['sold'] if r['sold'] > 0 else 0
        sustain_pop = G * pcpop >= D * r['stockBefore'] * 0.98
        sustain_all = G * r['pc'] >= D * r['stockBefore'] * 0.98
        if nonpop > 1e-9 and sustain_all and not sustain_pop: cls['(i) 分子の食い違いだけで保つ'] += 1
        elif sustain_pop: cls['(ii) POP だけで増えるか保つ'] += 1
        else: cls['減っている途中（どちらでも保たない）'] += 1
    print('判定（在庫>%.1f の州×月）:' % TH, dict(cls))
    eq = [r['stockBefore'] / (5 * r['pc']) for r in hi if r['pc'] > 0]
    print('在庫/(5×pc)（1 未満＝まだ増える・1＝釣り合い） 中央 %.3f p10 %.3f p90 %.3f' % (st.median(eq), q(eq, .1), q(eq, .9)))
    mx = max(hi, key=lambda r: r['stockBefore'])
    print('最大の行:', {k: mx[k] for k in ('seed', 'year', 'nation', 'region', 'totalPop', 'stockBefore', 'pc', 'popSold', 'need', 'usePrice', 'refPrice', 'priceFactor', 'ration', 'boundShare', 'budget', 'earned', 'gate')})
# 全行の在庫の最大と種ごとの最大
bys = defaultdict(float)
for r in rows: bys[r['seed']] = max(bys[r['seed']], r['stockBefore'])
print('種ごとの在庫の最大:', dict(bys))
# 門の素案の参考：種ごとの〈実際の在庫が 0.2・0.25 を超えた州×月〉と pc の最大
c2 = defaultdict(int); c25 = defaultdict(int); pcm = defaultdict(float)
for r in rows:
    if r['stockBefore'] > 0.2 + 1e-12: c2[r['seed']] += 1
    if r['stockBefore'] > 0.25: c25[r['seed']] += 1
    pcm[r['seed']] = max(pcm[r['seed']], r['pc'])
print('実際の在庫>0.2 の州×月（種ごと）:', dict(c2), '／>0.25:', dict(c25), '／pc の最大:', {k: round(v, 3) for k, v in pcm.items()})
fl = sum(1 for r in rows if r['priceFactor'] <= 0.4 + 1e-9)
print('priceFactor が下端 0.4（PriceFactorMin）の行: %d/%d（値段が上がっても需要の倍率はこれ以上下がらない）' % (fl, len(rows)))
