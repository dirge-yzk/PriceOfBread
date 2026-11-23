# (1332) c139nc 紙の台本：今の盤面の生（region_s・nation_s・capcol_s）で、格の順の門の旧の規則（旗 0＝本国格が有れば本国格だけ・Core と Integrated を区別しない）の値を数える。
#   首都＝region_s の国×四半期の最初の行（World.Regions[0]）・格＝region_s の tier・内戦＝nation_s の civilWarMonths>0。エンジンの関数を呼ばない。
#   使い方: python -X utf8 -I paper_nc.py <生のフォルダ> <種の最初> <種の数>
#   列：capIntCore＝首都が Integrated で同じ国に Core の州が有る国×四半期（内戦でない／内戦中）・capColHome＝首都が植民地格で本国格が有る（旧 G1）・
#       pickIntCore＝capcol_s の付け替え（picksCount>0）の四半期で、lastActual が Integrated かつ同じ四半期の末に Core が有る回数（新の規則なら Core を選ぶ見込み）・
#       maxSovColCap＝首都が植民地格の主権国〔内戦でない〕の数の四半期ごとの最大（史実の桁の量）・ukCaps＝英の首都の州の名（10 年ごとの最初の四半期で変わった所）
import sys, csv, os, collections
raw, s0, n = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
RANK = {'Core': 0, 'Integrated': 1, 'Colonial': 2}
def rd(p):
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r: yield ix, row
tot = collections.Counter(); maxes = []
print('seed\tcapIntCore_peace\tcapIntCore_civil\tcapColHome_peace\tcapColHome_civil\tpicks\tpickIntCore\tpickIntCore_civil\tmaxSovColCap\tnations(capIntCore)\tukCaps')
for s in range(s0, s0 + n):
    rp = os.path.join(raw, f'region_s{s}.tsv'); np_ = os.path.join(raw, f'nation_s{s}.tsv'); cp = os.path.join(raw, f'capcol_s{s}.tsv')
    if not os.path.exists(rp): print(f'{s}\t(region_s が無い)'); continue
    civ = {}
    for ix, row in rd(np_):
        civ[(int(row[ix['quarter']]), int(row[ix['nationIndex']]))] = float(row[ix['civilWarMonths']] or 0) > 0
    cap = {}; best = {}; tierOf = {}; names = {}; capName = {}; year = {}
    for ix, row in rd(rp):
        k = (int(row[ix['quarter']]), int(row[ix['nationIndex']])); t = row[ix['tier']]
        if k not in cap: cap[k] = t; names[k] = row[ix['nation']]; capName[k] = row[ix['region']]; year[k] = row[ix['year']]
        best[k] = min(best.get(k, 9), RANK[t]); tierOf[(k, row[ix['region']])] = t
    c = collections.Counter(); perq = collections.Counter(); nat = collections.Counter()
    for k, t in cap.items():
        cv = civ.get(k, False); w = 'civil' if cv else 'peace'
        if t == 'Integrated' and best[k] == 0: c['ic_' + w] += 1; nat[names[k]] += 1
        if t == 'Colonial' and best[k] <= 1: c['ch_' + w] += 1
        if t == 'Colonial' and not cv: perq[k[0]] += 1
    if os.path.exists(cp):
        for ix, row in rd(cp):
            if int(row[ix['picksCount']]) <= 0: continue
            k = (int(row[ix['quarter']]), int(row[ix['nationIndex']])); c['picks'] += 1
            ta = tierOf.get((k, row[ix['lastActual']]))
            if ta == 'Integrated' and best.get(k, 9) == 0:
                c['pic'] += 1
                if row[ix['civilWarFlag']] == '1': c['pic_civil'] += 1
    uk = []; prev = None
    for k in sorted(cap):
        if names[k] != 'イギリス': continue
        if capName[k] != prev: uk.append(f"{year[k][:4]}:{capName[k]}({cap[k][:3]})"); prev = capName[k]
    mx = max(perq.values()) if perq else 0; maxes.append(mx)
    for kk in c: tot[kk] += c[kk]
    print(f"{s}\t{c['ic_peace']}\t{c['ic_civil']}\t{c['ch_peace']}\t{c['ch_civil']}\t{c['picks']}\t{c['pic']}\t{c['pic_civil']}\t{mx}\t" + ','.join(f'{a}:{b}' for a, b in nat.most_common(4)) + '\t' + '→'.join(uk[:8]))
maxes.sort()
print(f"計\tcapIntCore 内戦でない={tot['ic_peace']}・内戦中={tot['ic_civil']}\tcapColHome 内戦でない={tot['ch_peace']}・内戦中={tot['ch_civil']}\tpicks={tot['picks']}\tpickIntCore={tot['pic']}（内戦中 {tot['pic_civil']}）\tmaxSovColCap 種の中央={maxes[len(maxes)//2] if maxes else 0}・最大={max(maxes) if maxes else 0}")
