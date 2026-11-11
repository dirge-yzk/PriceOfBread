# (1015) c94rh 甜菜糖：国ごとに砂糖の設備の規模のうち甜菜糖の割合（eqr_s shareOfGood・年の q%4==3・種の中央値と最小〜最大）と、
#   その国の州の緯度の帯（Climate.CityCoord は読まない＝world.json の国の latitude だけ・州ごとの座標は Climate.cs の表＝この台本は読まない＝限界）。
# 使い方: python -X utf8 -I rh_beet.py <world.json> <生のフォルダ> <種の範囲> <出力のファイル>
import sys, os, json, statistics as st
WJ, RAW, RNG, OUT = sys.argv[1:5]
lo, hi = map(int, RNG.split('-'))
NINE = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'アメリカ', '清', 'オーストリア', 'オスマン', '日本', 'スペイン']
w = json.load(open(WJ, encoding='utf-8-sig'))
lat = {n['name']: n['latitude'] for n in w['nations']}
V = {}
for s in range(lo, hi + 1):
    p = os.path.join(RAW, f'eqr_s{s}.tsv')
    if not os.path.exists(p): continue
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            r = line.rstrip('\n').split('\t')
            if r[ix['good']] != '砂糖' or r[ix['recipe']] != '甜菜糖' or r[ix['nation']] not in NINE: continue
            y = int(r[ix['year']])
            if y not in (1820, 1850, 1870, 1913) or int(r[ix['quarter']]) % 4 != 3: continue
            V.setdefault((r[ix['nation']], y), []).append(float(r[ix['shareOfGood']] or 'nan'))
o = open(OUT, 'w', encoding='utf-8', newline='\n')
o.write(f'# rh_beet raw={RAW} seeds={RNG}（eqr_s・砂糖の設備の規模のうち甜菜糖の割合・種の中央値[最小,最大]）\nnation\tcapitalLat\t1820\t1850\t1870\t1913\n')
for n in NINE:
    cells = []
    for y in (1820, 1850, 1870, 1913):
        v = [x for x in V.get((n, y), []) if x == x]
        cells.append(f'{st.median(v):.3f}[{min(v):.2f},{max(v):.2f}]n{len(v)}' if v else '-')
    o.write(f'{n}\t{lat.get(n, float("nan")):.1f}\t' + '\t'.join(cells) + '\n')
o.close(); print('ok', OUT)
