"""(822) 先に読む（keep_818/base_ins・記録）：植民地格の州×四半期を、持ち主の国の人頭税の段（nation_s の pollTaxEndShare）の帯で分け、稼ぎ＜1e-6 の割合を数える。"""
import csv, os, collections
B = 'D:/PoB_work/keep_818/base_ins'
bins = [0, 0.05, 0.1, 0.15, 0.2, 0.25, 0.3, 1.01]
cnt = collections.Counter(); zero = collections.Counter()
for sd in range(1, 11):
    nat = {}
    with open(os.path.join(B, 'nation_s%d.tsv' % sd), encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for x in r:
            try: nat[(x[ix['nation']], x[ix['quarter']])] = float(x[ix['pollTaxEndShare']])
            except Exception: pass
    with open(os.path.join(B, 'region_s%d.tsv' % sd), encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for x in r:
            if x[ix['tier']] != 'Colonial': continue
            p = float(x[ix['populationK']])
            if p < 1: continue
            pl = nat.get((x[ix['nation']], x[ix['quarter']]))
            if pl is None: continue
            b = next(i for i in range(len(bins) - 1) if bins[i] <= pl < bins[i + 1])
            cnt[b] += 1
            if float(x[ix['earnedPerMonth']]) / p < 1e-6: zero[b] += 1
with open('D:/PoB_work/c65ce_work/pre_poll.tsv', 'w', encoding='utf-8', newline='\n') as o:
    o.write('pollBand\tcolonialRegionQuarters\tzeroEarnRegionQuarters\tzeroShare\n')
    for b in range(len(bins) - 1):
        o.write('%.2f-%.2f\t%d\t%d\t%.5f\n' % (bins[b], bins[b + 1], cnt[b], zero[b], zero[b] / cnt[b] if cnt[b] else float('nan')))
print(open('D:/PoB_work/c65ce_work/pre_poll.tsv', encoding='utf-8').read())
