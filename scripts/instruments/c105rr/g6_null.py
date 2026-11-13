# (1097) c105rr 門 G6 のヌル腕の一つ抜き（gaud (682) の型）：M・N1・N2 のうち 1 本を腕と見なし、残り 2 本の中央の最小〜最大の内かを年ごとに数える＝空振りの率。
# 1/3 以上の組み合わせで〈外〉が出るなら G6 は記録へ下げる（README の尾の門の決まり）。gates_rr.py の G6 と同じ読み（scalars_s の tradeToGdpRatio を人口で加重・年の 4 四半期の平均・10 種の中央）。
# 使い方: python -X utf8 -I g6_null.py <M の ins> <N1 の ins> <N2 の ins>
import sys, os, collections, statistics as st
sys.stdout.reconfigure(newline='\n')
YS = [1790, 1820, 1840, 1850, 1860, 1870, 1880, 1900, 1913, 1930, 1949]
def tg_med(d):
    byy = collections.defaultdict(list)
    for s in range(1, 11):
        p = os.path.join(d, f'scalars_s{s}.tsv')
        if not os.path.exists(p): continue
        acc = collections.defaultdict(lambda: [0.0, 0.0])
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {x: i for i, x in enumerate(h)}
            for ln in f:
                c = ln.rstrip('\n').split('\t')
                try: y = int(float(c[ix['year']])); q = int(c[ix['quarter']]); pk = float(c[ix['popK']]); tg = float(c[ix['tradeToGdpRatio']])
                except (ValueError, KeyError, IndexError): continue
                if y not in YS or not (pk > 0) or tg != tg: continue
                acc[(y, q)][0] += pk * tg; acc[(y, q)][1] += pk
        per = collections.defaultdict(list)
        for (y, q), (a, b) in acc.items():
            if b > 0: per[y].append(a / b)
        for y, v in per.items(): byy[y].append(st.mean(v))
    return {y: st.median(v) for y, v in byy.items() if v}
names = ['M', 'N1', 'N2']
m = [tg_med(d) for d in sys.argv[1:4]]
for y in YS: print(f'{y}\t' + '\t'.join(f'{n} {m[i].get(y, float("nan")):.4f}' for i, n in enumerate(names)))
fails = 0; combos = 0
for i in range(3):
    others = [m[j] for j in range(3) if j != i]
    outy = [y for y in YS if all(y in o for o in others) and y in m[i] and not (min(o[y] for o in others) <= m[i][y] <= max(o[y] for o in others))]
    combos += 1
    if outy: fails += 1
    print(f'{names[i]} を腕と見なす：外の年 {outy}')
print(f'空振り（外の年が 1 つでもある組み合わせ）{fails}/{combos}＝{"記録へ下げる" if fails * 3 >= combos else "門のまま"}')
