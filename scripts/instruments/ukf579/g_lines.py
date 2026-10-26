# (579) 線 ukf：狙いの数字の読み手（読むだけ）。事前登録 measurements/ukf579_20261027_prereg.txt の追記 3 の定義のとおり（採否の線＝G6・G7・Gm1・Gm2／記録＝G0・G1・G3・G5・ほか）。
# 使い方：PYTHONUTF8=1 python scripts/instruments/ukf579/g_lines.py <計器の出力フォルダ(…/ins)> [種の数=10] [種の起点=1]（判定の種 11〜20 は 10 11）
# 基線 D:/PoB_work/keep_576/m576_ins に通すと基線の値が出る（aib_s が要る G0・Gm1・Gm2 は基線では〈aib_s なし〉）。
import csv, sys, os, statistics as st

D = sys.argv[1]
NS = int(sys.argv[2]) if len(sys.argv) > 2 else 10
SB = int(sys.argv[3]) if len(sys.argv) > 3 else 1   # 種の起点（計器のファイル名の _s<実の種>）
SEEDS = range(SB, SB + NS)
UK = 'イギリス'
MILADV = {'火砲', '小銃', '弾薬', '帆船', '軍艦', '汽船', '機関車', '電化製品', '送電網', '電力', '機関銃', '戦車', '装甲艦', '弩級戦艦'}   # FactorySpec の組が 軍需・先端・高級軍需・超重軍需・先端軍需 の建てられる財
HOME = ['ロンドン', 'マンチェスター', 'バーミンガム', 'グラスゴー', 'ダブリン']
NINE = ['清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本', 'アメリカ']

def rows(name, s):
    p = os.path.join(D, f'{name}_s{s}.tsv')
    if not os.path.exists(p): return None, None
    fh = open(p, encoding='utf-8-sig'); rd = csv.reader(fh, delimiter='\t'); h = next(rd)
    return {n: i for i, n in enumerate(h)}, rd

def fnum(x):
    try: return float(x)
    except: return float('nan')

def isuk(n): return UK in n

# G1：英の AI が 1783〜1820 に 軍需・先端 の工場を着工した種の数（nation_s の着工した財の名前）／全国の件数も記録
g1 = []; g1_all = []; g1_uk_all_starts = []
for s in SEEDS:
    ix, rd = rows('nation', s)
    if ix is None: continue
    uk = 0; allc = 0; ukall = 0
    for r in rd:
        y = int(r[ix['year']])
        if y > 1820: continue
        gs = [g for g in r[ix['stateFacilityStartedGoods']].split(';') if g]
        a = sum(1 for g in gs if g in MILADV)
        allc += a
        if isuk(r[ix['nation']]): uk += a; ukall += len(gs)
    g1.append(uk); g1_all.append(allc); g1_uk_all_starts.append(ukall)
print(f'記録 G1 英の 軍需・先端 の着工（1783-1820）種ごと: {g1}  → 件数>0 の種 {sum(1 for x in g1 if x>0)}/{len(g1)}（記録・線なし）｜全国の同件数 {g1_all}｜英の全着工 {g1_uk_all_starts}')

# 記録：英の完成数（〜1835）÷着工数（〜1820）の種ごとの値と中央値（nation_s の stateFacilityCompletedCount ÷ stateFacilityStartedCount）。線は無い（基線で既に 1 を超える）
g3 = []
for s in SEEDS:
    ix, rd = rows('nation', s)
    if ix is None: continue
    st_ = 0.0; co = 0.0
    for r in rd:
        if not isuk(r[ix['nation']]): continue
        y = int(r[ix['year']])
        if y <= 1820: st_ += fnum(r[ix['stateFacilityStartedCount']])
        if y <= 1835: co += fnum(r[ix['stateFacilityCompletedCount']])
    g3.append(co / st_ if st_ > 0 else float('nan'))
print(f'記録 英の完成数(〜1835)÷着工数(〜1820) 種ごと: ' + ' '.join(f'{x:.2f}' for x in g3) + f'  中央値 {st.median([x for x in g3 if x==x]):.3f}')

# G5（尾の確認・採否に使わない）：1830 年に英の本国 5 州を全て持つ種の数と、持つ州の数の種ごとの値
g5 = []
for s in SEEDS:
    ix, rd = rows('region', s)
    if ix is None: continue
    by = {}
    for r in rd:
        if r[ix['region']] in HOME and int(r[ix['year']]) == 1830:
            by.setdefault(int(r[ix['quarter']]), {})[r[ix['region']]] = r[ix['nation']]
    if not by: g5.append(None); continue
    d = by[max(by)]
    g5.append(sum(1 for rg in HOME if isuk(d.get(rg, ''))))
print(f'G5 1830 年に英が持つ本国 5 州の数 種ごと: {g5} → 5 州全て {sum(1 for x in g5 if x==5)}/{len(g5)}（記録・尾の確認）')

# G6：9 国の〈国家の支払い率 r＜1〉の月の割合（fiscal_s の payRateShare）の種ごとの値の中央値
cnt = {n: [] for n in NINE}
for s in SEEDS:
    ix, rd = rows('fiscal', s)
    if ix is None: continue
    c = {}
    for r in rd:
        n = r[ix['nation']]
        if n not in cnt: continue
        v = fnum(r[ix['payRateShare']]); a = c.setdefault(n, [0, 0]); a[0] += 1
        if not (v >= 0.9999): a[1] += 1
    for n, (a, b) in c.items(): cnt[n].append(b / a)
worst = 0.0
for n in NINE:
    if cnt[n]:
        med = st.median(cnt[n]); worst = max(worst, med)
        print(f'G6 {n}: r<1 の月の割合 種ごとの中央値 {med:.3f} 最大 {max(cnt[n]):.3f}')
print(f'G6 9 国の中央値の最大 {worst:.3f}（線 0.05 以下 → {"通る" if worst <= 0.05 else "外れ"}）')

# G7：国庫が 0 以下の行の割合（全 61 国の平均を種ごとに・stock_s の treasuryStock）／ジェノヴァ・ハノーファーも記録
g7 = []; gh = []
for s in SEEDS:
    ix, rd = rows('stock', s)
    if ix is None: continue
    c = {}
    for r in rd:
        n = r[ix['nation']]; v = fnum(r[ix['treasuryStock']]); a = c.setdefault(n, [0, 0]); a[0] += 1
        if v <= 0: a[1] += 1
    sh = [b / a for a, b in c.values() if a > 20]
    g7.append(sum(sh) / len(sh)); gh.append((c.get('ジェノヴァ', [1, 0])[1] / c.get('ジェノヴァ', [1, 0])[0], c.get('ハノーファー', [1, 0])[1] / c.get('ハノーファー', [1, 0])[0]))
print(f'G7 国庫≦0 の行の割合（全国平均）種ごと: ' + ' '.join(f'{x:.3f}' for x in g7) + f'  中央値 {st.median(g7):.3f}（線 0.05 以下 → {"通る" if st.median(g7) <= 0.05 else "外れ"}）｜ジェノヴァ・ハノーファー: ' + ' '.join(f'{a:.2f}/{b:.2f}' for a, b in gh))

# 記録：英の艦隊（1792÷1788）・英÷仏の海軍力（1792）
fr = []; nv = []
for s in SEEDS:
    ix, rd = rows('stock', s)
    if ix is None: continue
    t = {}
    for r in rd:
        n = r[ix['nation']]
        if n in (UK, 'フランス') or isuk(n):
            y = int(r[ix['year']])
            if y in (1788, 1792): t.setdefault((('uk' if isuk(n) else 'fr'), y), []).append((fnum(r[ix['fleetUnitsStock']]), fnum(r[ix['navalPowerIndex']])))
    m = lambda k, j: st.mean(x[j] for x in t[k]) if k in t else float('nan')
    fr.append(m(('uk', 1792), 0) / m(('uk', 1788), 0)); nv.append(m(('uk', 1792), 1) / max(1e-9, m(('fr', 1792), 1)))
print('記録 英の艦隊 1792÷1788 種ごと: ' + ' '.join(f'{x:.2f}' for x in fr) + f'  中央値 {st.median(fr):.2f}｜英÷仏の海軍力 1792: ' + ' '.join(f'{x:.2f}' for x in nv) + f'  中央値 {st.median(nv):.2f}')

# G0・G3'・鎖の中間（aib_s がある腕だけ）：英の 門が開いた回の割合（1783-1820）／軍艦・帆船・火砲・小銃 の BuiltScale の和（完成した棟の累計）の 1783 年→1830 年の増え
open_share = []; chain = []
for s in SEEDS:
    ix, rd = rows('aib', s)
    if ix is None: break
    calls = rf = rr = 0; sc0 = None; sc = {}
    for r in rd:
        if not isuk(r[ix['nation']]): continue
        y = int(r[ix['year']])
        if y <= 1820:
            calls += int(r[ix['calls']]); rf += int(r[ix['returnFundingCount']]); rr += int(r[ix['returnRoomCount']])
        tot = sum(fnum(r[ix[c]]) for c in ('warshipBuiltSum', 'sailingShipBuiltSum', 'artilleryBuiltSum', 'rifleBuiltSum'))
        if int(r[ix['quarter']]) == 0 and sc0 is None: sc0 = tot
        sc[y] = tot
    open_share.append((calls - rf - rr) / calls if calls else float('nan'))
    chain.append((sc0, sc.get(1795), sc.get(1830)))
if open_share:
    print('記録 G0 英の 門が開いた回の割合（1783-1820・calls−r<1−余地なし÷calls）種ごと: ' + ' '.join(f'{x:.2f}' for x in open_share) + f'  中央値 {st.median(open_share):.2f}（記録・この形は月の出費の上限を変えないので線に使わない）')
    print('記録 英の 軍艦+帆船+火砲+小銃 の BuiltScale の和（開幕→1795→1830）: ' + ' | '.join(f'{a:.2f}→{b:.2f}→{c:.2f}' for a, b, c in chain))
    g1set = [i for i, x in enumerate(g1) if x > 0]
    ok = [i for i in g1set if i < len(chain) and chain[i][2] is not None and chain[i][0] is not None and chain[i][2] - chain[i][0] >= 0.5]
    print(f"記録 G1 を通った種のうち BuiltScale の和が 1783→1830 に 0.5 以上増えた種 {len(ok)}/{len(g1set)}（州の持ち替えでも増減する量＝記録だけ）")
else:
    print('G0・鎖の中間: aib_s なし（基線または旗 0 の計器なしの腕）')

# 記録：英の着工（〜1830・全種の合計）に占める 防水布 の割合（nation_s）／aib_s がある腕は 英の〈州の最良〉の上位（startedGoods＋skippedGoods）
tot = {}; n_all = 0
for s in SEEDS:
    ix, rd = rows('nation', s)
    if ix is None: continue
    for r in rd:
        if isuk(r[ix['nation']]) and int(r[ix['year']]) <= 1830:
            for g in r[ix['stateFacilityStartedGoods']].split(';'):
                if g: tot[g] = tot.get(g, 0) + 1; n_all += 1
if n_all:
    top = sorted(tot.items(), key=lambda kv: -kv[1])[:6]
    print(f"記録 英の着工（〜1830・種の合計 {n_all} 件）の上位: " + ' '.join(f'{k}:{v}' for k, v in top) + f"｜防水布の割合 {tot.get('防水布', 0) / n_all:.2f}（基線 0.72）")
best = {}
for s in SEEDS:
    ix, rd = rows('aib', s)
    if ix is None: break
    for r in rd:
        if isuk(r[ix['nation']]) and int(r[ix['year']]) <= 1830:
            for col in ('startedGoods', 'skippedGoods'):
                for kv in r[ix[col]].split(';'):
                    if kv:
                        k, v = kv.rsplit(':', 1); best[k] = best.get(k, 0) + int(v)
if best:
    tb = sorted(best.items(), key=lambda kv: -kv[1])[:6]; sb = sum(best.values())
    print(f"記録 英の〈州の最良〉の上位（着工＋余地が足りず飛ばした・〜1830・種の合計 {sb} 件）: " + ' '.join(f'{k}:{v}' for k, v in tb) + f"｜軍艦・帆船・火砲・小銃の合計 {sum(best.get(k, 0) for k in ('軍艦', '帆船', '火砲', '小銃'))}")


# Gm1・Gm2（aib_s がある腕だけ）：外した天井（総費用の身の丈の門）が実際に縛っていたか／着工に変わったか。全国・全期間の和を種ごとに。
m1 = []; m2 = []; heavy = []
for s in SEEDS:
    ix, rd = rows('aib', s)
    if ix is None: break
    oc = so = 0; cat = {}
    for r in rd:
        oc += int(r[ix['overCostCapCandidateCount']]); so += int(r[ix['startedOverCapCount']])
        for kv in r[ix['startedByCategory']].split(';'):
            if kv:
                k, v = kv.rsplit(':', 1); cat[k] = cat.get(k, 0) + int(v)
    m1.append(oc); m2.append(so); heavy.append(cat)
if m1:
    print(f'Gm1 旧の身の丈の門なら落ちた候補（全国・全期間）の種ごとの数: {m1} → >0 の種 {sum(1 for x in m1 if x>0)}/{len(m1)}（線 6/10 以上 → {"通る" if sum(1 for x in m1 if x>0) >= 0.6*len(m1) else "外れ"}）')
    print(f'Gm2 総費用が旧の身の丈の門を超える着工（全国・全期間）の種ごとの数: {m2} → >0 の種 {sum(1 for x in m2 if x>0)}/{len(m2)}（線 6/10 以上 → {"通る" if sum(1 for x in m2 if x>0) >= 0.6*len(m2) else "外れ"}）')
    tot = {}
    for c in heavy:
        for k, v in c.items(): tot[k] = tot.get(k, 0) + v
    print('記録 全国・全期間の着工の組ごとの件数（種の合計）: ' + ' '.join(f'{k}:{v}' for k, v in sorted(tot.items(), key=lambda kv: -kv[1])))
