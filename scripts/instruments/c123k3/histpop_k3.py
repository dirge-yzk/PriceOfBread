# (1222) c123k3 史実の桁の線 H1（★(1219)＝この線が一番動かす量＝人口）：6 大国と清・インドの 1850 年と 1913 年の人口を史実と比べる。
#   使い方: python -X utf8 -I histpop_k3.py <ins のフォルダ> <種の始め> <種の終わり>
#   読む表：region_s<種>.tsv（州の人口）。列強 6 国＝その年の最後の四半期に持つ州のうち Colonial でない州の和（本国の版）。
#   清＝開幕に清の州だった州の和（持ち主を問わない）。インド＝開幕にインドの 6 国の州だった州＋英仏のインドの植民地 5 州の和（持ち主を問わない）。
#   史実（千人・Maddison 2001 の表の丸め〈推〉・オーストリアはハプスブルクの全域の概数〈推〉・プロイセンはプロイセン王国の領域〈推〉＝ドイツの領域で書くと旗 0 の基線が 5 件赤＝物差しの誤り・腕の前に直した）。
#   線：国と年ごとの比（模擬÷史実）の種の中央が 10 以上か 0.1 以下なら赤（門）。種ごとの赤は記録（国が滅ぶ種＝ヌル腕 N1 でも 2 種＝英 種 2・普 種 7）。
import sys, os, csv
HIST = {'イギリス': (27181, 45649), 'フランス': (36350, 41463), 'プロイセン': (16300, 40165), 'ロシア': (73750, 156192),
        'オーストリア': (31000, 52000), 'アメリカ': (23580, 97606), '清': (412000, 437140), 'インド': (235800, 303700)}
INDIA_N = {'アワド', 'シク連邦', 'デリー', 'ハイデラバード', 'マイソール', 'マラータ同盟'}
INDIA_R = {'中核ベンガル', '内陸ベンガル', 'マドラス', 'ボンベイ', 'ポンディシェリ'}
d, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
red = 0; RAT = {}; print('種\t国\t1850 模擬\t1850 比\t1913 模擬\t1913 比')
for s in range(s0, s1 + 1):
    p = os.path.join(d, f'region_s{s}.tsv')
    if not os.path.exists(p): print(f'{s}\t欠け'); red += 1; continue
    rows = list(csv.DictReader(open(p, encoding='utf-8'), delimiter='\t'))
    q0 = min(int(r['quarter']) for r in rows)
    china = {r['region'] for r in rows if int(r['quarter']) == q0 and r['nation'] == '清'}
    india = {r['region'] for r in rows if int(r['quarter']) == q0 and (r['nation'] in INDIA_N or r['region'] in INDIA_R)}
    lastq = {}
    for r in rows: y = int(r['year']); lastq[y] = max(lastq.get(y, -1), int(r['quarter']))
    val = {}
    for y in (1850, 1913):
        if y not in lastq: continue
        at = [r for r in rows if int(r['quarter']) == lastq[y]]
        for nm in HIST:
            if nm == '清': v = sum(float(r['populationK']) for r in at if r['region'] in china)
            elif nm == 'インド': v = sum(float(r['populationK']) for r in at if r['region'] in india)
            else: v = sum(float(r['populationK']) for r in at if r['nation'] == nm and r['tier'] != 'Colonial')
            val[(nm, y)] = v
    for nm, (h50, h13) in HIST.items():
        a, b = val.get((nm, 1850), float('nan')), val.get((nm, 1913), float('nan'))
        ra, rb = a / h50, b / h13
        bad = [x for x in (ra, rb) if x == x and (x >= 10 or x <= 0.1)]
        red += len(bad); RAT.setdefault((nm, 1850), []).append(ra); RAT.setdefault((nm, 1913), []).append(rb)
        print(f'{s}\t{nm}\t{a:.0f}\t{ra:.2f}\t{b:.0f}\t{rb:.2f}' + ('\t赤' if bad else ''))
import statistics as st
gate = 0
for (nm, y), v in RAT.items():
    v = [x for x in v if x == x]
    if not v: continue
    m = st.median(v); bad = m >= 10 or m <= 0.1; gate += bad
    print(f'中央	{nm}	{y}	{m:.2f}	最小 {min(v):.2f}	最大 {max(v):.2f}' + ('	赤' if bad else ''))
print(f'H1 種ごとの赤（記録）{red}')
print(f'H1 門（種の中央の赤）{gate}（0 なら緑）')
