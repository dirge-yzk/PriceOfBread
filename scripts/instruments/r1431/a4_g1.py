# (1431) G1 の道の分け：腕 c152sdB の facfloor riotHeatRegions を 国×四半期（発火は 1 国 1 四半期に 1 回まで＝Politics.cs:792 の最悪の州）で数え直す
import csv, sys
SD='D:/PoB_work/keep_1425_rej/c152sdB'
G5=set(sys.argv[1].split(','))
reg=0; nq=0; nqall=0; regall=0
for s in range(1,11):
    with open(f'{SD}/facfloor_s{s}.tsv', newline='', encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            h=int(r['riotHeatRegions'])
            regall+=h; nqall+= h>0
            if r['nation'] in G5: reg+=h; nq+= h>0
open('D:/PoB_work/r1431_work/a4_g1.txt','w',encoding='utf-8').write(f'5 大国：州×四半期 {reg}・国×四半期 {nq}\n全ての国：州×四半期 {regall}・国×四半期 {nqall}\n')
