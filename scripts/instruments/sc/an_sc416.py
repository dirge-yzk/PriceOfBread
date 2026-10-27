"""(416) 工業化度の読み手の置き換えと国力の物差しの読み手（読むだけ・採否に使わない記録の線 R1〜R6）。
python an_sc416.py <計器の出力ディレクトリ> [tag]
  入力＝標準の計器（INS=1）の score_s<seed>.tsv（国×年末・全国）と modsig_s<seed>.tsv（8 国は毎四半期・他は年末）。
  R1 国力の年ごとの顔ぶれ（旧と新）と清・英・米・日の順位（種ごとの中央値と幅）／史実の列強の名簿（記憶・出典未確認）との一致数
  R2 列強の入れ替わり・条約港の風化の速さ（比÷0.5 のクランプの平均）
  R4 近代化投資の信号 returnShare の年ごとの分布・信号 0 の割合
  R5 工業化度 meanIndustrialization の高い国×四半期の数（4.5 以上・4.9 以上）と 8 大国の軌道
  R6 近代化スタンス stance の分布（生の値）"""
import sys, os, glob, statistics, collections

def fl(x):
    try: return float(x)
    except Exception: return float('nan')

def table(path):
    with open(path, encoding='utf-8') as fh:
        head = fh.readline().rstrip('\n').split('\t')
        for line in fh:
            v = line.rstrip('\n').split('\t')
            if len(v) == len(head): yield dict(zip(head, v))

D = sys.argv[1]
TAG = sys.argv[2] if len(sys.argv) > 2 else ''
YEARS = [1784, 1800, 1820, 1850, 1880, 1900, 1914, 1930, 1950]
FOUR = ['清', 'イギリス', 'アメリカ', '日本']
EIGHT = ['清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本']
# 史実の列強の名簿（Correlates of War の主要国の記憶・出典未確認・採否に使わない）：年→その年以降に加わる国名の部分文字列
ROSTER = [(1816, ['イギリス', 'フランス', 'プロイセン', 'オーストリア', 'ロシア']), (1860, ['イタリア']), (1895, ['日本']), (1898, ['アメリカ'])]

def roster_at(y):
    r = []
    for yy, names in ROSTER:
        if y >= yy: r += names
    return r

def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')

def f(x, nd=1):
    return 'NA' if x != x else f'{x:.{nd}f}'

seeds = sorted(glob.glob(os.path.join(D, 'score_s*.tsv')))
print(f'== (416) 読み手 tag={TAG} dir={D} score_s={len(seeds)} 本')

# ---- score_s を種ごとに読む ----
S = {}   # (seed, year) -> list of dict
for p in seeds:
    sd = os.path.basename(p)[len('score_s'):-4]
    last = {}
    for r in table(p):
        y = int(r['year']); q = int(r['quarter'])
        if (sd, y) not in last or q >= last[(sd, y)]: last[(sd, y)] = q
    for r in table(p):
        y = int(r['year']); q = int(r['quarter'])
        if q == last[(sd, y)]:
            S.setdefault((sd, y), []).append(r)

def years_avail():
    return sorted({y for (_, y) in S})

print('\n[R1] 国力の顔ぶれ（旧→新）。清・英・米・日の順位＝種ごとの中央値（最小〜最大）／新しい点の中央値')
for y in YEARS:
    rows_by_seed = {sd: rs for (sd, yy), rs in S.items() if yy == y}
    if not rows_by_seed: continue
    print(f' -- {y} 年（{len(rows_by_seed)} 種）')
    for nm in FOUR:
        ro, rn, sc, so = [], [], [], []
        for sd, rs in rows_by_seed.items():
            for r in rs:
                if r['nation'] == nm:
                    ro.append(fl(r['rankOld'])); rn.append(fl(r['rankShare'])); sc.append(fl(r['scoreShare'])); so.append(fl(r['scoreOld']))
        if ro:
            print(f'   {nm:6s} 旧 {f(med(ro),0):>3s}位（{f(min(ro),0)}〜{f(max(ro),0)}） 新 {f(med(rn),0):>3s}位（{f(min(rn),0)}〜{f(max(rn),0)}） 新点 {f(med(sc),2)} 旧点 {f(med(so),1)}')
    # 新しい上位 5 の国の出現（種をまたぐ）
    cnt = collections.Counter(); cnto = collections.Counter()
    for sd, rs in rows_by_seed.items():
        for r in sorted(rs, key=lambda r: fl(r['rankShare']))[:5]: cnt[r['nation']] += 1
        for r in sorted(rs, key=lambda r: fl(r['rankOld']))[:5]: cnto[r['nation']] += 1
    print('   新の上位5に入った回数（種）:', ' '.join(f'{k}{v}' for k, v in cnt.most_common(8)))
    print('   旧の上位5に入った回数（種）:', ' '.join(f'{k}{v}' for k, v in cnto.most_common(8)))
    # 史実の列強の名簿との一致（Great の集合）
    ros = roster_at(y)
    if y >= 1816 and ros:
        hit_o = hit_n = 0; ng_o = ng_n = 0
        for sd, rs in rows_by_seed.items():
            gO = {r['nation'] for r in rs if r['isGreatOld'] == '1'}
            gN = {r['nation'] for r in rs if r['isGreatShare'] == '1'}
            hit_o += sum(1 for n in ros if any(n in g for g in gO)); hit_n += sum(1 for n in ros if any(n in g for g in gN))
            ng_o += len(gO); ng_n += len(gN)
        k = len(rows_by_seed)
        print(f'   名簿{len(ros)}国のうち Great に入った数（種の平均）: 旧 {hit_o/k:.2f} 新 {hit_n/k:.2f}／Great の人数（種の平均）: 旧 {ng_o/k:.1f} 新 {ng_n/k:.1f}')

print('\n[R2] 列強（Great）の入れ替わり（年末断面の集合の変化の回数・種の平均）と条約港の風化の速さ')
ch_o = ch_n = 0; n_seed = 0
for sd in sorted({s for (s, _) in S}):
    ys = sorted(y for (s, y) in S if s == sd)
    prevO = prevN = None; n_seed += 1
    for y in ys:
        rs = S[(sd, y)]
        gO = frozenset(r['nation'] for r in rs if r['isGreatOld'] == '1'); gN = frozenset(r['nation'] for r in rs if r['isGreatShare'] == '1')
        if prevO is not None:
            ch_o += len(gO ^ prevO); ch_n += len(gN ^ prevN)
        prevO, prevN = gO, gN
print(f'   Great の入れ替わり（対称差の人数・種の平均）: 旧 {ch_o/max(n_seed,1):.1f} 新 {ch_n/max(n_seed,1):.1f}')
for y in [1850, 1900, 1914]:
    po, pn, so, sn, bo, bn = [], [], [], [], [], []
    for (sd, yy), rs in S.items():
        if yy != y: continue
        gmaxO = max([fl(r['scoreOld']) for r in rs if r['isGreatOld'] == '1'] or [float('nan')])
        gmaxN = max([fl(r['scoreShare']) for r in rs if r['isGreatShare'] == '1'] or [float('nan')])
        for r in rs:
            if r['isGreatShare'] == '1' and r['isGreatOld'] == '1': continue
            ro_ = fl(r['scoreOld']) / gmaxO if gmaxO == gmaxO and gmaxO > 0 else float('nan')
            rn_ = fl(r['scoreShare']) / gmaxN if gmaxN == gmaxN and gmaxN > 0 else float('nan')
            if ro_ == ro_: po.append(min(1.0, ro_ / 0.5)); bo.append(ro_)
            if rn_ == rn_: pn.append(min(1.0, rn_ / 0.5)); bn.append(rn_)
    if po:
        print(f'   {y} 年 列強以外の国の 自国÷列強の最大 の中央値: 旧 {med(bo):.3f} 新 {med(bn):.3f}／風化の係数 Clamp01(比÷0.5) の平均: 旧 {sum(po)/len(po):.3f} 新 {sum(pn)/len(pn):.3f}')

# ---- modsig_s ----
ms = sorted(glob.glob(os.path.join(D, 'modsig_s*.tsv')))
print(f'\n[R4/R5/R6] modsig_s={len(ms)} 本')
rs_by_year = collections.defaultdict(list)    # year -> returnShare（8 国・毎四半期）
hi45 = hi49 = 0; rows8 = 0; zero = 0; maxmi = 0.0
traj = collections.defaultdict(lambda: collections.defaultdict(list))   # nation -> year -> [meanInd]
stance = collections.Counter(); flag = collections.Counter()
sig_pos_t = collections.defaultdict(lambda: [0, 0])
cap_nations = collections.Counter()
for p in ms:
    for r in table(p):
        nm = r['nation']; y = int(r['year'])
        mi = fl(r['meanIndustrialization']); rsg = fl(r['returnShare'])
        flag[r.get('flagLevel', '(列なし・(591) で消えた)')] += 1
        if mi == mi: maxmi = max(maxmi, mi)
        if nm in EIGHT:
            rows8 += 1
            rs_by_year[y].append(rsg)
            if rsg == rsg and rsg <= 0: zero += 1
            if mi == mi and mi >= 4.5: hi45 += 1
            if mi == mi and mi >= 4.9: hi49 += 1
            traj[nm][y].append(mi)
            stance[r['stance']] += 1
        if mi == mi and mi >= 4.9: cap_nations[(os.path.basename(p), nm)] += 1
print(f'   flagLevel の値の分布: {dict(flag)}（B は 3 のはず・A は 0）')
print(f'   8 国×四半期 {rows8} 行：returnShare が 0 以下の割合 {zero/max(rows8,1)*100:.1f}%／meanInd 4.5 以上 {hi45} 行（{hi45/max(rows8,1)*100:.1f}%）・4.9 以上 {hi49} 行／全国の meanInd の最大 {maxmi:.2f}')
print('   returnShare（8 国・毎四半期）の年ごとの中央値（最小〜最大）:')
for y in YEARS:
    xs = rs_by_year.get(y, [])
    if xs: print(f'     {y}: {med(xs):.3f}（{min(xs):.3f}〜{max(xs):.3f}）n={len(xs)}')
print('   8 国の meanIndustrialization（種をまたぐ中央値）:')
print('     ' + ' '.join(f'{y}' for y in YEARS))
for nm in EIGHT:
    print(f'     {nm:6s} ' + ' '.join(f(med(traj[nm].get(y, [])), 2) for y in YEARS))
print('   stance の生の値（8 国×四半期）:', dict(sorted(stance.items())))
print(f'   4.9 以上に達した（計器ファイル×国）の数: {len(cap_nations)}')
