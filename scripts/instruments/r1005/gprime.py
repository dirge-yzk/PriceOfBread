# (1005) 開幕で定額の国（清・イギリス）の γ′（f・R をエンジンの凍結の写し bxFrozenPbPerMonth と同じ RGO の今の地代 bxRentOfFrozenPbPerMonth に）で、φ* の影（q≥0・s＝0.12）が 0.5 を下へまたぐ年。latch_read.py の series・follow・t_gamma・metrics を読み込んで使う。
# 使い方：python -X utf8 -I gprime.py <書き先> <latch_read.py のあるフォルダ> <腕>=<フォルダ> ...
import sys, statistics
sys.path.insert(0, sys.argv[2]); import latch_read as LR
out = sys.argv[1]; arms = [a.split('=', 1) for a in sys.argv[3:]]
L = ['# gprime（(1005)）：開幕で定額の国の γ′（エンジンの凍結の写しの対）で φ* の影（q≥0）', '腕\t国\t0.5 を下へまたいだ種\t下へまたいだ年(種ごと)\tΣ|Δφ*| の中央\tφ*@1850 中央\tφ*@1900 中央\tφ*@1941 中央']
for name, ins in arms:
    D = LR.series(ins)
    for nat in ('清', 'イギリス'):
        dn = []; tv = []; ph50 = []; ph00 = []; ph41 = []
        for s in D.get(nat, []):
            T = [LR.t_gamma(r, x, f) if r > 0 else LR.NAN for f, r, x in zip(s['fz'], s['rfz'], s['pi'])]
            ph = LR.follow(T, s['q'], 1.0, 0); c, t = LR.metrics(ph, s['q'], 0); tv.append(t)
            d = '-'
            for i in range(1, len(ph)):
                if ph[i - 1] >= 0.5 > ph[i]: d = str(s['y'][i]); break
            dn.append(d)
            for acc, y0 in ((ph50, 1850), (ph00, 1900), (ph41, 1941)):
                i = LR.year_at(s, y0)
                if i is not None: acc.append(ph[i])
        L.append('%s\t%s\t%d/%d\t%s\t%.3g\t%.2f\t%.2f\t%.2f' % (name, nat, sum(1 for d in dn if d != '-'), len(dn), ','.join(dn), statistics.median(tv), statistics.median(ph50), statistics.median(ph00), statistics.median(ph41)))
open(out, 'wb').write(('\n'.join(L) + '\n').encode('utf-8'))
