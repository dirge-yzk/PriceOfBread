# (1097) c105rr 紙の台本：門 G1 が旗 0（何もしない実装＝州の線路 T_r を積まない）の盤面で落ちることの確かめ。
# 使い方: python -X utf8 -I paper_g1_flag0.py <生のフォルダ（keep_1096/base_ins）> > out
# 読む物: trroute_s{1..10}.tsv（10 年ごとの上位の組・陸の倍率＝terrainCost×landSeaRatio を売り手側・買い手側で）・trstat_s1.tsv の Z 行（係数）。
# 旗 0 では T_r＝0（積まない）＝計器の φ＝0 の州ばかり。門 G1 の下側〈T_r＝0 の州の陸の倍率＝12×馬の地形〉は、
#   発見の後の全ての州が 3×鉄道の地形になる今の木では、発見の後の側の数だけ破れる（＝旗 0 で落ちる）。水運の州は母集合の外。
import sys, os, collections
sys.stdout.reconfigure(newline='\n')
d = sys.argv[1]
Z = {}
with open(os.path.join(d, 'trstat_s1.tsv'), encoding='utf-8') as f:
    h = f.readline().rstrip('\n').split('\t'); ix = {x: i for i, x in enumerate(h)}
    for line in f:
        c = line.rstrip('\n').split('\t')
        if c[0] == 'Z': Z[c[ix['name']]] = float(c[ix['value']])
TN = ['Plain', 'WaterMajor', 'WaterMinor', 'Hill', 'Forest', 'Mountain', 'Barren']
horse = [Z['TerrainHorseCost[%s]' % t] for t in TN]; rail = [Z['TerrainRailCost[%s]' % t] for t in TN]
ratio = Z['LandSeaCostRatio']; rcf = Z['RailCostFraction']
tot = collections.Counter()
for s in range(1, 11):
    p = os.path.join(d, f'trroute_s{s}.tsv')
    if not os.path.exists(p): continue
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {x: i for i, x in enumerate(h)}
        for line in f:
            c = line.rstrip('\n').split('\t')
            if not c[ix['sellerTerrainCode']]: continue
            for side in ('Seller', 'Buyer'):
                t = int(c[ix[side.lower() + 'TerrainCode']])
                if t in (1, 2): tot['water_side_excluded'] += 1; continue
                m = float(c[ix['terrainCost' + side]]) * float(c[ix['landSeaRatio' + side]])
                hm = ratio * horse[t]; rm = ratio * rcf * rail[t]
                if abs(m - hm) <= 1e-9 * hm: k = 'horse'
                elif abs(m - rm) <= 1e-9 * rm: k = 'rail'
                else: k = 'other'
                tot[(s, k)] += 1; tot[k] += 1
print('# 紙 G1（旗 0 の盤面）：側（売り手・買い手）ごとの陸の倍率の分類・水運の州は外す')
print('# horse＝12×馬の地形（φ＝0 の正しい値）・rail＝3×鉄道の地形（国の発見の 0/1 の値）・other＝どちらでもない')
for s in range(1, 11):
    print(f's{s}\thorse={tot[(s, "horse")]}\trail={tot[(s, "rail")]}\tother={tot[(s, "other")]}')
print(f'計\thorse={tot["horse"]}\trail={tot["rail"]}\tother={tot["other"]}\t水運で外した側={tot["water_side_excluded"]}')
print('# 旗 0 では計器の T_r＝0＝全ての側が φ＝0 の州。G1 の下側の破れ＝rail の数（＞0 なら旗 0 で落ちる）。')
print(f'G1_lower_violations_flag0={tot["rail"] + tot["other"]}')
