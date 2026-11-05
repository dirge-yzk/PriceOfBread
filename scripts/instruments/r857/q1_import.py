# (857) c70ea Q1：植民地格の輸入の代金を払った POP の内訳と、旗 1 で消えた分の振り分け（読むだけ・事前登録 prereg.txt §0〜§2）
#   python -I q1_import.py <出力の頭> <腕名=ins のフォルダ> ...   最初の腕が基準（旗 0＝keep_845/base_ins）
#   cearn_s から (種,四半期,国,州)→tier、cpop_s から州ごとの POP の列を拾う。tier=Colonial だけ。
import sys, os, statistics as st
from collections import defaultdict
EMPTY = 1e-9
outp = sys.argv[1]; arms = [a.split('=', 1) for a in sys.argv[2:]]
def tiers(d, s):
    m = {}
    with open(os.path.join(d, f'cearn_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); it = h.index('tier')
        for line in f:
            x = line.split('\t', it + 1)
            m[(x[0], x[2], x[3])] = x[it]
    return m
def regions(d, s):
    T = tiers(d, s); R = {}
    with open(os.path.join(d, f'cpop_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        iq, inat, ireg, icl, ipk, isv = h.index('quarter'), h.index('nation'), h.index('region'), h.index('className'), h.index('popK'), h.index('savings')
        iimp, icr, ilate = h.index('outImportPerQuarter'), h.index('inCreditPerQuarter'), h.index('lateRegistered')
        iom, iim = h.index('outMarketPerQuarter'), h.index('inMarketPerQuarter')
        for line in f:
            x = line.rstrip('\n').split('\t')
            k = (x[iq], x[inat], x[ireg])
            if T.get(k) != 'Colonial': continue
            r = R.get(k)
            if r is None:
                r = R[k] = {'imp': 0.0, 'impEmpty': 0.0, 'impLive': 0.0, 'mImp': 0.0, 'mCr': 0.0, 'mK': None, 'mSav': 0.0, 'byCls': defaultdict(float), 'om': 0.0, 'omEmpty': 0.0, 'nanImp': 0}
            pk = float(x[ipk]); imp = float(x[iimp]); cr = float(x[icr]); sv = float(x[isv]); om = float(x[iom])
            cl = x[icl]
            if imp != imp: r['nanImp'] += 1; continue
            empty = not (pk > EMPTY)
            r['imp'] += imp
            if empty: r['impEmpty'] += imp
            else: r['impLive'] += imp
            r['byCls'][(cl, 'E' if empty else 'L')] += imp
            if om == om:
                r['om'] += om
                if empty: r['omEmpty'] += om
            if cl == '商人':
                r['mImp'] += imp; r['mCr'] += cr if cr == cr else 0.0; r['mK'] = pk; r['mSav'] = sv if sv == sv else 0.0
    return R
L = []
def P(s=''): L.append(s); print(s, flush=True)
data = {}
for name, d in arms:
    data[name] = {s: regions(d, s) for s in range(1, 11)}
    P(f'# 読んだ {name} = {d}')
# 表 1：誰が払ったか（腕ごと・植民地格の全ての州×四半期の和）
P('表1\tarm\tclassName\tempty\toutImportSum\tshare')
for name, _ in arms:
    tot = 0.0; by = defaultdict(float); nan = 0
    for s, R in data[name].items():
        for r in R.values():
            tot += r['imp']; nan += r['nanImp']
            for k, v in r['byCls'].items(): by[k] += v
    for k, v in sorted(by.items(), key=lambda kv: -abs(kv[1]))[:8]:
        P(f'表1\t{name}\t{k[0]}\t{k[1]}\t{v:.6g}\t{v / tot if tot else float("nan"):.4f}')
    P(f'表1\t{name}\tALL\t-\t{tot:.6g}\t1\tnanRows={nan}')
# 表 1b：商人の人数の状態の州×四半期の割合と輸入の和（腕ごと）
P('表1b\tarm\tmerchantState\tregionQuarters\tshareRQ\toutImportSum\tshareImp\tmerchantSavingsSum\tmerchantInCreditSum')
for name, _ in arms:
    agg = defaultdict(lambda: [0, 0.0, 0.0, 0.0]); tot = 0.0; n = 0
    for s, R in data[name].items():
        for r in R.values():
            st_ = 'none' if r['mK'] is None else ('empty' if not (r['mK'] > EMPTY) else 'live')
            a = agg[st_]; a[0] += 1; a[1] += r['imp']; a[2] += r['mSav']; a[3] += r['mCr']; tot += r['imp']; n += 1
    for k, a in sorted(agg.items()):
        P(f'表1b\t{name}\t{k}\t{a[0]}\t{a[0] / n:.4f}\t{a[1]:.6g}\t{a[1] / tot if tot else float("nan"):.4f}\t{a[2]:.6g}\t{a[3]:.6g}')
# 表 2：同じ鍵の組を、基準（最初の腕）と腕の商人の状態で分けて、輸入の和の比と中央・腕の 1/10 未満の割合
base = arms[0][0]
P('表2\tarm\tbaseMerchant\tarmMerchant\tnPairs\tbaseImpSum\tarmImpSum\tsumRatio\tmedianRatio(both>0)\tshareArmBelowTenthOfBase(base>0)\tarmMerchantInCreditSum\tbaseMerchantInCreditSum')
for name, _ in arms[1:]:
    g = defaultdict(lambda: [0, 0.0, 0.0, [], 0, 0, 0.0, 0.0])
    for s in range(1, 11):
        A = data[name][s]; B = data[base][s]
        for k, ra in A.items():
            rb = B.get(k)
            if rb is None: continue
            sb = 'none' if rb['mK'] is None else ('empty' if not (rb['mK'] > EMPTY) else 'live')
            sa = 'none' if ra['mK'] is None else ('empty' if not (ra['mK'] > EMPTY) else 'live')
            for key in [(sb, sa), ('ALL', 'ALL')]:
                x = g[key]; x[0] += 1; x[1] += rb['imp']; x[2] += ra['imp']
                if ra['imp'] > 0 and rb['imp'] > 0: x[3].append(ra['imp'] / rb['imp'])
                if rb['imp'] > 0:
                    x[4] += 1
                    if ra['imp'] < 0.1 * rb['imp']: x[5] += 1
                x[6] += ra['mCr']; x[7] += rb['mCr']
    for key, x in sorted(g.items()):
        P(f'表2\t{name}\t{key[0]}\t{key[1]}\t{x[0]}\t{x[1]:.6g}\t{x[2]:.6g}\t{x[2] / x[1] if x[1] else float("nan"):.4g}\t{st.median(x[3]) if x[3] else float("nan"):.4g}\t{x[5] / x[4] if x[4] else float("nan"):.4f}\t{x[6]:.6g}\t{x[7]:.6g}')
# 表 2b：輸入の和の減り（基準−腕）を、基準の商人の状態ごとに割り振る（減った額の何割がどの州から）
P('表2b\tarm\tbaseMerchant\tdropSum(base-arm)\tshareOfTotalDrop')
for name, _ in arms[1:]:
    d = defaultdict(float); tot = 0.0
    for s in range(1, 11):
        A = data[name][s]; B = data[base][s]
        for k, ra in A.items():
            rb = B.get(k)
            if rb is None: continue
            sb = 'none' if rb['mK'] is None else ('empty' if not (rb['mK'] > EMPTY) else 'live')
            dd = rb['imp'] - ra['imp']; d[sb] += dd; tot += dd
    for k, v in sorted(d.items()):
        P(f'表2b\t{name}\t{k}\t{v:.6g}\t{v / tot if tot else float("nan"):.4f}')
    P(f'表2b\t{name}\tALL\t{tot:.6g}\t1')
# 表 2c：旗 1 の腕（2 番目）で、人の居ない商人の州（腕の側）の時系列＝四半期ごとの輸入と商人の借り入れ（種ごとの和・40 四半期ごと）
if len(arms) > 1:
    name = arms[1][0]
    P(f'表2c\t{name}\tseed\tqBin\tregionQuartersEmptyMerchant\tarmImp\tbaseImpSameKeys\tarmMerchInCredit\tarmMerchSavings')
    for s in range(1, 11):
        A = data[name][s]; B = data[base][s]; bins = defaultdict(lambda: [0, 0.0, 0.0, 0.0, 0.0])
        for k, ra in A.items():
            if ra['mK'] is None or ra['mK'] > EMPTY: continue
            rb = B.get(k); b = bins[int(k[0]) // 80]
            b[0] += 1; b[1] += ra['imp']; b[2] += rb['imp'] if rb else 0.0; b[3] += ra['mCr']; b[4] += ra['mSav']
        for qb in sorted(bins):
            b = bins[qb]; P(f'表2c\t{name}\t{s}\t{qb * 80}-{qb * 80 + 79}\t{b[0]}\t{b[1]:.6g}\t{b[2]:.6g}\t{b[3]:.6g}\t{b[4]:.6g}')
with open(outp + '.tsv', 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(L) + '\n')
