# (1304) c135nc 紙の台本：今の盤面の生（region_s・nation_s）で、門 G1 の旧の規則の値（旗 0 で門が落ちることの確かめ）と史実の桁の線の量を数える。
#   首都＝region_s の国×四半期の最初の行（World.Regions[0]）・本国格＝tier が Colonial でない州・内戦＝nation_s の civilWarMonths>0（建国の四半期の見かけを含む＝計器 capcol_s は四半期の末の InCivilWar）。
#   使い方: python -X utf8 -I paper_nc.py <生のフォルダ> <種の最初> <種の数>
#   出力：種ごとの G1（内戦でなく本国格 1 以上で首都が植民地格の国×四半期）・本国格 0 で首都が植民地格（内戦でない）・内戦中で首都が植民地格・
#        史実の桁の量（首都が植民地格の主権国〔内戦でない〕の数の四半期ごとの最大）・英の植民地の地代の送金÷名目 GDP（記録・landshare_s の colonialRemitPerQuarter×4÷nominalGdpPerYear・1913 年）。
import sys, csv, os, collections
raw, s0, n = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
def rd(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r: yield ix, row
tot = collections.Counter(); maxes = []
print('seed\tG1\tcapColHome0\tcapColCivil\tmaxSovColCap\tukRemitToGdp1913\tG1nations')
for s in range(s0, s0 + n):
    rp = os.path.join(raw, f'region_s{s}.tsv'); np_ = os.path.join(raw, f'nation_s{s}.tsv'); lp = os.path.join(raw, f'landshare_s{s}.tsv')
    if not os.path.exists(rp): print(f'{s}\t(region_s が無い)'); continue
    civ = {}; gdp = {}
    for ix, row in rd(np_):
        k = (int(row[ix['quarter']]), int(row[ix['nationIndex']]))
        civ[k] = float(row[ix['civilWarMonths']] or 0) > 0
        if row[ix['nation']] == 'イギリス': gdp[int(row[ix['quarter']])] = (row[ix['year']], float(row[ix['nominalGdpPerYear']] or 'nan'))
    cap = {}; home = collections.Counter(); names = {}
    for ix, row in rd(rp):
        k = (int(row[ix['quarter']]), int(row[ix['nationIndex']]))
        if k not in cap: cap[k] = row[ix['tier']]; names[k] = row[ix['nation']]
        if row[ix['tier']] != 'Colonial': home[k] += 1
    g1 = h0 = cv = 0; perq = collections.Counter(); g1n = collections.Counter()
    for k, t in cap.items():
        if t != 'Colonial': continue
        c = civ.get(k, False)
        if c: cv += 1; continue
        perq[k[0]] += 1
        if home[k] >= 1: g1 += 1; g1n[names[k]] += 1
        else: h0 += 1
    mx = max(perq.values()) if perq else 0
    rem = 'nan'
    if os.path.exists(lp):
        acc = 0.0; g = 0.0
        for ix, row in rd(lp):
            if row[ix['nation']] != 'イギリス': continue
            q = int(row[ix['quarter']])
            if q in gdp and gdp[q][0].startswith('1913'): acc += float(row[ix['colonialRemitPerQuarter']] or 0); g += gdp[q][1] / 4
        rem = f'{acc / g:.6f}' if g > 0 else 'nan'
    tot['G1'] += g1; tot['h0'] += h0; tot['cv'] += cv; maxes.append(mx)
    print(f'{s}\t{g1}\t{h0}\t{cv}\t{mx}\t{rem}\t' + ','.join(f'{a}:{b}' for a, b in g1n.most_common(4)))
maxes.sort()
med = maxes[len(maxes) // 2] if maxes else 0
print(f'計\tG1={tot["G1"]}\tcapColHome0={tot["h0"]}\tcapColCivil={tot["cv"]}\tmaxSovColCap の種の中央={med}・最大={max(maxes) if maxes else 0}')
