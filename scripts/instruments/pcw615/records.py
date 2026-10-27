#!/usr/bin/env python
# (615) 線 pcw：事前登録 [4] の記録（読むだけ・採否に使わない）。
# 使い方：PYTHONUTF8=1 python scripts/instruments/pcw615/records.py --arm <腕 B の ins> --base <基線> --null <N1> <N2>
import argparse, csv, glob, os, statistics, collections
ERAS = [('1783-1850', 0, 268), ('1850-1913', 268, 520), ('1913-1950', 520, 10**6)]
def era(q):
    for nm, a, b in ERAS:
        if a <= q < b: return nm
def rows(arm):
    for rp in sorted(glob.glob(os.path.join(arm, 'reform_s*.tsv'))):
        by = collections.defaultdict(list)
        with open(rp, encoding='utf-8') as f:
            for d in csv.DictReader(f, delimiter='\t'): by[d['nation']].append(d)
        s = os.path.basename(rp)[len('reform_s'):-4]
        for n, rs in by.items():
            if len(rs) >= 200: yield s, n, rs
def med(xs): return statistics.median(xs) if xs else float('nan')
def one(name, arm):
    ev = collections.Counter(); leg = collections.defaultdict(list); post = []; pol = collections.Counter(); sb = [0, 0, 0]
    coop = collections.defaultdict(list); mil = collections.defaultdict(list); stock = collections.defaultdict(list); lawchg = collections.Counter()
    jp = []; minleg = 1.0; nseed = set()
    for s, n, rs in rows(arm):
        nseed.add(s); prev = None; prevlaw = None; prevpol = None
        for i, d in enumerate(rs):
            q = int(d['quarter']); e = era(q)
            L = float(d['legitimacyShare']); leg[e].append(L); minleg = min(minleg, L)
            st = float(d['mandateStockIndex']); mx = float(d['mandateMaxIndex'])
            stock[e].append(st); coop[e].append(float(d['coopParliamentCrownIndex'])); mil[e].append(float(d['militaryPushIndex']))
            sb[0] += 1
            if mx > 0 and st > mx * (1 + 1e-9):
                sb[1] += 1
                if 'concedeSpendIndex' in d and float(d['concedeSpendIndex']) > 1e-12: sb[2] += 1
            if int(d['wallPassCount']) > 0:
                ev[(n, e)] += 1
                post.append(min(float(x['legitimacyShare']) for x in rs[i + 1:i + 9]) if i + 1 < len(rs) else float('nan'))
            pc = d['polityCode']
            if prevpol is not None and pc != prevpol: pol[e] += 1
            prevpol = pc
            laws = tuple(d[k] for k in ('educationLawCode', 'suffrageLawCode', 'censorshipLawCode', 'landReformLawCode', 'bureaucracyLawCode', 'serfLawCode'))
            if prevlaw is not None and laws != prevlaw: lawchg[e] += 1
            prevlaw = laws
            if n == '日本' and 'educationLawCode' in d:
                if prev is not None and d['educationLawCode'] != prev: jp.append((d['year'], prev, d['educationLawCode']))
                prev = d['educationLawCode']
    print('== %s（種 %d）' % (name, len(nseed)))
    print('  壁の一歩の出来事（9 国・年代別）：' + ' '.join('%s=%d' % (nm, sum(v for (n, e), v in ev.items() if e == nm)) for nm, _, _ in ERAS))
    print('  正統性の中央値（年代別）：' + ' '.join('%s=%.3f' % (nm, med(leg[nm])) for nm, _, _ in ERAS) + '／全体の最小 %.4f／壁の一歩の後 8 四半期の最小の中央値 %.3f' % (minleg, med([x for x in post if x == x])))
    print('  政体の変化の回数：' + ' '.join('%s=%d' % (nm, pol[nm]) for nm, _, _ in ERAS) + '／法（教育・参政権・検閲・土地・官僚制・農奴）の変化の回数：' + ' '.join('%s=%d' % (nm, lawchg[nm]) for nm, _, _ in ERAS))
    print('  S＞b の行の割合 %.4f（講和の列がある腕でのうち concede 正の割合 %.4f）' % (sb[1] / sb[0], sb[2] / max(1, sb[1])))
    print('  mandateStockIndex の中央値：' + ' '.join('%s=%.3f' % (nm, med(stock[nm])) for nm, _, _ in ERAS))
    print('  coopParliamentCrownIndex の中央値：' + ' '.join('%s=%.3f' % (nm, med(coop[nm])) for nm, _, _ in ERAS) + '／militaryPushIndex：' + ' '.join('%s=%.3f' % (nm, med(mil[nm])) for nm, _, _ in ERAS))
    print('  日本の educationLawCode の変化（年・旧→新）：' + ('; '.join('%s:%s→%s' % x for x in jp[:12]) if jp else 'なし') + ('（他 %d）' % (len(jp) - 12) if len(jp) > 12 else ''))
if __name__ == '__main__':
    ap = argparse.ArgumentParser(); ap.add_argument('--arm', required=True); ap.add_argument('--base', required=True); ap.add_argument('--null', nargs='*', default=[])
    a = ap.parse_args()
    one('腕B', a.arm); one('基線', a.base)
    for i, p in enumerate(a.null): one('N%d' % (i + 1), p)
