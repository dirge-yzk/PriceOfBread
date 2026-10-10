"""(193) 徴兵法の判断の読み手（読むだけ）。python an_cnl193.py <cnl の出力フォルダ> [種の上限]
K 行（inj_cnl193）から：
 ① 平時の段の変化（施行の前後）＝上げ・下げ・戦後の失効を国ごと（種あたり）。上げの時に読んだ不安の割合・その値を計算した時に交戦中か・戦後の四半期数・最大の寄与の国。
 ② 戦後の上げ直し＝戦後の失効から何四半期で上げたか（分布）。
 ③ 下げる一歩の門＝平時・段≧選抜・割合が線の下（1/3・0.4・0.5）の国×四半期で、門が通る割合と代価の中央・政治資本の残りの中央。
 ④ 平時に皆兵の国×四半期の割合の分布（国ごと・10年の窓の中央）。
 ⑤ 西＝初めて皆兵になった四半期の前後（本国の州・自国の養える軍・陸軍の不安・最大の寄与・割合）を開幕と比べる。
 ⑥ 露＝初めて皆兵になった四半期・農奴解放が全面になった四半期・その時の割合。"""
import sys, os, glob, statistics as st
from collections import defaultdict
D = sys.argv[1]; NMAX = int(sys.argv[2]) if len(sys.argv) > 2 else 99
MAJ = ['イギリス', 'アメリカ', 'フランス', 'プロイセン', 'ロシア', '日本', '清', 'オーストリア', 'スペイン', 'オスマン']
COL = None
rows = defaultdict(list)   # (seed, nation) -> list of dict
seeds = []
for f in sorted(glob.glob(os.path.join(D, 'c*.txt')), key=lambda p: int(os.path.basename(p)[1:-4])):
    sd = int(os.path.basename(f)[1:-4])
    if sd > NMAX: continue
    seeds.append(sd)
    for ln in open(f, encoding='utf-8'):
        if ln.startswith('#'):
            COL = ln[1:].rstrip('\n').split('\t'); continue
        v = ln.rstrip('\n').split('\t')
        d = dict(zip(COL, v))
        rows[(sd, d['nation'])].append(d)
NS = len(seeds)
def fl(x):
    try: return float(x)
    except: return float('nan')
def I(x):
    try: return int(x)
    except: return -9
print(f'######## 種 {NS}（{seeds}）')
# ① 平時の変化
cnt = defaultdict(lambda: defaultdict(float)); ups = defaultdict(list)
for (sd, n), L in rows.items():
    for d in L:
        a, l0, l1, p0 = I(d['atWar']), I(d['law0']), I(d['law1']), I(d['pre0'])
        if l1 == l0: continue
        key = n if n in MAJ else '_他'
        if a == 1:
            cnt[key]['戦時に上' if l1 > l0 else '戦時に下'] += 1
        elif p0 >= 0 and l1 < l0 and I(d['pre1']) < 0 and l1 == p0:
            cnt[key]['戦後の失効'] += 1
        elif l1 > l0:
            cnt[key]['平時に上'] += 1; ups[key].append(d)
        else:
            cnt[key]['平時に下'] += 1
print('\n## ① 段の変化（種あたり）＝戦時に上｜戦後の失効｜平時に上｜平時に下｜戦時に下')
tot = defaultdict(float)
for n in MAJ + ['_他']:
    c = cnt[n]
    for k, v in c.items(): tot[k] += v
    print(f'  {n}: ' + '・'.join(f'{k} {c[k] / NS:.1f}' for k in ['戦時に上', '戦後の失効', '平時に上', '平時に下', '戦時に下'] if c[k]))
print('  全ての国: ' + '・'.join(f'{k} {tot[k] / NS:.1f}' for k in ['戦時に上', '戦後の失効', '平時に上', '平時に下', '戦時に下']))
print('\n## ① の続き＝平時に上げた一歩（主要10か国）：読んだ割合の中央｜その値を交戦中に計算した割合｜戦後の四半期数（中央・1〜4 の割合）｜最大の寄与の国（上位3）｜その国と計算の時に交戦中の割合')
for n in MAJ:
    U = ups[n]
    if not U: continue
    sh = [fl(d['share']) for d in U]; taw = sum(I(d['thrAtWar']) for d in U); since = [I(d['since']) for d in U]
    s14 = sum(1 for s in since if 1 <= s <= 4); tw = sum(I(d['topAtWar']) for d in U)
    tops = defaultdict(int)
    for d in U: tops[d['top']] += 1
    tt = sorted(tops.items(), key=lambda x: -x[1])[:3]
    sv = [s for s in since if s >= 0]
    print(f'  {n}: 件 {len(U)}｜割合 中央 {st.median(sh):.3f}（最小 {min(sh):.3f}）｜交戦中に計算 {taw}/{len(U)}｜戦後 中央 {st.median(sv) if sv else "-"}・1〜4 {s14}/{len(U)}｜寄与 ' + '・'.join(f'{k} {v}' for k, v in tt) + f'｜その国と交戦中 {tw}/{len(U)}')
# ② 戦後の上げ直し
print('\n## ② 平時の上げ（全ての国）の戦後の四半期数の分布（-1＝一度も交戦していない）')
hist = defaultdict(int)
for n, U in ups.items():
    for d in U:
        s = I(d['since']); b = s if s <= 8 else (12 if s <= 12 else (20 if s <= 20 else (40 if s <= 40 else 999)))
        hist[b] += 1
print('  ' + '・'.join(f'{k}:{v}' for k, v in sorted(hist.items())))
# ③ 下げる一歩の門
print('\n## ③ 下げる一歩の門（平時・段≧選抜）＝割合が線の下の国×四半期で門が通る割合｜代価の中央｜政治資本の残りの中央（種をまとめる）')
for line in (1 / 3, 0.4, 0.5):
    print(f'#### 線 {line:.3f}')
    for n in MAJ:
        ok = no = 0; costs = []; caps = []
        for sd in seeds:
            for d in rows.get((sd, n), []):
                if I(d['atWar']) == 1 or I(d['law0']) < 1: continue
                if not (fl(d['share']) < line): continue
                if d['dnOk'] == '-': continue
                if I(d['dnOk']) == 1: ok += 1
                else: no += 1
                costs.append(fl(d['dnCost'])); caps.append(fl(d['capLeft']))
        if ok + no:
            print(f'  {n}: {ok}/{ok + no}（{100 * ok / (ok + no):.0f}%）｜代価 {st.median(costs):.3f}｜残り {st.median(caps):.3f}')
# ④ 平時に皆兵の国×四半期の割合の分布
print('\n## ④ 平時・皆兵の国×四半期の陸軍の不安の割合（10年の窓・中央｜1/3 未満の割合%）')
W0 = list(range(1784, 1950, 10))
print('         ' + ''.join(f'{y:>12}' for y in W0))
for n in MAJ:
    out = []
    for y0 in W0:
        xs = [fl(d['share']) for sd in seeds for d in rows.get((sd, n), []) if I(d['atWar']) == 0 and I(d['law0']) == 2 and y0 <= I(d['yr']) < y0 + 10]
        out.append(f'{st.median(xs):5.2f}|{100 * sum(1 for x in xs if x < 1 / 3) / len(xs):3.0f}%' if xs else '       -    ')
    print(f'  {n}: ' + ' '.join(f'{o:>11}' for o in out))
# ⑤ 西
def first_univ(L):
    for i, d in enumerate(L):
        if I(d['atWar']) == 0 and I(d['law1']) == 2 and I(d['law0']) < 2: return i
    return None
for nm in ('スペイン', 'ロシア', 'フランス', 'オーストリア'):
    print(f'\n## ⑤ {nm}＝初めて平時に皆兵へ上げた四半期（種ごと）：年｜本国の州（開幕→その時）｜自国の養える軍（開幕→その時）｜陸軍の不安（開幕→その時）｜割合｜最大の寄与（その国と交戦中・敵意）｜農奴解放（全面になった年）｜戦後の四半期数')
    for sd in seeds:
        L = rows.get((sd, nm), [])
        if not L: continue
        i = first_univ(L)
        full = next((I(d['yr']) for d in L if I(d['serf']) == 2), None)
        if i is None:
            print(f'  種{sd}: —（農奴解放 全面 {full}）'); continue
        d0, d = L[0], L[i]
        print(f'  種{sd}: {d["yr"]}｜州 {d0["core"]}+{d0["integ"]}→{d["core"]}+{d["integ"]}｜軍 {fl(d0["own"]):.0f}→{fl(d["own"]):.0f}｜不安 {fl(d0["thrArmy"]):.0f}→{fl(d["thrArmy"]):.0f}｜割合 {fl(d["share"]):.3f}｜寄与 {d["top"]} {fl(d["topVal"]):.0f}（交戦 {d["topAtWar"]}・敵意 {fl(d["topHost"]):.2f}）｜全面 {full}｜戦後 {d["since"]}')
# ⑥ 西の割合の推移（10年の窓の中央）＝分子と分母
print('\n## ⑥ 西と露と仏と墺の陸軍の不安の分子（不安）と分母（自国の養える軍）と本国の州の数（平時・10年の窓・種の中央）')
for nm in ('スペイン', 'ロシア', 'フランス', 'オーストリア', 'プロイセン'):
    out = []
    for y0 in W0:
        xs = [(fl(d['thrArmy']), fl(d['own']), I(d['core']) + I(d['integ'])) for sd in seeds for d in rows.get((sd, nm), []) if I(d['atWar']) == 0 and y0 <= I(d['yr']) < y0 + 10]
        if xs: out.append(f'{st.median(x[0] for x in xs):.0f}/{st.median(x[1] for x in xs):.0f}/{st.median(x[2] for x in xs):.0f}')
        else: out.append('-')
    print(f'  {nm}: ' + ' '.join(f'{o:>12}' for o in out))
# ⑦ 寄与の国（平時・皆兵・主要国）
print('\n## ⑦ 平時の陸軍の不安の最大の寄与の国（国×四半期の数の上位・割合≧0.5 の時）')
for nm in MAJ:
    tops = defaultdict(int); n0 = 0
    for sd in seeds:
        for d in rows.get((sd, nm), []):
            if I(d['atWar']) == 0 and fl(d['share']) >= 0.5: tops[d['top']] += 1; n0 += 1
    if n0: print(f'  {nm}（{n0}）: ' + '・'.join(f'{k} {v}' for k, v in sorted(tops.items(), key=lambda x: -x[1])[:4]))
