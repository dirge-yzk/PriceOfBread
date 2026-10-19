# ★(434) 清算の片方向の錠の読み手（読むだけ・シミュを回さない）。事前登録 measurements/cl_20261019_prereg.txt の [4]〜[6]。
# 使い方：PYTHONUTF8=1 python csc434_read.py <計器の生のフォルダ> [N=4] [種の数=10]
# csc_s<seed>.tsv の末尾の列（throttledRowsCount ほか）から、国×時代で次を出す：
#   律速の州の行の割合（種類 1 の行・種類 4 の飛ばした行）・欠けた潜在量の割合 L（種類 1 の素の潜在量×(N−1)÷(種類 0＋種類 1×N)）・
#   (ii) が増やす走査の見積り（(N−1)×律速の州の種類 1 の行と充填）と増える秒（充填の単価 0.10014 µs・種の数で割る）。
# 旗 1 の腕では種類 1 の素の量は同じ・足した量（potAddedPerYear）が重みを含む＝P_B÷P_A も出す（第 3 引数に A のフォルダ）。
import csv, sys, collections
d = sys.argv[1]; N = int(sys.argv[2]) if len(sys.argv) > 2 else 4; SEEDS = int(sys.argv[3]) if len(sys.argv) > 3 else 10
dA = sys.argv[4] if len(sys.argv) > 4 else None
FILL_US = 0.10014
NAT = ['清', 'ロシア', 'イギリス', 'フランス', 'プロイセン', 'オーストリア', 'オスマン', '日本']
def era(y): return '1784-1829' if y < 1830 else '1830-1869' if y < 1870 else '1870-1950'
def load(folder):
    acc = collections.defaultdict(lambda: collections.defaultdict(float))  # (nat,era,cls)->field->sum
    n = 0
    for s in range(1, SEEDS + 1):
        try: f = open(f'{folder}/csc_s{s}.tsv', encoding='utf-8')
        except OSError: continue
        n += 1
        with f:
            for r in csv.DictReader(f, delimiter='\t'):
                if r['roleCode'] != '0': continue
                if 'throttledRowsCount' not in r: raise SystemExit('csc に (434) の列が無い＝古い計器の生')
                k = (r['nation'], era(int(r['year'])), int(r['classCode']))
                a = acc[k]
                for c in ['rowsCount', 'dealtRowsCount', 'throttledRowsCount', 'throttledDealtRowsCount', 'fillsCount', 'throttledFillsCount',
                          'potRawPerYear', 'potAddedPerYear', 'throttledPotRawPerYear', 'qtyPerYear', 'throttledQtyPerYear']:
                    a[c] += float(r[c])
    return acc, n
A, nA = load(d)
B = load(dA)[0] if dA else None
print(f'folder={d} seeds={nA} N={N}')
print('nation era | cls1 rows thr% | cls4 skipped rows thr% | thrShare(cls1 pot) | L(lost pot share) | (ii) extra rows/seed fills/seed sec/seed')
tot_sec = 0.0
for n in NAT:
    for e in ['1784-1829', '1830-1869', '1870-1950']:
        c0 = A.get((n, e, 0)); c1 = A.get((n, e, 1)); c4 = A.get((n, e, 4))
        if not c0 or not c1: continue
        P0 = c0['potRawPerYear']; P1 = c1['potRawPerYear']
        L = (N - 1) * P1 / max(1e-300, P0 + N * P1)
        r1 = c1['rowsCount']; r1t = c1['throttledRowsCount']; r4 = c4['rowsCount'] if c4 else 0; r4t = c4['throttledRowsCount'] if c4 else 0
        ex_rows = (N - 1) * r1t / nA; ex_fills = (N - 1) * c1['throttledFillsCount'] / nA; sec = ex_fills * FILL_US * 1e-6
        tot_sec += sec
        thrp = c1['throttledPotRawPerYear'] / max(1e-300, P1)
        line = f'{n} {e} | {int(r1)} {100*r1t/max(1,r1):.0f}% | {int(r4)} {100*r4t/max(1,r4):.0f}% | {thrp:.0%} | {L:.1%} | {ex_rows:.0f} {ex_fills:.3g} {sec:.3f}'
        if B is not None:
            b0 = B.get((n, e, 0)); b1 = B.get((n, e, 1))
            if b0 and b1:
                pa = c0['potAddedPerYear'] + c1['potAddedPerYear']; pb = b0['potAddedPerYear'] + b1['potAddedPerYear']
                line += f' | P_B/P_A {pb/max(1e-300, pa):.2f}（期待 {1/(1-L):.2f}）'
        print(line)
# 全ての国の和（csc は全ての国を出す）：増える走査の充填 = (N−1)×(律速の州の種類 1 の充填)
all_fills = sum((N - 1) * a['throttledFillsCount'] for (n, e, c), a in A.items() if c == 1) / nA
all_rows = sum((N - 1) * a['throttledRowsCount'] for (n, e, c), a in A.items() if c == 1) / nA
c1_fills = sum(a['fillsCount'] for (n, e, c), a in A.items() if c == 1) / nA
c0_fills = sum(a['fillsCount'] for (n, e, c), a in A.items() if c == 0) / nA
print(f'(ii) が増やす走査（全ての国・1 試合あたり）: 行 {all_rows:.3g}・着値の充填 {all_fills:.3g}・増える秒 {all_fills*FILL_US*1e-6:.2f}（表の国の和 {tot_sec:.2f}）　参考＝種類 0 の充填 {c0_fills:.3g}・種類 1 の充填 {c1_fills:.3g}')
