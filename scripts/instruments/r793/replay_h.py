# (793) 形 H（依存を人の割合 h にする SIS 型）の紙の再生：旗0 の腕の州×月の sigma（POP の買った量/必要）を外から与えて h を回す（近似）
#   h' = h + G*x*(1-h) - D*h,  x = pc/d,  pc = sigma*(B*g + h*d)   ＝ h が必要量を変えても sigma は変わらないと置く（近似・需要が減ると配給は上がる側＝h を低めに見る向き）
#   行が無い月（在庫<=0.1 かつ pc<=0.05）は飛ばす（近似）。初期 h＝最初の行の在庫/d を [0,1] に切る。
import sys, glob, os
from collections import defaultdict
d_ins = sys.argv[1]; d = float(sys.argv[2]) if len(sys.argv) > 2 else 0.2
G, D, B = 0.1, 0.02, 0.02
for f in sorted(glob.glob(os.path.join(d_ins, 'r793_s*.tsv'))):
    seed = os.path.basename(f)[6:-4]
    fh = open(f, encoding='utf-8'); h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    ser = defaultdict(list)
    for line in fh:
        v = line.rstrip('\n').split('\t')
        need = float(v[ix['need']]); sig = float(v[ix['popSold']]) / need if need > 0 else 0.0
        ser[(v[ix['nation']], v[ix['region']])].append((float(v[ix['stockBefore']]), sig, float(v[ix['gate']]) if v[ix['gate']] not in ('NaN', 'nan') else 1.0))
    n = over25 = over50 = 0; hmax = 0.0; pcmax = 0.0; pin = 0; n24 = 0
    for k, rs in ser.items():
        hh = min(1.0, max(0.0, rs[0][0] / d))
        for t, (s0, sig, g) in enumerate(rs):
            pc = sig * (B * g + hh * d)
            hh = hh + G * (pc / d) * (1 - hh) - D * hh
            n += 1; over25 += hh > 0.25
            if t >= 24: n24 += 1; pin += hh * d >= 0.199
            over50 += hh > 0.5; hmax = max(hmax, hh); pcmax = max(pcmax, pc)
    print(f'種 {seed}：州×月 {n}・h>0.25 {over25}・h>0.5 {over50}・h の最大 {hmax:.4f}・pc の最大 {pcmax:.4f}・25 か月目以降で s=d*h>=0.199 の行 {pin}/{n24}（d={d}）')
