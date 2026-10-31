#!/usr/bin/env python
"""(710) rd48 ①の続き：PopDecision の駆動（urxpd_s＝本国格の州だけ・国×四半期の和）から往復の型を読む（読むだけ・既存の列だけ）。
使い方：python -X utf8 -I pd_drive.py <計器の出力 ins フォルダ>[:<種の起点>]
出力：
  ①州×月の数（regionMonths）ごとの、四半期の中の打ち消しの割合 W/G＝Σ2·min(pos,neg)÷Σ(pos+neg)。regionMonths＝3 は本国格の州が 1 つだけの四半期
    ＝その W は〈同じ州の同じ対（最初の農村 POP＝農民・最初の都市 POP＝工場労働者）の月どうしの逆向き〉だけ。4 以上は州どうしの逆向きも入る。
  ②本国格の州が 1 つだけの十年（その十年の全ての四半期で regionMonths＝3・36 四半期以上）の分解：W（月どうし）・A（四半期をまたぐ逆向き）・N（純）。
  ③20 年ごとの駆動の大きさ：|z|（国×四半期の重み付きの平均の絶対値＝州の平均で薄まる下限）・都市の 1 人あたりの所得・|所得の差|÷都市の所得・
    四半期の総量÷対の規模（Mobility＝月 0.02 の上限は四半期 0.06）。
限界の申告：urxpd_s は国×四半期の和＝州×月の符号は直接は見えない（①の regionMonths＝3 だけが 1 州の月どうしを切り出せる）。植民地格の州は数えない。
"""
import csv, os, re, sys
from collections import defaultdict
D = sys.argv[1]; s0 = 1
m = re.match(r'^(.*):(\d+)$', D)
if m and not re.match(r'^[A-Za-z]$', m.group(1)): D, s0 = m.group(1), int(m.group(2))
seeds = range(s0, s0 + 10)
byrm = defaultdict(lambda: [0.0, 0.0, 0]); dec = defaultdict(list); tr = defaultdict(lambda: [0.0] * 6)
for s in seeds:
    with open(os.path.join(D, f'urxpd_s{s}.tsv'), encoding='utf-8', newline='') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            rm = int(r['regionMonths']); pos = float(r['sumFlowPos']); neg = float(r['sumFlowNeg']); t = float(r['sumTotal'])
            b = byrm[min(rm, 30) if rm < 30 else 30]; b[0] += pos + neg; b[1] += 2 * min(pos, neg); b[2] += 1
            dec[(s, r['nation'], int(r['year']) // 10 * 10)].append((rm, pos, neg))
            if t > 0 and rm > 0:
                a = tr[int(r['year']) // 20 * 20]
                a[0] += abs(float(r['wZ']) / t); a[1] += float(r['wIncU']) / t; a[2] += abs(float(r['wIncU']) - float(r['wIncR'])) / t; a[3] += 1
                a[4] += pos + neg; a[5] += t / 3   # 対の規模の四半期の値＝州×月の和÷3 か月
print('# ① 州×月の数ごとの四半期の中の打ち消し（30 は 30 以上をまとめた）')
print('regionMonths\tquarters\tgrossK\tW/G')
for k in sorted(byrm):
    g, w, n = byrm[k]
    if g > 0: print(f'{k}\t{n}\t{g:.0f}\t{w/g:.3f}')
G = W = A = N = 0.0; c = 0
for k, v in dec.items():
    if len(v) >= 36 and all(x[0] == 3 for x in v):
        g = sum(p + n for _, p, n in v); w = sum(2 * min(p, n) for _, p, n in v)
        absq = sum(abs(p - n) for _, p, n in v); net = abs(sum(p - n for _, p, n in v))
        G += g; W += w; A += absq - net; N += net; c += 1
print('\n# ② 本国格の州が 1 つだけの十年（種×国×十年 %d 件）' % c)
if G > 0: print(f'gross={G:.0f}K\tW/G={W/G:.3f}\tA/G={A/G:.3f}\tN/G={N/G:.3f}\t戻す割合=(G−N)/G={(G-N)/G:.3f}')
print('\n# ③ 20 年ごとの駆動の大きさ（国×四半期の行の平均）')
print('period\t|z_q|\tincUrban\t|gap|\t|gap|/incUrban\tgrossPerPairPerQ')
for k in sorted(tr):
    a = tr[k]; n = a[3]
    print(f'{k}\t{a[0]/n:.2f}\t{a[1]/n:.2f}\t{a[2]/n:.3f}\t{a[2]/a[1]:.3f}\t{a[4]/a[5]:.4f}')
