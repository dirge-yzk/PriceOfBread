# (774) c7sw の門の台本。事前登録 measurements/m774_20261102_prereg_sw.txt §3。腕の生（計器つき 10 種）に当て、旗 0 の腕と腕 B で同じ台本を使う。
#   python -I scripts/instruments/c7sw/gate774.py <生の根（nation_s・aib_s・inheritintent_s がある所）> [--seeds 1-10] [--x0 N] [--pend0 X] [--floor-u2 1861] [--floor-u2r 106]
#   --x0   旗 0 の腕の L1 の値（腕 B に当てるとき・⌊x0/3⌋ 以下で合格）
#   --pend0 旗 0 の腕の pendingWillCount の平均（腕 B に当てるとき・0.5 倍以上で合格）
# 出力＝種ごとの数と、門ごとの値・合否（旗 0 の腕に当てると合否は〈参考〉・値だけ読む）。
import csv, sys, statistics, argparse, os
ap = argparse.ArgumentParser()
ap.add_argument('root'); ap.add_argument('--seeds', default='1-10'); ap.add_argument('--x0', type=float, default=None); ap.add_argument('--pend0', type=float, default=None)
ap.add_argument('--floor-u2', type=float, default=1861); ap.add_argument('--floor-u2r', type=float, default=106); ap.add_argument('--window', type=int, default=12)
a = ap.parse_args()
lo, hi = [int(x) for x in a.seeds.split('-')]
seeds = list(range(lo, hi + 1))
def rows(name, s):
    p = os.path.join(a.root, f'{name}_s{s}.tsv')
    with open(p, encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r)
        for row in r:
            if len(row) < len(h): row += [''] * (len(h) - len(row))
            yield dict(zip(h, row))
L1 = L1p = 0; l2_seeds = 0; u1a_viol = 0; built_pos_rows = 0; pend_vals = []; u2_vals = []; u2r_vals = []; e_old_gt_new_total = 0; e_old_gt_used_total = 0
print('seed  bornNations  L1join  L1p(starts12Q)  Eold>new  Eold>used  Ebuilt>0  U1aViol  openStarts  openRetry  pendMean')
for s in seeds:
    # 国営の着工（nation_s）
    nat = {}
    for d in rows('nation', s):
        q = int(d['quarter']); n = d['nation']
        goods = [g for g in d['stateFacilityStartedGoods'].split(';') if g]
        nat.setdefault(n, {})[q] = (int(float(d['stateFacilityStartedCount'])), goods)
    aib = {}
    for d in rows('aib', s):
        aib.setdefault(d['nation'], {})[int(d['quarter'])] = int(float(d['startedCount']))
    # 事象の行
    keys = set(); eng = egu = ebp = viol = 0; pend = []
    for d in rows('inheritintent', s):
        if d['scope'] == 'E':
            old, new, built, used = int(d['stateTargetOld']), int(d['stateTargetNew']), int(d['builtCount']), int(d['stateTargetUsed'])
            if old > new:
                eng += 1
                if d['toNation']: keys.add((d['toNation'], d['good']))
            if old > used: egu += 1
            if built > 0: ebp += 1
            if used != new: viol += 1
        elif d['scope'] == 'Q' and int(d['bornQuarter']) == 0:
            pend.append(int(d['pendingWillCount']))
    l1 = l1p = nb = 0; op = opr = 0
    for n, qs in nat.items():
        b = min(qs)
        if b == 0:
            t = sum(v[0] for v in qs.values()); op += t; opr += t - sum(aib.get(n, {}).values()); continue
        nb += 1
        for q, (c, goods) in qs.items():
            if q - b < a.window:
                l1p += c
                l1 += sum(1 for g in goods if (n, g) in keys)
    L1 += l1; L1p += l1p; l2_seeds += egu > 0; u1a_viol += viol; built_pos_rows += ebp; e_old_gt_new_total += eng; e_old_gt_used_total += egu
    pm = sum(pend) / len(pend) if pend else float('nan'); pend_vals.extend(pend); u2_vals.append(op); u2r_vals.append(opr)
    print(f'{s:4d}  {nb:10d}  {l1:6d}  {l1p:13d}  {eng:8d}  {egu:9d}  {ebp:8d}  {viol:7d}  {op:10d}  {opr:9d}  {pm:8.3f}')
pm_all = sum(pend_vals) / len(pend_vals) if pend_vals else float('nan')
u2 = statistics.median(u2_vals); u2r = statistics.median(u2r_vals)
print(f'# L1（結合・種の和）={L1}  L1p（nation_s のみ・種の和）={L1p}  L2（実際に下げた行のある種）={l2_seeds}/{len(seeds)}  U1a 違反の行={u1a_viol}  builtCount>0 の事象の行={built_pos_rows}')
print(f'# 事象の行 old>new={e_old_gt_new_total}  old>used={e_old_gt_used_total}')
print(f'# U1b 開幕からの国の pendingWillCount の平均（年末×国×種）={pm_all:.4f}（行数 {len(pend_vals)}）')
print(f'# U2 開幕からの国の国営の着工（全期間）の種ごとの中央値={u2}（床 {a.floor_u2}）  U2r 再挑戦の着工の種ごとの中央値={u2r}（床 {a.floor_u2r}）')
def verdict(name, ok, ref=False): print(f'# {name}: {"通る" if ok else "落ちる"}{"（参考）" if ref else ""}')
if a.x0 is not None:
    verdict('L1', a.x0 >= 6 and L1 <= int(a.x0 // 3)) if a.x0 >= 6 else print('# L1: X0<6 のため記録に下げる')
    verdict('L2', l2_seeds >= 9)
    verdict('U1a', u1a_viol == 0)
    if a.pend0 is not None: verdict('U1b', (pm_all >= 0.5 * a.pend0) if a.pend0 >= 1 else True)
verdict('L1p', L1p <= 6, a.x0 is None); verdict('U2', u2 >= a.floor_u2, a.x0 is None); verdict('U2r', u2r >= a.floor_u2r, a.x0 is None)
