# (991) c90sl 紙の台本：今の盤面（keep_988/base_ins の landsale_s）から、B-1 段 1〜2 の門の下限と見込みを数える（読むだけ）。
# 使い方: python paper991.py <生のフォルダ> [種の数=10]
import sys, os, statistics as st
d = sys.argv[1]; ns = int(sys.argv[2]) if len(sys.argv) > 2 else 10
def f(x):
    try: return float(x)
    except: return float('nan')
agg = {}   # nation -> dict
rows = 0
for s in range(1, ns + 1):
    p = os.path.join(d, f'landsale_s{s}.tsv')
    with open(p, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in fh:
            v = ln.rstrip('\n').split('\t'); rows += 1
            n = v[ix['nation']]
            a = agg.setdefault(n, {'unpaid': 0.0, 'incl': 0.0, 'net': 0.0, 'nob': 0.0, 'nop': 0.0, 'seedshare': {}, 'lawq': 0, 'q': 0,
                                   'cover': [], 'over': [], 'incl_open': 0.0, 'claim': 0.0, 'paid': 0.0})
            un = f(v[ix['unpaidLandPerQuarter']]); inc = f(v[ix['saleIncludedUnpaidPerQuarter']]); net = f(v[ix['saleNetRentPerQuarter']])
            sh = f(v[ix['saleExpectedSharePerYear']]); law = f(v[ix['saleLawOpenFlag']])
            a['q'] += 1; a['lawq'] += 1 if law > 0 else 0
            a['unpaid'] += un; a['incl'] += inc; a['net'] += net
            a['nob'] += f(v[ix['saleBlockedNoBuyerPerQuarter']]); a['nop'] += f(v[ix['saleBlockedNoPricePerQuarter']])
            a['claim'] += f(v[ix['landClaimDeliveredPerQuarter']]); a['paid'] += f(v[ix['paidCopyPerQuarter']])
            if law > 0: a['incl_open'] += inc
            ss = a['seedshare'].setdefault(s, [0.0, 0.0])
            if net > 0 and sh == sh: ss[0] += sh * net; ss[1] += net
            cv = f(v[ix['buyerSavingsCoverShare']])
            if inc > 0 and cv == cv: a['cover'].append(cv)
            ov = f(v[ix['paidCopyOverEngine']])
            if f(v[ix['lowRpNationMonths']]) > 0 and ov == ov and ov > 0: a['over'].append(ov)
print(f'行 {rows}・国 {len(agg)}')
print('国\t未払いの和\t期待に入れた和\t法が開いた四半期の期待の和\t買い手なし\t値段なし\t年の割合の中央(種ごと)\t法が開いた割合\t被覆の中央\t被覆<1 の率\tcopy/engine の最小(rp<1 の行)\t未払い÷請求')
tot_incl = tot_open = 0.0
for n, a in sorted(agg.items(), key=lambda kv: -kv[1]['incl']):
    if a['unpaid'] <= 0: continue
    shares = [x[0] / x[1] for x in a['seedshare'].values() if x[1] > 0]
    med = st.median(shares) if shares else 0
    cov = a['cover']; cmed = st.median(cov) if cov else float('nan'); clow = sum(1 for x in cov if x < 1) / len(cov) if cov else float('nan')
    omin = min(a['over']) if a['over'] else float('nan')
    tot_incl += a['incl']; tot_open += a['incl_open']
    print(f"{n}\t{a['unpaid']:.6g}\t{a['incl']:.6g}\t{a['incl_open']:.6g}\t{a['nob']:.4g}\t{a['nop']:.4g}\t{med:.6g}\t{a['lawq']/a['q']:.3f}\t{cmed:.4g}\t{clow:.4f}\t{omin:.6g}\t{a['unpaid']/a['claim'] if a['claim']>0 else 0:.4g}")
print(f'全体：期待に入れた和 {tot_incl:.6g}・法が開いた四半期の分 {tot_open:.6g}（比 {tot_open/tot_incl if tot_incl>0 else 0:.4f}）')
