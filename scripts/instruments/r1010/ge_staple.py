# (1010) c93ge：主食の組（麦 15・野菜 98・米 93・ジャガイモ 105・玉蜀黍 106）と綿花 12 の産地の州の数（開幕・州＋植民地）。案 B で費用が下がるのは産地の州だけ。
# 使い方: python -X utf8 -I ge_staple.py <world.json> <出力のファイル>
import sys, json
WJ, OUT = sys.argv[1:3]
w = json.load(open(WJ, encoding='utf-8-sig'))
G = {15: '麦', 98: '野菜', 93: '米', 105: 'ジャガイモ', 106: 'トウモロコシ', 12: '綿花'}
N = 0; k = {g: 0 for g in G}; nat = {g: set() for g in G}
for n in w['nations']:
    for p in n['provinces'] + n.get('initialColonies', []):
        N += 1
        for g in G:
            if g in p['resources']: k[g] += 1; nat[g].add(n['name'])
with open(OUT, 'w', encoding='utf-8', newline='\n') as o:
    o.write(f'# ge_staple 州＋植民地 N={N}\ngood\tk\tnations\n')
    for g, nm in G.items(): o.write(f'{nm}\t{k[g]}\t{"・".join(sorted(nat[g]))}\n')
print('ok', OUT)
