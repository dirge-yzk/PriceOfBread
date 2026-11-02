# (775) 銀の平価の自己参照を断つ線の門の読み手（読むだけ）。事前登録 measurements/m775_20261102_prereg_ag.txt の §3 の定義そのまま。
#   python -I scripts/instruments/c7ag/gates775.py <計器の出力フォルダ ins> --seeds 1-10 [--mode normal|stress] [--l2base <旗 0 の Σ fiscalBAt3Count>] [--tag 名前]
#   normal：L1・L2・U1 と記録。stress（挟みを外した腕）：U2 と記録。どちらも全部出す（判定は事前登録の §3 の定義の行を読む）。
import sys, os, math, statistics, argparse
sys.stdout.reconfigure(encoding='utf-8', newline=chr(10))
ap = argparse.ArgumentParser()
ap.add_argument('ins'); ap.add_argument('--seeds', default='1-10'); ap.add_argument('--mode', default='normal')
ap.add_argument('--l2base', type=float, default=None); ap.add_argument('--tag', default='')
a = ap.parse_args()
lo_s, hi_s = a.seeds.split('-'); seeds = list(range(int(lo_s), int(hi_s) + 1))
HI, LO = 2.99, 0.31

def table(path):
    with open(path, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        for line in f:
            p = line.rstrip('\n').split('\t')
            if len(p) == len(h): yield dict(zip(h, p))
def fl(x):
    try: return float(x)
    except: return float('nan')
def ols(x, y):
    n = len(x)
    if n < 3: return float('nan')
    mx = sum(x) / n; my = sum(y) / n
    sxx = sum((u - mx) ** 2 for u in x)
    return sum((u - mx) * (v - my) for u, v in zip(x, y)) / sxx if sxx > 0 else float('nan')

print(f'# gates775 tag={a.tag} mode={a.mode} ins={a.ins} seeds={seeds[0]}-{seeds[-1]}')
L1_out = {}; sum_b = 0; qb = 0; seeds_missing = []
U1x = []; U1y = []; U1xl = []; U1yl = []
allmed = []; in_band = 0; tot_q = 0
okflag0 = 0; ceilN = 0; floorN = 0; ratio_new_old = []; fp_rows = []; used_vs_rel = []
for s in seeds:
    path = os.path.join(a.ins, f'silverpar_s{s}.tsv')
    if not os.path.exists(path): seeds_missing.append(s); continue
    rowsS = {}; byq = {}
    for r in table(path):
        q = int(r['quarter'])
        if r['law'] == 'S': rowsS[q] = r
        byq.setdefault(q, {})[r['law']] = r
    out = 0; prev_rel = None; prev_med = None
    for q in sorted(rowsS):
        r = rowsS[q]; nS = int(r['nationCount']); med = fl(r['eMedian']); rel = fl(r['relRecomputedIndex'])
        sum_b += int(r['fiscalBAt3Count']); qb += (int(r['fiscalBAt3Count']) > 0)
        if int(r['ratioOkFlag']) == 0: okflag0 += 1
        ceilN += int(r['eAtCeilCount']); floorN += int(r['eAtFloorCount'])
        old = fl(r['oldParityIndex']); used = fl(r['usedParityIndex'])
        if old > 0 and used > 0: ratio_new_old.append(used / old)
        if rel > 0 and used > 0: used_vs_rel.append(abs(used - rel))
        if nS >= 1 and med == med:
            tot_q += 1; allmed.append(med)
            if med >= HI or med <= LO: out += 1
            if 0.5 <= med <= 2.0: in_band += 1
        if nS >= 3 and med > 0 and rel > 0:
            U1x.append(math.log(rel)); U1y.append(math.log(med))
        if prev_rel is not None and nS >= 3 and med > 0 and prev_rel > 0:
            U1xl.append(math.log(prev_rel)); U1yl.append(math.log(med))
        prev_rel = rel if rel > 0 else None
        # 不動点 E* = (1-w)·mAgEAll(非S) / (1 - w·mAgAll(S))（非S＝金と不換の州の合算）
        w = fl(r['wAgShare']); mS = fl(r['mAgAllIndex']); g = byq[q].get('G'); f = byq[q].get('F')
        cnt = [(int(x['agCount']), fl(x['mAgEAllIndex'])) for x in (g, f) if x is not None and int(x['agCount']) > 0]
        tot = sum(c for c, _ in cnt)
        if tot > 0 and nS >= 1 and med > 0 and w == w and mS == mS and 1 - w * mS > 1e-6:
            est = (1 - w) * (sum(c * m for c, m in cnt) / tot) / (1 - w * mS)
            if est > 0: fp_rows.append((math.log(est), math.log(med)))
    L1_out[s] = out
print('--- L1（銀本位国の E の中央値が %.2f 以上か %.2f 以下の四半期・種ごと）' % (HI, LO))
print('  種ごと:', {s: L1_out[s] for s in sorted(L1_out)}, ' 欠けた種:', seeds_missing)
ok_seeds = sum(1 for s in L1_out if L1_out[s] <= 10)
print(f'  10 以下の種: {ok_seeds}/{len(seeds)}  → L1 ' + (('通る' if ok_seeds >= 9 and not seeds_missing else '落ちる') if len(seeds) >= 10 else '判定せず（10 種の腕だけで判定する・種の数が少ない腕は記録）') + '（9/10 以上で通る）')
print('--- L2（Σ fiscalBAt3Count＝b が上限に貼り付いた 国×四半期）')
print(f'  Σ={sum_b}  b>0 の四半期={qb}  旗 0 の値={a.l2base}' + ('' if a.l2base is None else f'  比={sum_b / a.l2base:.4f}' if a.l2base else '  （旗 0 の値が 0＝記録）'))
if a.l2base:
    print('  → L2 ' + ('通る' if sum_b <= a.l2base / 5 else '落ちる') + '（旗 0 の 1/5 以下で通る）')
sl = ols(U1x, U1y); sll = ols(U1xl, U1yl)
print('--- U1（ln 銀本位国の E の中央値 を ln relRecomputedIndex に回帰した傾き・nationCount_S≥3 の四半期）')
print(f'  n={len(U1x)}  傾き={sl:.4f} → U1 ' + ('通る' if 0.8 <= sl <= 1.2 else '落ちる') + '（[0.8,1.2]）   記録：rel を 1 四半期遅らせた傾き={:.4f}'.format(sll))
print('--- U2（挟みを外した腕で E の中央値が [0.5,2.0] に入る四半期の割合 ≥0.9 かつ全体の中央値 [0.7,1.4]）')
gm = statistics.median(allmed) if allmed else float('nan')
frac = in_band / tot_q if tot_q else float('nan')
print(f'  四半期={tot_q}  [0.5,2.0] 内={frac:.4f}  中央値={gm:.4f} → U2 ' + ('通る' if frac >= 0.9 and 0.7 <= gm <= 1.4 else '落ちる') + ('' if a.mode == 'stress' else '（この腕は通常の挟み＝U2 の判定には使わない・記録）'))
if allmed:
    allmed.sort()
    pc = lambda p: allmed[min(len(allmed) - 1, int(p * len(allmed)))]
    print(f'--- 記録：E_ag の中央値の分位 p5={pc(0.05):.3f} p25={pc(0.25):.3f} p50={pc(0.5):.3f} p75={pc(0.75):.3f} p95={pc(0.95):.3f} 最小={allmed[0]:.3f} 最大={allmed[-1]:.3f}')
print(f'--- 記録：ratioOkFlag=0 の四半期={okflag0}  銀本位国の E が天井 {ceilN}・下限 {floorN}（国×四半期の和）')
if ratio_new_old:
    print(f'--- 記録：同じ盤面の usedParity÷oldParity（銀本位行）の中央値={statistics.median(ratio_new_old):.4f}  最小={min(ratio_new_old):.4f}  最大={max(ratio_new_old):.4f}')
if used_vs_rel:
    print(f'--- 記録：|usedParity−relRecomputed| の最大={max(used_vs_rel):.3e}（旗 1 では 1e-9 以下のはず・旗 0 では旧の統計と新の rel の差）')
if len(fp_rows) > 5:
    d = [x - y for x, y in fp_rows]
    print(f'--- 記録：不動点 E*=(1−w)·mAgE(非S)/(1−w·mAg(S)) と実際の E の中央値（旗 0 の腕で読む物・旗 1 では E が平均に入らないので意味がない・ln の差の中央値={statistics.median(d):.3f}・差の標準偏差={statistics.pstdev(d):.3f}・傾き(ln E_actual on ln E*)={ols([x for x,_ in fp_rows],[y for _,y in fp_rows]):.3f}・n={len(fp_rows)}）')
# 下限に長く貼り付いた国（nation_s の fxRate・全ての国）
stuck = 0; stuck_list = []
for s in seeds:
    path = os.path.join(a.ins, f'nation_s{s}.tsv')
    if not os.path.exists(path): continue
    run = {}; best = {}
    for r in table(path):
        k = r['nation']; v = fl(r['fxRate'])
        if v <= 0.3 + 1e-6: run[k] = run.get(k, 0) + 1; best[k] = max(best.get(k, 0), run[k])
        else: run[k] = 0
    for k, v in best.items():
        if v >= 80: stuck += 1; stuck_list.append((s, k, v))
print(f'--- 記録：ExchangeRateFloor 0.3 に連続 80 四半期（20 年）以上張り付いた 国×種 の数={stuck}  {stuck_list[:12]}')
