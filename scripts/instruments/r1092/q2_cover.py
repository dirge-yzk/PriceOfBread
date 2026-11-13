# (1092) 問い 1・2：四半期 0 の首都の市場の軍需の在庫が月の必要量の何か月分か・1784 q3 の生産（国の和）÷月の必要量・装備の充足の落ちの時期・L の底。
# 月の必要量＝MilitaryUpkeepScale(0.18/0.2765)×兵K×レート（小銃 1.0・弾薬 0.8・火砲 0.4）＝Simulation.cs:2164 と EconomyStep の want（首都の分け前 1・平時・発見の解禁度 1 と仮定＝1783 年の 3 財は解禁済み）。
# 使い方: python -X utf8 -I q2_cover.py <表のフォルダ> > q2_out.txt
import sys, os, collections, statistics as st
sys.stdout.reconfigure(newline='\n')
d = sys.argv[1]; MUS = 0.18 / 0.2765
G = [('小銃', 'capitalRifleStock', 1.0), ('弾薬', 'capitalAmmunitionStock', 0.8), ('火砲', 'capitalArtilleryStock', 0.4)]
NAT = ['フランス', 'オーストリア', 'プロイセン', 'ロシア', 'イギリス', 'オスマン', '清', '日本', 'アメリカ']
R = collections.defaultdict(lambda: collections.defaultdict(list))
for s in range(1, 11):
    rows = collections.defaultdict(dict)
    with open(os.path.join(d, f'stock_s{s}.tsv'), encoding='utf-8') as fi:
        h = fi.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fi:
            c = line.rstrip('\n').split('\t'); t = c[0]
            if not t.startswith('q'): continue
            q = int(t[1:])
            if q > 60: break
            rows[c[ix['nation']]][q] = c
    prod = collections.defaultdict(float)
    with open(os.path.join(d, f'prodcap_s{s}.tsv'), encoding='utf-8') as fi:
        fi.readline()
        for line in fi:
            c = line.split('\t')
            if c[0] != '1784': break
            if c[4] in ('小銃', '弾薬', '火砲'): prod[(c[3], c[4])] += float(c[11])
    for n in NAT:
        if 0 not in rows[n]: continue
        c0 = rows[n][0]; sol = float(c0[ix['soldierK']])
        for g, col, rate in G:
            want = MUS * sol * rate
            R[n][f'在庫月_{g}'].append(float(c0[ix[col]]) / want)
            R[n][f'生産÷必要_{g}'].append(prod[(n, g)] / want)
            z = next((q for q in range(0, 61) if q in rows[n] and float(rows[n][q][ix[col]]) < 0.01 * want), None)
            R[n][f'在庫0の四半期_{g}'].append(z if z is not None else 99)
        e0 = float(c0[ix['capitalEquipFillShare']]); L0 = float(c0[ix['landPowerIndex']])
        dq = next((q for q in range(1, 61) if q in rows[n] and float(rows[n][q][ix['capitalEquipFillShare']]) < 0.6 * e0), None)
        R[n]['充足が0.6倍を切る四半期'].append(dq if dq is not None else 99)
        Ls = [(float(rows[n][q][ix['landPowerIndex']]), q) for q in range(0, 41) if q in rows[n]]
        mn, mq = min(Ls)
        R[n]['L底÷L0'].append(mn / L0 if L0 > 0 else float('nan')); R[n]['L底の四半期'].append(mq)
        R[n]['L40÷L0'].append(float(rows[n][40][ix['landPowerIndex']]) / L0 if 40 in rows[n] and L0 > 0 else float('nan'))
        R[n]['戦の四半期数0-20'].append(sum(float(rows[n][q][ix['atWarFlag']]) for q in range(0, 21) if q in rows[n]))
keys = ['在庫月_小銃', '在庫月_弾薬', '在庫月_火砲', '生産÷必要_小銃', '生産÷必要_弾薬', '生産÷必要_火砲', '在庫0の四半期_小銃', '在庫0の四半期_弾薬', '在庫0の四半期_火砲', '充足が0.6倍を切る四半期', 'L底÷L0', 'L底の四半期', 'L40÷L0', '戦の四半期数0-20']
print('値＝10 種の中央［最小〜最大］・99＝四半期 60 までに起きない')
print('国\t' + '\t'.join(keys))
for n in NAT:
    out = []
    for k in keys:
        a = [x for x in R[n][k] if x == x]
        out.append(f'{st.median(a):.3g}［{min(a):.3g}〜{max(a):.3g}］' if a else '-')
    print(n + '\t' + '\t'.join(out))
