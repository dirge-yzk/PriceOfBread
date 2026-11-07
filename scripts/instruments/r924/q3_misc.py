# (924) 問い2 (a)(c)・問い3：召集の項の符号・② の発火・英 1920 の札・立憲の区間の項の符号の割合
# 使い方: python -X utf8 -I q3_misc.py <out>
import csv, sys, math, os
csv.field_size_limit(10**8)
ROOT = 'D:/PoB_work/keep_919'
ARMS = [('base', 'base_ins', range(1, 11)), ('n1', 'n1', range(1, 11)), ('n2', 'n2', range(1, 11)), ('a11', 'a11', range(11, 21))]
POL = {0: '絶対', 1: '専制', 2: '幕府', 3: '立憲', 4: '共和', 5: 'ブル独', 6: 'プロ独', 7: '軍政', 8: '無政府'}
def f(x):
    try: return float(x)
    except Exception: return math.nan
out = open(sys.argv[1], 'w', encoding='utf-8', newline='\n')
for arm, sub, seeds in ARMS:
    smin = math.inf; sneg = 0; srows = 0; t2fire = 0; t2fireCm = 0; t2pfire = 0; t2pCm = 0; t1fireCm = 0; cmq = 0
    t2pCmSum = 0.0; t1CmSum = 0.0; sumCmSum = 0.0; ukCm = 0; ukCmDg0 = 0; ukCmDgBelowPc = 0
    y1920 = []
    for s in seeds:
        with open(os.path.join(ROOT, sub, f'crownpc_s{s}.tsv'), encoding='utf-8', newline='') as fh:
            for r in csv.DictReader(fh, delimiter='\t'):
                sm = f(r['summonTermShare'])
                if not math.isnan(sm):
                    srows += 1; smin = min(smin, sm)
                    if sm < -1e-15: sneg += 1
                pol = r['polityCode']
                t2 = f(r['term2Share']); t2p = f(r['term2pShare']); t1 = f(r['term1Share'])
                if not math.isnan(t2) and t2 < 0: t2fire += 1
                if pol == '3':
                    cmq += 1
                    if not math.isnan(t2) and t2 < 0: t2fireCm += 1
                    if not math.isnan(t2p) and t2p < 0: t2pCm += 1; t2pCmSum += t2p
                    if not math.isnan(t1) and t1 > 0: t1fireCm += 1; t1CmSum += t1
                    if not math.isnan(sm): sumCmSum += sm
                    if r['nation'] == 'イギリス':
                        ukCm += 1
                        dg = f(r['debtGripShare']); pc = f(r['pcNowShare'])
                        if not (dg > 1e-9): ukCmDg0 += 1
                        elif dg <= pc: ukCmDgBelowPc += 1
                if r['nation'] == 'イギリス' and r['year'] == '1920' and int(r['quarter']) % 4 == 0:
                    y1920.append(POL.get(int(float(pol)), pol) + f"(王冠{r['crownCode']})")
    out.write(f'{arm}: 召集の項 行{srows} 最小{smin:.3g} 負の行{sneg}｜② が負の行 全{t2fire}・立憲で{t2fireCm}｜立憲の四半期{cmq}（全ての国）: ①>0 {t1fireCm}行 Σ{t1CmSum:.2f}・②′<0 {t2pCm}行 Σ{t2pCmSum:.2f}・召集 Σ{sumCmSum:.2f}'
              f'｜英の立憲{ukCm}: DebtGrip≈0 {ukCmDg0}・0<DebtGrip≦PC {ukCmDgBelowPc}｜英 1920: {" ".join(y1920)}\n')
out.close()
