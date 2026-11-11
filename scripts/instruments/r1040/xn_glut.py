# (1040) c98xn 設計の材料の台本（読むだけ・エンジンの関数を呼ばない・既にある生だけ）
# 問い1・2 の補い：国×財で xNeed≦0（閉じる側）のますの余り −xNeed を〈在庫の余り S−TargetStock〉と〈流れの余り P−D〉に分ける。
#   −xNeed ＝ (S − TS) + (P − D)（BilateralMarket.cs:1041 の式の並べ替え）。
# 使い方: python xn_glut.py <keep の base_ins> <出力 tsv>
# ⚠限界は xn_need.py と同じ（国の和は州の符号を混ぜる・在庫は書いた時点の断面・TS＝max(1,D)・戦時備蓄の倍率は 1）。
import sys, os
from collections import defaultdict

KEEP, OUT = sys.argv[1], sys.argv[2]
NAMES = ['閉じた側の(S-TS)÷全需要', '閉じた側の(P-D)÷全需要', '閉じた側の需要÷全需要', '閉じた側のうちP>=Dの需要の割合']
with open(OUT, 'w', encoding='utf-8', newline='\n') as fo:
    fo.write('year\tquantity\tmedian\tmin\tmax\n')
    for y0 in ('1870', '1913'):
        res = []
        for s in range(1, 11):
            a = defaultdict(float)
            with open(os.path.join(KEEP, 'nationgood_s%d.tsv' % s), encoding='utf-8') as f:
                H = f.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(H)}
                for ln in f:
                    v = ln.rstrip('\n').split('\t')
                    if v[0] != y0: continue
                    P = float(v[ix['producedQty']]); D = float(v[ix['demandQty']]); S = float(v[ix['stockQty']]); pr = float(v[ix['priceMeanGold']])
                    if not (D > 0) or not (pr > 0): continue
                    TS = max(1.0, D); need = TS + D - P - S
                    a['D'] += D * pr
                    if not (need > 0):
                        a['stockEx'] += (S - TS) * pr; a['flowEx'] += (P - D) * pr; a['Dc'] += D * pr
                        if P >= D: a['PgeD_Dc'] += D * pr
            res.append((a['stockEx'] / a['D'], a['flowEx'] / a['D'], a['Dc'] / a['D'], a['PgeD_Dc'] / a['Dc']))
        for k, n in enumerate(NAMES):
            vs = sorted(r[k] for r in res)
            fo.write('%s\t%s\t%.3f\t%.3f\t%.3f\n' % (y0, n, (vs[4] + vs[5]) / 2, vs[0], vs[-1]))
print('ok', OUT)
