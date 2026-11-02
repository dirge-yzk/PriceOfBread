# (775) 紙の予言：m773 の木の生（nation_s の fxRate と monlaw_s の MonetaryStandard）から、
# 銀の国の E（fxRate）の天井・下限への張り付きを種ごとに数え直す。読むだけ。
#   python -I scripts/instruments/c7ag/paper_forecast.py <生のフォルダ> [種の数]
import sys, os, statistics
sys.stdout.reconfigure(encoding='utf-8', newline=chr(10))
raw = sys.argv[1]; nseed = int(sys.argv[2]) if len(sys.argv) > 2 else 10
HI, LO = 2.99, 0.31
def read(path, cols):
    with open(path, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        idx = [h.index(c) for c in cols]
        for line in f:
            p = line.rstrip('\n').split('\t')
            yield tuple(p[i] for i in idx)
tot_ns = tot_hi = tot_lo = 0
print('seed\tquarters\tqMedHi(>=%.2f)\tqMedLo(<=%.2f)\tqMedOut\tnatQ\tnatQHi(>=2.9)\tnatQLo(<=0.31)\tsilverNatQMonths\tmeanSilverNations' % (HI, LO))
sumout = []; lo_all = [0]; lo_ppp = [0]
for s in range(1, nseed + 1):
    fx = {}; pl = {}
    for q, ni, v, pv in read(os.path.join(raw, f'nation_s{s}.tsv'), ['quarter', 'nationIndex', 'fxRate', 'priceLevel']):
        fx[(int(q), int(ni))] = float(v); pl[(int(q), int(ni))] = float(pv)
    byq = {}
    for q, ni, law in read(os.path.join(raw, f'monlaw_s{s}.tsv'), ['quarter', 'nationIndex', 'MonetaryStandard']):
        if law == '1' and (int(q), int(ni)) in fx:
            byq.setdefault(int(q), []).append(fx[(int(q), int(ni))])
            if fx[(int(q), int(ni))] <= LO: lo_all[0] += 1; lo_ppp[0] += (pl[(int(q), int(ni))] >= 3.0)
    qs = sorted(byq)
    mh = sum(1 for q in qs if statistics.median(byq[q]) >= HI)
    ml = sum(1 for q in qs if statistics.median(byq[q]) <= LO)
    nq = sum(len(v) for v in byq.values())
    nh = sum(1 for v in byq.values() for x in v if x >= 2.9)
    nl = sum(1 for v in byq.values() for x in v if x <= 0.31)
    tot_ns += nq; tot_hi += nh; tot_lo += nl; sumout.append(mh + ml)
    print(f'{s}\t{len(qs)}\t{mh}\t{ml}\t{mh+ml}\t{nq}\t{nh}\t{nl}\t-\t{nq/max(1,len(qs)):.2f}')
print(f'ALL\tnatQ={tot_ns}\tshareHi={tot_hi/tot_ns:.4f}\tshareLo={tot_lo/tot_ns:.4f}')
print('qMedOut per seed:', sumout, ' seeds with <=10:', sum(1 for x in sumout if x <= 10), '/', len(sumout))
print('下限(<=0.31)の国x四半期のうち priceLevel>=3.0（PPP の経路でも下限に届く）の割合: %.3f  (%d / %d)' % (lo_ppp[0]/max(1,lo_all[0]), lo_ppp[0], lo_all[0]))
