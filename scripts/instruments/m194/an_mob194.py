"""(194) 総動員の判断の読み手（読むだけ）。python an_mob194.py <out の dir> <タグ> [--seeds N]
M 行（m194/inj_mob194＝交戦中の国×四半期・AI の判断の直後）と K 行（m193/inj_cnl193＝全ての国×四半期・徴兵法）を読む。
 ① 総動員の割合（交戦中の四半期のうち）＝国ごと・戦争の型ごと
    型＝植民地（直接の敵が全て国を持たない）／小（敵に列強なし・塊の列強 2 以下）／列強（敵に列強・塊の列強 2 以下）／大戦（塊の列強 3 以上）
 ② 総動員の始まり（前0→後1）の入口＝劣勢（陣営の戦力比>1.2）か敗色（>0.5）か・その時の型・相手が全て海越え・陸で押せる敵
 ③ 総動員の続き（国×種の連続の四半期）＝長さの中央・続きの中で直接の敵の組が変わった割合・最初の型と最後の型
 ④ 戦時の徴兵法の上げ（交戦中の四半期に法が前の四半期より上がった）＝総動員中か・型
 ⑤ 候補の物差し（型ごとの中央〔四分位〕）＝陣営の戦力比・押しの比（敵の押し÷自国の押し）・養える軍の比（(敵＋その仲間)÷(自＋共に戦う国)）・陸の届き・戦場の強さ・敗色
 ⑥ 大戦の型の兵士÷人口の頂点（国×種の最大・種の中央）"""
import sys, os, glob, statistics as st, collections
args = sys.argv[1:]
maxsd = 999
if '--seeds' in args:
    i = args.index('--seeds'); maxsd = int(args[i + 1]); del args[i:i + 2]
O = args[0]; TAG = args[1] if len(args) > 1 else ''
MAJ = ['イギリス', 'フランス', 'プロイセン', 'オーストリア', 'ロシア', 'アメリカ', '日本', 'オスマン', 'スペイン', '清']


def fl(x):
    try: return float(x)
    except Exception: return float('nan')


def kind(r):
    if r['nFnat'] == 0: return '植民地'
    if r['cg'] >= 3: return '大戦'
    if r['nFgreat'] > 0: return '列強'
    return '小'


KINDS = ['植民地', '小', '列強', '大戦']
rows = []
for f in sorted(glob.glob(os.path.join(O, 'mob', 'm*.txt'))):
    sd = int(os.path.basename(f)[1:-4])
    if sd > maxsd: continue
    for ln in open(f, encoding='utf-8'):
        t = ln.rstrip('\n').split('|')
        if t[0] != 'M' or len(t) < 42: continue
        r = dict(sd=sd, q=int(t[1]), y=int(t[2]), n=t[3], rank=int(t[4]), m0=int(t[5]), m1=int(t[6]), law=int(t[7]), nWar=int(t[8]), nFoe=int(t[9]),
                 nFnat=int(t[10]), nFgreat=int(t[11]), ratio=fl(t[12]), out=int(t[13]), lose=fl(t[14]), allSea=int(t[15]), foeLands=int(t[16]),
                 selfMil=fl(t[17]), selfLand=fl(t[18]), selfEst=fl(t[19]), selfMan=fl(t[20]), fMil=fl(t[21]), fLand=fl(t[22]), fPush=fl(t[23]), myPush=fl(t[24]),
                 fEst=fl(t[25]), fMan=fl(t[26]), nCo=int(t[27]), cMil=fl(t[28]), cEst=fl(t[29]), cG=int(t[30]), nR2=int(t[31]), dMil=fl(t[32]), dEst=fl(t[33]),
                 dG=int(t[34]), cg=int(t[35]), wz=fl(t[36]), exh=fl(t[37]), sold=fl(t[38]), pop=fl(t[39]), foes=t[40], reach=fl(t[41]))
        r['k'] = kind(r)
        rows.append(r)
seeds = sorted({r['sd'] for r in rows}); nsd = len(seeds) or 1
print(f'# (194) 総動員の判断 {TAG}｜種 {nsd}｜交戦中の国×四半期 {len(rows)}')


def pct(a, b): return f'{100.0 * a / b:.0f}%' if b else '-'


def med(v):
    v = [x for x in v if x == x]
    if not v: return '-'
    v.sort(); n = len(v)
    q1 = v[n // 4]; q3 = v[(3 * n) // 4]
    return f'{st.median(v):.3g}〔{q1:.3g}-{q3:.3g}〕'


# ① 総動員の割合
print('\n## ① 総動員の割合（交戦中の四半期のうち・判断の後）＝全体｜植民地｜小｜列強｜大戦（括弧は交戦の四半期の数／種）')
def share_line(sel, label):
    tot = [r for r in sel]
    parts = [f'{pct(sum(r["m1"] for r in tot), len(tot))}（{len(tot) / nsd:.0f}）']
    for k in KINDS:
        s = [r for r in tot if r['k'] == k]
        parts.append(f'{pct(sum(r["m1"] for r in s), len(s))}（{len(s) / nsd:.0f}）')
    print(f'{label}: ' + '｜'.join(parts))
share_line(rows, '世界')
for n in MAJ: share_line([r for r in rows if r['n'] == n], n)
for era in [(1784, 1849), (1850, 1913), (1914, 1950)]:
    share_line([r for r in rows if era[0] <= r['y'] <= era[1]], f'世界 {era[0]}-{era[1]}')

# ② 始まりの入口
print('\n## ② 総動員の始まり（判断の前0→後1）の入口（主要10か国・種あたり）＝件数｜劣勢のみ｜敗色のみ｜両方｜型（植民地/小/列強/大戦）｜相手が全て海越え｜陸で押せる敵なし')
onsets = [r for r in rows if r['m0'] == 0 and r['m1'] == 1]
for n in ['世界'] + MAJ:
    s = onsets if n == '世界' else [r for r in onsets if r['n'] == n]
    if not s: print(f'{n}: 0'); continue
    o = sum(1 for r in s if r['out'] and not r['lose'] > 0.5); l = sum(1 for r in s if not r['out'] and r['lose'] > 0.5); b = sum(1 for r in s if r['out'] and r['lose'] > 0.5)
    ks = '/'.join(str(sum(1 for r in s if r['k'] == k)) for k in KINDS)
    print(f'{n}: {len(s) / nsd:.1f}｜{pct(o, len(s))}｜{pct(l, len(s))}｜{pct(b, len(s))}｜{ks}｜{pct(sum(r["allSea"] for r in s), len(s))}｜{pct(sum(1 for r in s if not r["foeLands"]), len(s))}')

# ③ 続き
print('\n## ③ 総動員の続き（国×種の連続の四半期）＝続きの数／種｜長さの中央〔四分位〕｜最長｜続きの中で直接の敵の組が変わった割合｜最初の型→最後の型（上位）')
by = collections.defaultdict(list)
for r in rows: by[(r['sd'], r['n'])].append(r)
eps = collections.defaultdict(list)
for (sd, n), v in by.items():
    v.sort(key=lambda r: r['q'])
    cur = []
    for r in v:
        if r['m1'] == 1 and (not cur or r['q'] == cur[-1]['q'] + 1): cur.append(r)
        elif r['m1'] == 1: eps[n].append(cur); cur = [r]
        else:
            if cur: eps[n].append(cur); cur = []
    if cur: eps[n].append(cur)
for n in MAJ + ['(全て)']:
    E = [e for k, es in eps.items() for e in es] if n == '(全て)' else eps.get(n, [])
    if not E: print(f'{n}: 0'); continue
    L = [len(e) for e in E]
    chg = sum(1 for e in E if len({x['foes'] for x in e}) > 1)
    tr = collections.Counter(f"{e[0]['k']}→{e[-1]['k']}" for e in E).most_common(3)
    print(f'{n}: {len(E) / nsd:.1f}｜{med(L)}｜{max(L)}｜{pct(chg, len(E))}｜' + '・'.join(f'{a} {b}' for a, b in tr))

# ④ 戦時の徴兵法の上げ
print('\n## ④ 交戦中に徴兵法が前の四半期より上がった四半期（種あたり）＝件数｜うち総動員中｜型（植民地/小/列強/大戦）')
up = collections.defaultdict(list)
for (sd, n), v in by.items():
    v.sort(key=lambda r: r['q'])
    for i in range(1, len(v)):
        if v[i]['q'] == v[i - 1]['q'] + 1 and v[i]['law'] > v[i - 1]['law']: up[n].append(v[i])
for n in MAJ:
    s = up.get(n, [])
    ks = '/'.join(str(sum(1 for r in s if r['k'] == k)) for k in KINDS)
    print(f'{n}: {len(s) / nsd:.1f}｜{pct(sum(r["m1"] for r in s), len(s))}｜{ks}')

# ⑤ 候補の物差し
print('\n## ⑤ 候補の物差し（主要10か国の交戦中の四半期・型ごとの中央〔四分位〕）')
def feats(r):
    own = r['selfEst'] + r['cEst']; foe = r['fEst'] + r['dEst']
    return dict(ratio=r['ratio'], push=(r['fPush'] / r['myPush']) if r['myPush'] > 0 else (99 if r['fPush'] > 0 else 0),
                est=(foe / own) if own > 0 else float('nan'), estD=(r['fEst'] / r['selfEst']) if r['selfEst'] > 0 else float('nan'),
                man=(r['fMan'] / r['selfMan']) if r['selfMan'] > 0 else float('nan'), reach=r['reach'], wz=r['wz'], lose=r['lose'])
M = [r for r in rows if r['n'] in MAJ]
for k in KINDS:
    s = [feats(r) for r in M if r['k'] == k]
    if not s: continue
    print(f'{k}（{len(s) / nsd:.0f}／種）: 戦力比 {med([x["ratio"] for x in s])}｜押しの比 {med([x["push"] for x in s])}｜養える軍の比(陣営) {med([x["est"] for x in s])}｜養える軍の比(直接) {med([x["estD"] for x in s])}｜兵役の母数の比 {med([x["man"] for x in s])}｜陸の届き {med([x["reach"] for x in s])}｜戦場の強さ {med([x["wz"] for x in s])}｜敗色 {med([x["lose"] for x in s])}')
for n in ['イギリス', 'アメリカ', 'フランス', 'プロイセン', 'ロシア', '日本']:
    for k in KINDS:
        s = [feats(r) for r in M if r['n'] == n and r['k'] == k]
        if len(s) < 4: continue
        print(f'  {n} {k}（{len(s) / nsd:.1f}）: 戦力比 {med([x["ratio"] for x in s])}｜押しの比 {med([x["push"] for x in s])}｜養える軍の比(陣営) {med([x["est"] for x in s])}｜陸の届き {med([x["reach"] for x in s])}｜戦場 {med([x["wz"] for x in s])}')

# ⑥ 大戦の型の兵士÷人口の頂点
print('\n## ⑥ 兵士÷人口の頂点（交戦中・国×種の最大・種の中央〔最小-最大〕）＝大戦の型｜全ての交戦')
for n in MAJ:
    a = []; b = []
    for sd in seeds:
        v = [r for r in by.get((sd, n), []) if r['pop'] > 0]
        if not v: continue
        b.append(max(r['sold'] / r['pop'] for r in v))
        g = [r['sold'] / r['pop'] for r in v if r['k'] == '大戦']
        if g: a.append(max(g))
    fa = f'{100 * st.median(a):.2f}%〔{100 * min(a):.2f}-{100 * max(a):.2f}〕（{len(a)}種）' if a else '-'
    fb = f'{100 * st.median(b):.2f}%' if b else '-'
    print(f'{n}: {fa}｜{fb}')

# ⑦ 1914-1950 の大戦の型で総動員したか（国×種）＝総動員した種の数／その型で交戦した種の数｜その型の四半期のうち総動員の割合｜1784-1913 の大戦の型の割合
print('\n## ⑦ 大戦の型（塊の列強 3 以上）での総動員＝1914-1950：総動員した種／交戦した種｜四半期の割合｜1784-1913：四半期の割合')
for n in MAJ:
    late = [r for r in rows if r['n'] == n and r['k'] == '大戦' and r['y'] >= 1914]
    early = [r for r in rows if r['n'] == n and r['k'] == '大戦' and r['y'] < 1914]
    sd_w = {r['sd'] for r in late}; sd_m = {r['sd'] for r in late if r['m1']}
    print(f'{n}: {len(sd_m)}/{len(sd_w)}｜{pct(sum(r["m1"] for r in late), len(late))}（{len(late) / nsd:.0f}）｜{pct(sum(r["m1"] for r in early), len(early))}（{len(early) / nsd:.0f}）')
# ⑧ 総動員の入り切りの回数（交戦中の四半期での切り替え）＝入り／切り（交戦中）／種
print('\n## ⑧ 交戦中の切り替え（種あたり）＝入り｜交戦中の切り（講和でない復員）｜同じ戦争の組の中で入って切ってまた入った回数')
for n in ['(全て)'] + MAJ:
    s = rows if n == '(全て)' else [r for r in rows if r['n'] == n]
    on = sum(1 for r in s if r['m0'] == 0 and r['m1'] == 1); off = sum(1 for r in s if r['m0'] == 1 and r['m1'] == 0)
    print(f'{n}: {on / nsd:.1f}｜{off / nsd:.1f}')
