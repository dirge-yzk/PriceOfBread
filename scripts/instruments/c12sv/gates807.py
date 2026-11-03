#!/usr/bin/env python3
# (807) c12sv 銀の平価の注入の票の門の読み手（読むだけ）。事前登録 measurements/m807_*_prereg_sv.txt の定義そのまま。
#   python -I scripts/instruments/c12sv/gates807.py <計器の出力フォルダ ins> [--seeds 1-10] [--tag 名前] [--tau 0.2] [--table]
#   表 silverwt_s<seed>.tsv（助走の注入も含む・1 注入 1 行）を読む。--table は注入ごとの真の表（種の最小〜最大）を出す。
# 門（旗 1 の腕で通る・旗 0 の盤面では落ちる）：
#   W1 働いた（片側の州）：助走の全ての注入（seq 1〜20）で usedOneSidedShare ≦ 0.05（全ての種・仕様の注入 2 を含む）。
#   W2 働いた（挟み）：W2a＝助走の注入で clampHitCount の和 == 0（全ての種）・W2b＝本走の注入で clampHitCount の和 == 0（仕様の〈全ての注入で 0〉）。
#   U1 上限（1 州の独占）：全ての注入のうち usedMaxShare ＞ 0.5 の割合 ≦ 0.01（全ての種を併せて）。
#   S1 形の一致：ratioOkFlag==1 の全ての注入で |usedRelIndex/relW1Index − 1| ≦ 1e-12（全ての種）。
# 記録（門にしない）：本走の頭 8 四半期の usedRel・本走の rel の水準・rel<1 の注入の割合（silverDrop が正になる）・ratioOkFlag==0 の数・片側の州の数の割合（等重み）・w1ZeroWeightCount。
import sys, os, math, argparse, statistics
sys.stdout.reconfigure(encoding='utf-8', newline=chr(10))
ap = argparse.ArgumentParser()
ap.add_argument('ins'); ap.add_argument('--seeds', default='1-10'); ap.add_argument('--tag', default='')
ap.add_argument('--tau', type=float, default=0.2); ap.add_argument('--table', action='store_true')
a = ap.parse_args()
lo_s, hi_s = a.seeds.split('-'); seeds = list(range(int(lo_s), int(hi_s) + 1))

def table(path):
    with open(path, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        for line in f:
            p = line.rstrip('\n').split('\t')
            if len(p) == len(h): yield dict(zip(h, p))
def fl(x):
    try: return float(x)
    except: return float('nan')

rows = {}; missing = []
for s in seeds:
    p = os.path.join(a.ins, f'silverwt_s{s}.tsv')
    if not os.path.exists(p): missing.append(s); continue
    rows[s] = [dict(r, frozen=r.get('frozen', r.get('frame'))) for r in table(p)]   # 試作の計器の列名 frame も読む
print(f'# gates807 tag={a.tag} ins={a.ins} seeds={seeds[0]}-{seeds[-1]} 欠けた種={missing} tau={a.tau}')
if a.table:
    print('--- 注入ごとの真の表（種の 最小〜最大・seq 1〜8 と本走の頭 1・2）')
    cols = ['relEqualIndex', 'relW1Index', 'relSqIndex', 'relProdIndex', 'usedRelIndex', 'equalOneSidedShare', 'w1OneSidedShare', 'usedOneSidedShare', 'w1MaxShare', 'sqMaxShare', 'prodMaxShare', 'usedMaxShare', 'clampHitCount', 'silverNationCount', 'stateCount', 'w1ZeroWeightCount']
    print('seq\tframe\t' + '\t'.join(cols))
    nseq = max((len(v) for v in rows.values()), default=0)
    for q in list(range(1, 9)) + [21, 22]:
        vs = [r for s in rows for r in rows[s] if int(r['seq']) == q]
        if not vs: continue
        out = []
        for c in cols:
            xs = [fl(r[c]) for r in vs if fl(r[c]) == fl(r[c])]
            out.append(f'{min(xs):.4g}〜{max(xs):.4g}' if xs else 'nan')
        print(f'{q}\t{vs[0]["frozen"]}\t' + '\t'.join(out))

# W1・W2a・W2b・U1・S1
w1v = {}; w2a = {}; w2b = {}; s1v = {}; ok0 = 0; w1max_all = 0.0; n_inj = 0; n_big = 0; n_main = 0; n_main_hit = 0
for s, rs in rows.items():
    for r in rs:
        q = int(r['seq']); fr = r['frozen'] == '1'
        if fr:
            v = fl(r['usedOneSidedShare'])
            w1v[s] = max(w1v.get(s, 0.0), v if v == v else 1.0)   # nan（票の和 0）は落ちる側へ
            w2a[s] = w2a.get(s, 0) + int(r['clampHitCount'])
        else:
            n_main += 1; w2b[s] = w2b.get(s, 0) + int(r['clampHitCount'])
            if int(r['clampHitCount']) > 0: n_main_hit += 1
        ms = fl(r['usedMaxShare']); n_inj += 1
        if not (ms <= 0.5): n_big += 1   # nan も大きい側へ数える（否定形）
        if r['ratioOkFlag'] == '1':
            u = fl(r['usedRelIndex']); w = fl(r['relW1Index'])
            if w == w and w > 0: s1v[s] = max(s1v.get(s, 0.0), abs(u / w - 1))
        else: ok0 += 1
        wm = fl(r['w1MaxShare'])
        if wm == wm: w1max_all = max(w1max_all, wm)
print('--- W1 助走の全ての注入（seq 1〜20）の usedOneSidedShare の最大（種ごと）:', {s: round(v, 4) for s, v in sorted(w1v.items())}, ' 最大=%.4f' % max(w1v.values(), default=float('nan')))
w1ok = bool(w1v) and not missing and all(v <= 0.05 for v in w1v.values())
print('    → W1 ' + ('通る' if w1ok else '落ちる') + '（全ての種・助走の全ての注入で 0.05 以下）')
w2aok = bool(w2a) and not missing and all(v == 0 for v in w2a.values())
print('--- W2a 助走の注入で挟みに当たった銀本位国の数の和（種ごと）:', dict(sorted(w2a.items())), ' 合計=%d' % sum(w2a.values()))
print('    → W2a ' + ('通る' if w2aok else '落ちる') + '（全ての種で 0）')
w2bok = bool(w2b) and not missing and sum(w2b.values()) == 0
print('--- W2b 本走の注入で挟みに当たった銀本位国の数の和（種ごと）:', dict(sorted(w2b.items())), ' 合計=%d  当たった注入=%d／%d（%.4f）' % (sum(w2b.values()), n_main_hit, n_main, n_main_hit / n_main if n_main else float('nan')))
print('    → W2b ' + ('通る' if w2bok else '落ちる') + '（全ての種で 0＝仕様の〈全ての注入で 0〉の本走の側）')
u1frac = n_big / n_inj if n_inj else float('nan')
u1ok = n_inj > 0 and not missing and u1frac <= 0.01
print('--- U1 usedMaxShare＞0.5 の注入の割合（全ての種・全ての注入）= %d／%d = %.4f' % (n_big, n_inj, u1frac))
print('    → U1 ' + ('通る' if u1ok else '落ちる') + f'（0.01 以下）  記録：需要の票を独立に組んだ w1MaxShare の全体の最大={w1max_all:.4f}')
s1ok = bool(s1v) and not missing and all(v <= 1e-12 for v in s1v.values())
print('--- S1 形の一致 |usedRel/relW1−1| の最大（種ごと）:', {s: float('%.3g' % v) for s, v in sorted(s1v.items())}, ' 最大=%.3e' % max(s1v.values(), default=float('nan')))
print('    → S1 ' + ('通る' if s1ok else '落ちる') + '（全ての種で 1e-12 以下）')
print('--- 全体: ' + ('全ての門が通る' if (w1ok and w2aok and w2bok and u1ok and s1ok) else '落ちる門がある') + '（W2＝W2a かつ W2b）')
# 記録
print('--- 記録：ratioOkFlag==0 の注入の数=%d' % ok0)
allrel = [fl(r['usedRelIndex']) for rs in rows.values() for r in rs if r['frozen'] == '0']
head = [fl(r['usedRelIndex']) for rs in rows.values() for r in rs if r['frozen'] == '0' and int(r['seq']) <= 20 + 8]
if allrel:
    print('    本走の usedRel：中央値=%.4f p5=%.4f p95=%.4f  頭 8 四半期の中央値=%.4f  usedRel<1 の注入の割合（銀の下落の項が正）=%.4f' % (
        statistics.median(allrel), sorted(allrel)[int(0.05 * len(allrel))], sorted(allrel)[min(len(allrel) - 1, int(0.95 * len(allrel)))],
        statistics.median(head) if head else float('nan'), sum(1 for x in allrel if x < 1) / len(allrel)))
eq = [fl(r['relEqualIndex']) for rs in rows.values() for r in rs if r['frozen'] == '0']
w1 = [fl(r['relW1Index']) for rs in rows.values() for r in rs if r['frozen'] == '0']
if eq and w1:
    print('    本走の同じ盤面の relW1÷relEqual：中央値=%.4f  最小=%.4f  最大=%.4f' % (statistics.median([x / y for x, y in zip(w1, eq) if y > 0 and x == x]), min(x / y for x, y in zip(w1, eq) if y > 0 and x == x), max(x / y for x, y in zip(w1, eq) if y > 0 and x == x)))
zw = sum(int(r['w1ZeroWeightCount']) for rs in rows.values() for r in rs)
print('    票 0 の州（需要 0 か非有限）の延べ数=%d' % zw)
if a.table:
    # 紙の予言（旗 0 のプローブの独立な列から、旗 1 の注入が relW1 になったと置いた一次の予言＝軌道は替わる）
    war = [r for rs in rows.values() for r in rs if r['frozen'] == '1']; main = [r for rs in rows.values() for r in rs if r['frozen'] == '0']
    out = lambda r: not (0.3 <= fl(r['relW1Index']) <= 3.0)
    print('--- 紙の予言（旗 1 の一次）：W1＝助走の w1OneSidedShare の最大=%.5f・W2a＝助走で relW1 が挟みの外の注入の銀本位国の数の和=%d・W2b＝本走で同=%d（注入 %d）・U1＝w1MaxShare＞0.5 の割合=%.4f（二乗 %.4f・産出 %.4f）' % (
        max(fl(r['w1OneSidedShare']) for r in war), sum(int(r['silverNationCount']) for r in war if out(r)),
        sum(int(r['silverNationCount']) for r in main if out(r)), sum(1 for r in main if out(r)),
        sum(1 for rs in rows.values() for r in rs if fl(r['w1MaxShare']) > 0.5) / sum(len(rs) for rs in rows.values()),
        sum(1 for rs in rows.values() for r in rs if fl(r['sqMaxShare']) > 0.5) / sum(len(rs) for rs in rows.values()),
        sum(1 for rs in rows.values() for r in rs if fl(r['prodMaxShare']) > 0.5) / sum(len(rs) for rs in rows.values())))
    for k, nm in (('relW1Index', 'w1'), ('relSqIndex', '二乗'), ('relProdIndex', '産出')):
        print('    挟み[0.3,3]の外の注入（%s）：助走 %d・本走 %d' % (nm, sum(1 for r in war if not 0.3 <= fl(r[k]) <= 3.0), sum(1 for r in main if not 0.3 <= fl(r[k]) <= 3.0)))
