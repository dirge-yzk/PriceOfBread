#!/usr/bin/env python
# (615) 線 pcw の読み手（読むだけ）。事前登録 measurements/pcw615_20261028_prereg.txt [3] の定義のとおり。
# 使い方：PYTHONUTF8=1 python scripts/instruments/pcw615/g_lines.py --arm <腕 B の ins フォルダ> --base <基線 m609_ins> --null <N1> <N2> [--armA <腕 A の ins フォルダ>]
#   各フォルダには reform_s<seed>.tsv と nation_s<seed>.tsv がある（INS=1 batch の出力）。
# 出来事＝reform_s の挙げた国（毎四半期の行がある国）で wallPassCount>0 の行（四半期 q0）。
# 再開＝q0+1 以降で nation_s の同じ国の levelLawPaidInQuarter+ratePaidInQuarter+continuousPaidInQuarter が正の最初の四半期 q1。長さ＝q1−q0。q1 が無ければ打ち切り（終わりまでの長さ）。
# G1：腕 B の中央値・90 分位が 基線・N1・N2 の最小−その幅の2倍 より小さく、出来事が 100 件以上。G2：腕 B の spendStockShareOfMaxMax の最大 ≦ 1+1e-9（腕 A では 1 を超える行がある）。
# G3：腕 B の出来事 ≧100 件かつ spendLegitOnlyIndex が正の行がある。
import argparse, csv, collections, glob, os, statistics, sys

def pct(xs, p):
    xs = sorted(xs)
    return xs[min(len(xs) - 1, int(p * (len(xs) - 1) + 0.5))] if xs else float('nan')

def load_paid(path):
    paid = {}
    with open(path, encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r)
        iq, inn = h.index('quarter'), h.index('nation')
        cols = [h.index(c) for c in ('levelLawPaidInQuarter', 'ratePaidInQuarter', 'continuousPaidInQuarter')]
        for row in r:
            v = 0.0
            for c in cols:
                try: v += float(row[c])
                except ValueError: pass
            paid[(row[inn], int(row[iq]))] = v
    return paid

def events(arm):
    """[(seed, nation, q0, 長さ, 打ち切りか)]"""
    out = []
    for rp in sorted(glob.glob(os.path.join(arm, 'reform_s*.tsv'))):
        s = os.path.basename(rp)[len('reform_s'):-4]
        np_ = os.path.join(arm, 'nation_s' + s + '.tsv')
        if not os.path.exists(np_): continue
        paid = load_paid(np_)
        rows = collections.defaultdict(list)
        with open(rp, encoding='utf-8') as f:
            for d in csv.DictReader(f, delimiter='\t'): rows[d['nation']].append(d)
        for n, rs in rows.items():
            if len(rs) < 200: continue          # 毎四半期の行がある挙げた国だけ（他は年末の行）
            qmax = int(rs[-1]['quarter'])
            for d in rs:
                if int(d['wallPassCount']) > 0:
                    q0 = int(d['quarter']); t = None
                    for qq in range(q0 + 1, qmax + 1):
                        if paid.get((n, qq), 0.0) > 1e-12: t = qq - q0; break
                    cens = t is None
                    out.append((s, n, q0, (qmax - q0) if cens else t, cens))
    return out

def summarize(name, ev):
    L = [e[3] for e in ev]
    return dict(name=name, n=len(L), cens=sum(e[4] for e in ev), med=pct(L, .5), p90=pct(L, .9), mean=statistics.mean(L) if L else float('nan'))

def spend_cols(arm):
    mx, legit_rows, rows = 0.0, 0, 0
    for rp in sorted(glob.glob(os.path.join(arm, 'reform_s*.tsv'))):
        with open(rp, encoding='utf-8') as f:
            for d in csv.DictReader(f, delimiter='\t'):
                if 'spendStockShareOfMaxMax' not in d: return None
                rows += 1
                mx = max(mx, float(d['spendStockShareOfMaxMax']))
                if float(d['spendLegitOnlyIndex']) > 1e-12: legit_rows += 1
    return dict(maxshare=mx, legit_rows=legit_rows, rows=rows)

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--arm', required=True); ap.add_argument('--base', required=True)
    ap.add_argument('--null', nargs='*', default=[]); ap.add_argument('--armA')
    a = ap.parse_args()
    refs = [summarize('基線', events(a.base))] + [summarize('N%d' % (i + 1), events(p)) for i, p in enumerate(a.null)]
    B = summarize('腕B', events(a.arm))
    print('出来事（壁の一歩の通過）：名前 / 件数 / 打ち切り / 再開までの長さの中央値 / 90分位 / 平均')
    for s in refs + [B]:
        print('  %-4s n=%d cens=%d med=%g p90=%g mean=%.1f' % (s['name'], s['n'], s['cens'], s['med'], s['p90'], s['mean']))
    if a.armA:
        A = summarize('腕A', events(a.armA))
        print('  %-4s n=%d cens=%d med=%g p90=%g mean=%.1f（5種）' % (A['name'], A['n'], A['cens'], A['med'], A['p90'], A['mean']))
    meds = [s['med'] for s in refs]; p90s = [s['p90'] for s in refs]
    line_med = min(meds) - 2 * (max(meds) - min(meds)); line_p90 = min(p90s) - 2 * (max(p90s) - min(p90s))
    g1 = B['n'] >= 100 and B['med'] < line_med and B['p90'] < line_p90
    print('G1 線：中央値 < %g・90分位 < %g・件数 >= 100 → 腕B 中央値 %g・90分位 %g・件数 %d → %s' % (line_med, line_p90, B['med'], B['p90'], B['n'],
          ('判定不能（件数不足）' if B['n'] < 100 else ('PASS' if g1 else 'FAIL'))))
    sb = spend_cols(a.arm)
    if sb is None:
        print('G2/G3：腕Bの reform_s に spendStockShareOfMaxMax が無い（旧の木の生）')
        return
    g2 = sb['maxshare'] <= 1 + 1e-9
    print('G2 番人：腕B spendStockShareOfMaxMax の最大 %.6g → %s' % (sb['maxshare'], 'PASS' if g2 else 'FAIL'), end='')
    if a.armA:
        sa = spend_cols(a.armA)
        if sa: print('（腕A の最大 %.6g：1 を超えるのが対照）' % sa['maxshare'], end='')
    print()
    g3 = B['n'] >= 100 and sb['legit_rows'] >= 1
    print('G3 番人：出来事 %d 件・spendLegitOnlyIndex が正の行 %d → %s' % (B['n'], sb['legit_rows'], 'PASS' if g3 else 'FAIL'))
    # 国ごとの表（採否に使わない）
    print('国ごと（件数 / 中央値）：')
    by = collections.defaultdict(lambda: collections.defaultdict(list))
    for nm, arm in [('基線', a.base)] + [('N%d' % (i + 1), p) for i, p in enumerate(a.null)] + [('腕B', a.arm)]:
        for e in events(arm): by[e[1]][nm].append(e[3])
    for n in sorted(by):
        print('  %s ' % n + ' '.join('%s=%d/%g' % (nm, len(by[n][nm]), pct(by[n][nm], .5)) for nm in by[n]))
    print('G5：G1・G2・G3 が全て PASS なら 働いた（G4＝止める線は別に読む）')

if __name__ == '__main__':
    main()
