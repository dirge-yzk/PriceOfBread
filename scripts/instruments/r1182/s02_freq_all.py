# 読むだけ (1182) c117rs：S02-H004a（戦時の買いだめ）の満点の張り付きの率を、母集合を名乗って数え直す（r1169/s02_freq.py の S02-10 の節の直し）。
#  r1169 の版の〈年末〉は labor_s の year の列ごとの最後の四半期＝q%4==2（year の列は q%4==3 から次の年を名乗る）＝計器の国だけ（q%4==2 に行があるのは計器の国）。
#  ここでは nation_s（全ての国の毎四半期・totalPopulationK＞0）を枠にして 5 つの母集合を並べる：
#   F1 全ての国×q%4==3（年末の全ての国）・F2 計器の国×q%4==3・F3 計器の国×q%4==2（旧の版の母集合）・F4 計器の国×毎四半期・F5 全ての国×毎四半期。
#  満点＝国の最大の緊張（全ての組の最大・Simulation.cs の warScareTensionNat）が RivalTensionThreshold 40＋StockpileTensionSpan 60＝100 以上（Model.cs:675〜676）。
#  warpair_s は月の最大が 100 以上か出来事のある組だけ・列 monthsGt100 は 100 を超えた月（≧100 ではなく ＞100）＝国の月は
#   下限＝組の最大（旧の版と同じ）・上限＝min(3, 組の和)。ちょうど 100 の月は数えない（下限の側）。
#  旧の版の再現：--old で r1169 と同じ枠（labor_s の year の最後の四半期）も印字する。
# 使い方: python -X utf8 -I s02_freq_all.py <生のフォルダ> <名> [種の数=10]
import sys, os, csv, collections
B, TAG = sys.argv[1], sys.argv[2]
NS = int(sys.argv[3]) if len(sys.argv) > 3 else 10
def rd(t, s):
    p = os.path.join(B, f'{t}_s{s}.tsv')
    if not os.path.exists(p): return None
    with open(p, encoding='utf-8', newline='') as f: return list(csv.DictReader(f, delimiter='\t'))
def era(y): return '1783-1849' if y < 1850 else '1850-1913' if y < 1914 else '1914-1950'
FR = ['F1 全ての国×q%4==3', 'F2 計器の国×q%4==3', 'F3 計器の国×q%4==2', 'F4 計器の国×毎四半期', 'F5 全ての国×毎四半期', 'F0 旧の版の枠（labor の year の最後の四半期）']
tot = collections.Counter(); lo = collections.Counter(); hi = collections.Counter(); full3 = collections.Counter(); nq = collections.Counter()
etot = collections.defaultdict(collections.Counter); elo = collections.defaultdict(collections.Counter)
have = 0
for s in range(1, NS + 1):
    nat, wp, lab = rd('nation', s), rd('warpair', s), rd('labor', s)
    if nat is None or wp is None or lab is None: continue
    have += 1
    mx = collections.defaultdict(int); sm = collections.defaultdict(int)
    for r in wp:
        q = int(r['quarter']); m = int(float(r['monthsGt100']))
        for n in (r['nationA'], r['nationB']):
            k = (q, n)
            if m > mx[k]: mx[k] = m
            sm[k] += m
    pk = {r['nation'] for r in lab if int(r['quarter']) % 4 != 3}
    qy = {}
    for r in lab: qy[int(r['quarter'])] = int(r['year'])
    lastq = {}
    for q, y in qy.items(): lastq[y] = max(lastq.get(y, -1), q)
    endq = set(lastq.values())
    labkeys = {(int(r['quarter']), r['nation']) for r in lab if int(r['quarter']) in endq}
    for r in nat:
        try: alive = float(r['totalPopulationK']) > 0
        except Exception: alive = False
        if not alive: continue
        q = int(r['quarter']); n = r['nation']; y = int(r['year'])
        k = (q, n); a = mx.get(k, 0); b = min(3, sm.get(k, 0))
        fr = []
        if q % 4 == 3: fr.append(0)
        if n in pk and q % 4 == 3: fr.append(1)
        if n in pk and q % 4 == 2: fr.append(2)
        if n in pk: fr.append(3)
        fr.append(4)
        if k in labkeys: fr.append(5)
        for i in fr:
            tot[i] += 3; lo[i] += a; hi[i] += b; nq[i] += 1; full3[i] += (a == 3)
            etot[i][era(y)] += 3; elo[i][era(y)] += a
print(f'# {TAG} 生={B} 種 {have}/{NS}')
for i, nm in enumerate(FR):
    if not tot[i]: print(nm, '行なし'); continue
    print(f'{nm}：国×四半期 {nq[i]}・満点の国×月 下限 {lo[i]}/{tot[i]} = {lo[i] / tot[i]:.2%}・上限 {hi[i] / tot[i]:.2%}・3 か月とも {full3[i] / nq[i]:.2%}・時代別（下限） ' +
          ' '.join(f'{e}:{elo[i][e] / etot[i][e]:.1%}' for e in sorted(etot[i])))
