#!/usr/bin/env python
# (615) 線 pcw：README 110 の〈壊す〉の物差し ①②③ の読み手（読むだけ）。事前登録の追記（measurements/pcw615_20261028_prereg_addendum.txt）の定義のとおり。
# 使い方：PYTHONUTF8=1 python scripts/instruments/pcw615/broke_lines.py --arm <腕 B の ins> --base <基線 m609_ins> --null <N1> <N2>
# ①同じ国が毎回壊れる：国×種で 人口が（それまでの最大の）半分以下に落ちたら壊れた。壊れた種が B で 8/10 以上かつ基線・N1・N2 では最大 2/10 以下の国があれば該当。
# ②一国の発散：種ごとに 四半期ごとの 首位の国の 世界の実質GDP の取り分 の時間の中央値。腕の平均が 基線・N1・N2 の最大を（3本の幅の 2 倍）以上超え、かつ 8/10 以上の種で首位が同じ国なら該当。
# ③片方向の錠：reform_s の挙げた国で mandateStockIndex≧mandateMaxIndex（在庫が満タン以上に張り付く）の 国×四半期 の割合が 基線・N1・N2 の最大の 2 倍以上なら該当。
import argparse, csv, glob, os, statistics, collections

def seeds(arm):
    return sorted(int(os.path.basename(p)[len('nation_s'):-4]) for p in glob.glob(os.path.join(arm, 'nation_s*.tsv')))

def read_nation(arm, s):
    pop = collections.defaultdict(list); gdp = collections.defaultdict(dict)
    with open(os.path.join(arm, 'nation_s%d.tsv' % s), encoding='utf-8') as f:
        for d in csv.DictReader(f, delimiter='\t'):
            q = int(d['quarter']); n = d['nation']
            try: p = float(d['populationK']); g = float(d['realGdpPerYear'])
            except ValueError: continue
            pop[n].append(p); gdp[q][n] = g
    return pop, gdp

def broken_and_share(arm):
    ss = seeds(arm); broke = collections.defaultdict(set); shares = []; tops = []
    for s in ss:
        pop, gdp = read_nation(arm, s)
        for n, v in pop.items():
            if len(v) < 100: continue
            peak = 0.0
            for x in v:
                peak = max(peak, x)
                if peak > 0 and x <= 0.5 * peak: broke[n].add(s); break
        sh = []; top = collections.Counter()
        for q, m in gdp.items():
            tot = sum(v for v in m.values() if v > 0)
            if len(m) < 50 or tot <= 0: continue
            n, v = max(m.items(), key=lambda kv: kv[1]); sh.append(v / tot); top[n] += 1
        shares.append(statistics.median(sh) if sh else float('nan')); tops.append(top.most_common(1)[0][0] if top else '')
    return ss, broke, shares, tops

def lock_frac(arm):
    num = den = 0
    for rp in glob.glob(os.path.join(arm, 'reform_s*.tsv')):
        rows = collections.defaultdict(list)
        with open(rp, encoding='utf-8') as f:
            for d in csv.DictReader(f, delimiter='\t'): rows[d['nation']].append(d)
        for n, rs in rows.items():
            if len(rs) < 200: continue
            for d in rs:
                den += 1
                if float(d['mandateMaxIndex']) > 0 and float(d['mandateStockIndex']) >= float(d['mandateMaxIndex']) * (1 - 1e-9): num += 1
    return num / den if den else float('nan')

def main():
    ap = argparse.ArgumentParser(); ap.add_argument('--arm', required=True); ap.add_argument('--base', required=True); ap.add_argument('--null', nargs='*', default=[])
    a = ap.parse_args()
    refs = [('基線', a.base)] + [('N%d' % (i + 1), p) for i, p in enumerate(a.null)]
    R = {nm: broken_and_share(p) for nm, p in refs}; B = broken_and_share(a.arm)
    ns = len(B[0])
    print('① 同じ国が毎回壊れる（人口がそれまでの最大の半分以下）：B の種数 %d' % ns)
    hit1 = []
    for n in sorted(B[1]):
        b = len(B[1][n]); rr = [len(R[nm][1].get(n, ())) for nm, _ in refs]
        if b >= 0.8 * ns and max(rr) <= 0.2 * max(len(R[nm][0]) for nm, _ in refs): hit1.append(n)
        if b >= 3 or max(rr) >= 3: print('  %s B=%d %s' % (n, b, ' '.join('%s=%d' % (nm, x) for (nm, _), x in zip(refs, rr))))
    print('  ① → %s' % ('該当：' + ','.join(hit1) if hit1 else '該当なし'))
    means = {nm: statistics.mean(R[nm][2]) for nm, _ in refs}; mb = statistics.mean(B[2])
    width = max(means.values()) - min(means.values())
    topc = collections.Counter(B[3]).most_common(1)[0]
    hit2 = mb >= max(means.values()) + 2 * width and topc[1] >= 0.8 * ns
    print('② 首位の取り分の中央値の平均：B %.4f／%s／幅 %.4f／B の首位の最頻 %s %d/%d → %s' % (mb, ' '.join('%s=%.4f' % kv for kv in means.items()), width, topc[0], topc[1], ns, '該当' if hit2 else '該当なし'))
    lf = {nm: lock_frac(p) for nm, p in refs}; lb = lock_frac(a.arm)
    hit3 = lb >= 2 * max(lf.values()) and lb > 0
    print('③ 在庫が満タン以上に張り付く 国×四半期 の割合：B %.5f／%s → %s' % (lb, ' '.join('%s=%.5f' % kv for kv in lf.items()), '該当' if hit3 else '該当なし'))

if __name__ == '__main__':
    main()
