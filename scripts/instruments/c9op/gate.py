#!/usr/bin/env python3
# (787) c9op アヘンの蓋の段2 の門の読み手。計器の出力フォルダ（habitcap_s*.tsv・habitbal_s*.tsv）を読んで、事前登録の両側の門を出す。
#   python -I scripts/instruments/c9op/gate.py <ins のフォルダ> [--paper]       --paper＝旗 0 の紙の盤面（habitbal_s が無い）：下限の量だけを出す
# 数は事前登録（measurements/m787_20261102_prereg_op.txt）から写した物。結果を見た後に変えない。
import sys, glob, math, collections, re
sys.stdout.reconfigure(encoding='utf-8')
# --- 事前登録の数（腕の前に固定）
CAP = 0.2
L1_PER_SEED = 880          # 下限 L1：旧が蓋に当たり(oldNext ≧ 蓋)新が蓋を超える行の 1 種あたり ≧ 880（紙＝m784 の種 1〜10 の 1761.3 の半分）
L2_PER_SEED = 22.0         # 下限 L2：Σ(新−旧) の 1 種あたり ≧ 22.0（紙＝m784 の種 1〜10 の 44.36 の半分）
U2_MAX = 3.30              # 上限 U2：実際の次の在庫の最大 ≦ 3.30（紙の釣り合いの最大 2.1976 の 1.5 倍）
U3_SHARE = 0.010           # 上限 U3：労働の床が効く月の割合 ≦ 1.0%（紙の釣り合い ≧0.5 の行の割合 0.40〜0.52% の約 2 倍）
def rd(path):
    with open(path, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {n: i for i, n in enumerate(h)}
        for l in fh:
            p = l.rstrip('\n').split('\t')
            if len(p) < len(h): continue
            yield ix, p
def seedof(f): return int(re.search(r'_s(\d+)\.tsv$', f).group(1))
def main(d, paper):
    caps = sorted(glob.glob(d + '/habitcap_s*.tsv')); bals = sorted(glob.glob(d + '/habitbal_s*.tsv'))
    seeds = len(caps)
    print('フォルダ', d, '／habitcap_s', seeds, '本／habitbal_s', len(bals), '本')
    rows = months = bind = 0; l1 = 0; excess = 0.0; l3 = 0; flag = set(); over_cap_rows = 0
    for f in caps:
        for ix, p in rd(f):
            rows += 1; months += int(p[ix['monthsCount']]); bind += int(p[ix['bindMonthsCount']])
            cap = float(p[ix['capIndex']]); old = float(p[ix['oldNextLastIndex']]); new = float(p[ix['newNextLastIndex']]); used = float(p[ix['usedNextLastIndex']])
            flag.add(p[ix['capOffFlag']])
            if old >= cap and used > cap: l1 += 1
            if old >= cap and new > cap: over_cap_rows += 1
            excess += float(p[ix['excessSumIndex']])
            if old != min(new, cap): l3 += 1     # 同じ盤面の旧＝新を旧の蓋で切った値（"R" 書式の往復で bit 一致）
    print('[habitcap_s] 行', rows, '月', months, '蓋が効いた月（新＞旧）', bind, '(%.4f%%)' % (100 * bind / max(1, months)), '旗の値', sorted(flag))
    print('  L1 旧が蓋に当たり新が蓋を超える行（新で数える）', over_cap_rows, '／実際(used)が蓋を超える行', l1, '／1 種あたり %.1f' % (l1 / seeds), '（門 ≧ %d）' % L1_PER_SEED)
    print('  L2 Σ(新−旧) 合計 %.4f／1 種あたり %.3f（門 ≧ %.1f）' % (excess, excess / seeds, L2_PER_SEED))
    print('  L3 旧≠min(新,蓋) の行', l3, '（0 であるべき＝計器の同じ盤面の旧が旗に依らず出る）')
    res = {}
    res['L1'] = (l1 / seeds >= L1_PER_SEED) ; res['L2'] = (excess / seeds >= L2_PER_SEED) ; res['L3'] = (l3 == 0)
    if paper:
        print('紙の台本（旗 0）：L1 は new で数えた値が目安（旗 0 では used が蓋を超えない）。1 種あたり new 基準 %.1f' % (over_cap_rows / seeds))
        return
    # 実際の列と新の一致（旗 1 では used が new と bit 一致）
    mism = 0
    for f in caps:
        for ix, p in rd(f):
            if p[ix['capOffFlag']] != '0' and float(p[ix['usedNextLastIndex']]) != float(p[ix['newNextLastIndex']]): mism += 1
    print('  旗が正の行で used ≠ new の行', mism)
    viol = 0; floorm = 0; bm = 0; umax = 0.0; vmax = 0.0; rowsb = 0; hi = 0; pcmax = 0.0
    byreg = collections.Counter(); usedmaxs = []
    for f in bals:
        for ix, p in rd(f):
            rowsb += 1; bm += int(p[ix['monthsCount']]); viol += int(p[ix['balViolMonthsCount']]); fm = int(p[ix['floorMonthsCount']]); floorm += fm
            um = float(p[ix['stockUsedMaxIndex']]); usedmaxs.append(um)
            if um > umax: umax = um
            vmax = max(vmax, float(p[ix['balViolMaxIndex']])); pcmax = max(pcmax, float(p[ix['pcMaxPerMonth']]))
            if um > CAP: hi += 1
            if fm: byreg[p[ix['nation']] + '/' + p[ix['region']]] += fm
    usedmaxs.sort()
    q = lambda x: usedmaxs[min(len(usedmaxs) - 1, int(len(usedmaxs) * x))] if usedmaxs else float('nan')
    print('[habitbal_s] 行', rowsb, '月', bm)
    print('  U1 釣り合いを破った月', viol, '（門 ＝ 0）／破った量の最大 %.3e' % vmax)
    print('  U2 次の在庫の最大 %.4f（門 ≦ %.2f）／p99 %.4f p999 %.4f／1 人あたり消費の最大 %.4f／在庫が蓋 0.2 を超えた行 %d' % (umax, U2_MAX, q(.99), q(.999), pcmax, hi))
    sh = floorm / max(1, bm)
    print('  U3 労働の床が効く月 %d／月 %d＝%.4f%%（門 ≦ %.1f%%）' % (floorm, bm, 100 * sh, 100 * U3_SHARE))
    print('  記録：床が効く月の多い州（上位 12）', byreg.most_common(12))
    res['U1'] = (viol == 0); res['U2'] = (umax <= U2_MAX); res['U3'] = (sh <= U3_SHARE)
    res['L3b'] = (mism == 0)
    print('門の判定：' + '／'.join('%s %s' % (k, '○' if v else '×') for k, v in sorted(res.items())))
    print('結果：' + ('全て通る' if all(res.values()) else '通らない門がある'))
if __name__ == '__main__':
    a = [x for x in sys.argv[1:] if not x.startswith('--')]
    main(a[0], '--paper' in sys.argv)
