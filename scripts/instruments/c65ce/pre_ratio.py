"""(822) 先に読む（keep_818/base_ins・記録・門ではない）：植民地格の州×四半期の〈人頭税の 1 千人あたりの請求の近似〉÷〈1 千人あたりの月の稼ぎ〉の分布。
近似＝PollTaxRate 0.15×持ち主の国の pollTaxEndShare（平民の割合を 1 と置く＝上限の見積もり）。凍結の行（稼ぎ＜1e-6/千人）は除き、凍結の入りの 8 四半期前の行と、それ以外の行で分位を並べる。"""
import csv, os, bisect
B = 'D:/PoB_work/keep_818/base_ins'
allr, pre = [], []
onsets = {(4, 'カナダ'): 205, (4, 'セントマイケル'): 239, (10, 'イキトス'): 177, (10, 'チロエ'): 176, (10, 'ロレート'): 182, (10, 'カルメン・デ・パタゴネス'): 184,
          (10, 'エルパソ'): 213, (10, 'パラワン'): 215, (10, 'ボゴタ'): 266, (10, 'キト'): 263, (10, 'ラパス'): 267, (10, 'ヌエバ・エスパーニャ銀地帯'): 218}
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
            p = float(x[ix['populationK']]); e = float(x[ix['earnedPerMonth']])
            if p < 1 or e / p < 1e-6: continue
            pl = nat.get((x[ix['nation']], x[ix['quarter']]))
            if pl is None or pl <= 0: continue
            ratio = 0.15 * pl / (e / p)
            q = int(x[ix['quarter']]); key = (sd, x[ix['region']])
            if key in onsets and onsets[key] - 8 <= q < onsets[key]: pre.append(ratio)
            else: allr.append(ratio)
def qs(v):
    v = sorted(v)
    return ' '.join('p%d=%.3g' % (k, v[int(k / 100 * (len(v) - 1))]) for k in (10, 25, 50, 75, 90, 99)) if v else '-'
with open('D:/PoB_work/c65ce_work/pre_ratio.txt', 'w', encoding='utf-8', newline='\n') as o:
    o.write('凍結の入りの 8 四半期前（凍結の行を除く）：n=%d %s\n' % (len(pre), qs(pre)))
    o.write('それ以外の植民地格（人頭税の段が正・凍結の行を除く）：n=%d %s\n' % (len(allr), qs(allr)))
    o.write('比が 1 を超える割合：入りの前 %.3f・それ以外 %.3f\n' % (sum(1 for v in pre if v > 1) / max(1, len(pre)), sum(1 for v in allr if v > 1) / max(1, len(allr))))
print(open('D:/PoB_work/c65ce_work/pre_ratio.txt', encoding='utf-8').read())
