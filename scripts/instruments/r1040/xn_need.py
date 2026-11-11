# (1040) c98xn 設計の材料の台本（読むだけ・エンジンの関数を呼ばない・既にある生だけ）
# 問い1 の補い：国×財の粒度で xNeed（BilateralMarket.cs:1041 と同じ式 TargetStock+LastDemand−LastProduced−Stock）の符号を
#   nationgood_s から近似し、〈xNeed≦0＝案 B の前は行が閉じる〉側の需要の額の割合と、閉じた側の余り（−xNeed）÷需要を出す。
# 使い方: python xn_need.py <keep の base_ins> <出力 tsv>
# ⚠限界（自己申告）
#   ・nationgood_s の producedQty・demandQty は書いた時点の州の LastProduced・LastDemand の和（月の量・InstrumentsNationgood.cs:119）。
#     stockQty は同じ時点の州の Stock の和＝清算の前でなく書いた時点の断面（清算の時点と合わない）。
#   ・TargetStock は TargetStockBuffer 1.0×需要（下限 TargetStockFloor 1.0・戦時備蓄の倍率 StockpileFactor は 1 と置いた）。
#   ・国の和は州の符号を混ぜる（余りの州と不足の州が打ち消す）＝州の行の数でも量でもない。行の頻度は tradeskip の rowNoNeed（op_count）を読む。
#   ・xPrice は求めない（値段の比 rpT/unitA の列が 1870・1913 に無い＝pxgoods_s は 1783〜1794 だけ）。
import sys, os
from collections import defaultdict

KEEP, OUT = sys.argv[1], sys.argv[2]
SEEDS = range(1, 11)
YEARS = ('1870', '1913')
MAJ = {'イギリス', 'フランス', 'プロイセン', 'ロシア', 'アメリカ', '清'}
rows = []
for s in SEEDS:
    acc = defaultdict(lambda: defaultdict(float))
    with open(os.path.join(KEEP, 'nationgood_s%d.tsv' % s), encoding='utf-8') as f:
        H = f.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(H)}
        for ln in f:
            v = ln.rstrip('\n').split('\t')
            y = v[0]
            if y not in YEARS: continue
            P = float(v[ix['producedQty']]); D = float(v[ix['demandQty']]); S = float(v[ix['stockQty']])
            pr = float(v[ix['priceMeanGold']])
            if not (D > 0) or not (pr > 0): continue
            TS = max(1.0, D)
            need = TS + D - P - S
            for sc in (['世界'] + (['6大国'] if v[1] in MAJ else [])):
                a = acc[(y, sc)]
                a['Dv'] += D * pr
                if not (need > 0):
                    a['closedDv'] += D * pr; a['slackV'] += -need * pr; a['closedN'] += 1
                a['N'] += 1
    for (y, sc), a in sorted(acc.items()):
        rows.append((s, y, sc, a['closedDv'] / a['Dv'], a['slackV'] / a['Dv'], a['closedN'] / a['N']))
with open(OUT, 'w', encoding='utf-8', newline='\n') as f:
    f.write('seed\tyear\tscope\tclosedDemandValueShare\tclosedSlackOverDemand\tclosedCellShare\n')
    for r in rows: f.write('%d\t%s\t%s\t%.4f\t%.4f\t%.4f\n' % r)
    for y in YEARS:
        for sc in ('世界', '6大国'):
            L = [r for r in rows if r[1] == y and r[2] == sc]
            for k, name in ((3, 'closedDemandValueShare'), (4, 'closedSlackOverDemand'), (5, 'closedCellShare')):
                vs = sorted(r[k] for r in L)
                m = vs[len(vs) // 2] if len(vs) % 2 else 0.5 * (vs[len(vs) // 2 - 1] + vs[len(vs) // 2])
                f.write('median\t%s\t%s\t%s\t%.4f\t[%.4f-%.4f]\n' % (y, sc, name, m, vs[0], vs[-1]))
print('ok', OUT)
