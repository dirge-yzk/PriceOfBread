# (1335) c140hf 段 0 と紙の台本（読むだけ・エンジンの関数を呼ばない）：region_s から飢えた州（foodFillShare＜0.5 が 4 四半期以上）の
#   FoodFill・Holding・国内の流出・越境の流出を並べ、国内の残る値打ち〈旧＝機会×持ち分〉と〈新＝機会×持ち分×食の充足〉で、
#   同じ国の他の州の機会の最大との差（摩擦 0.15 を引く・距離の項は引かない＝差の上の側の近似）が正の四半期の割合を数える。
#   foodFillShare は四半期の末の生の充足（Region.FoodFill は平滑＝近似）。
#   6 年の人口の減り＝最初に飢えた四半期の人口から 24 四半期後の人口への減りの割合（史実の桁の線の量・錨はアイルランドの大飢饉 1845〜51 約 20〜25%〈推〉）。
#   使い方: python stage0_hf.py <ins（D:/ の形）> <種の起点> <種の数> [州の名=種 ...]（名を渡すとその州の系列を 4 四半期ごとに出す）
import sys, os, statistics as st
ins, s0, ns = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
want = {}
for a in sys.argv[4:]:
    n, s = a.split('='); want.setdefault(int(s), set()).add(n)
FR = 0.15
tot = dict(reg=0, hq=0, hq_out=0, g_old=0, g_new=0, g_newonly=0)
dec6 = []
print('# stage0_hf ins=%s 種=%d..%d' % (ins, s0, s0 + ns - 1))
print('種\t州\t国(最初の飢え)\t飢えの四半期\tff最小\t持ち分(最初)\t持ち分(最後)\t農村(最初)\t農村(最後)\t国内流出の和K\t越境流出の和K\t差>0(旧)\t差>0(新)\t6年の減り')
for seed in range(s0, s0 + ns):
    p = os.path.join(ins, 'region_s%d.tsv' % seed)
    if not os.path.exists(p): print('MISSING', p); continue
    rows = {}   # q -> list
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            v = line.rstrip('\n').split('\t')
            try:
                rows.setdefault(int(v[ix['quarter']]), []).append((v[ix['nationIndex']], v[ix['region']], v[ix['nation']], float(v[ix['populationK']]), float(v[ix['ruralPopulationK']]),
                    float(v[ix['opportunityIndex']]), float(v[ix['holdingFactor']]), float(v[ix['migrationOutKPerQuarter']]), float(v[ix['crossBorderOutKPerQuarter']]), float(v[ix['foodFillShare']]), int(v[ix['year']])))
            except ValueError:
                continue
    per = {}   # region -> list of (q, rec, gold, gnew)
    pops = {}
    for q in sorted(rows):
        byn = {}
        for r in rows[q]: byn.setdefault(r[0], []).append(r)
        for nat, lst in byn.items():
            for r in lst:
                pops.setdefault(r[1], {})[q] = r[3]
                best = max((o[5] for o in lst if o[1] != r[1]), default=float('nan'))
                so, sn = r[5] * r[6], r[5] * r[6] * r[9]
                per.setdefault(r[1], []).append((q, r, best - so - FR > 0, best - sn - FR > 0))
    for name, L in per.items():
        H = [x for x in L if x[1][9] < 0.5]
        if want.get(seed) and name in want[seed]:
            print('## 系列 種 %d %s（4 四半期ごと・飢えの四半期に *）' % (seed, name))
            print('## 年\t国\t人口K\t農村K\t機会\t持ち分\tff\t国内流出K\t越境流出K\t差(旧)\t差(新)')
            for x in L[::4]:
                r = x[1]
                print('## %d\t%s\t%.1f\t%.1f\t%.3f\t%.3f\t%.3f%s\t%.2f\t%.2f\t%d\t%d' % (r[10], r[2], r[3], r[4], r[5], r[6], r[9], '*' if r[9] < 0.5 else '', r[7], r[8], x[2], x[3]))
        if len(H) < 4: continue
        tot['reg'] += 1; tot['hq'] += len(H)
        tot['hq_out'] += sum(1 for x in H if x[1][7] > 0)
        go = sum(x[2] for x in H); gn = sum(x[3] for x in H)
        tot['g_old'] += go; tot['g_new'] += gn; tot['g_newonly'] += sum(1 for x in H if x[3] and not x[2])
        q0 = H[0][0]; p0 = pops[name].get(q0); p6 = pops[name].get(q0 + 24)
        d6 = (p0 - p6) / p0 if p0 and p6 is not None and p0 > 0 else float('nan')
        if d6 == d6: dec6.append(d6)
        print('%d\t%s\t%s\t%d\t%.3f\t%.3f\t%.3f\t%.1f\t%.1f\t%.2f\t%.2f\t%d\t%d\t%s' % (seed, name, H[0][1][2], len(H), min(x[1][9] for x in H), H[0][1][6], H[-1][1][6], H[0][1][4], H[-1][1][4],
              sum(x[1][7] for x in H), sum(x[1][8] for x in H), go, gn, '%.3f' % d6 if d6 == d6 else 'nan'))
print('# 合計 飢えた州=%d 飢えの四半期=%d うち国内流出>0=%d 差>0 旧=%d 新=%d 新だけ=%d' % (tot['reg'], tot['hq'], tot['hq_out'], tot['g_old'], tot['g_new'], tot['g_newonly']))
if dec6: print('# 6年の減り 件=%d 中央=%.4f 最小=%.4f 最大=%.4f（錨 0.20〜0.25・10 倍の線＝中央が 0.0225 未満か 2.25 超で線）' % (len(dec6), st.median(dec6), min(dec6), max(dec6)))
print('STAGE0DONE')
