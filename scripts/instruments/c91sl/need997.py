# (997) c91sl 必要性の確認（読むだけ・腕の前）：keep_994_rej の c90sl 腕の生で G-L3 の違反行（salePaidCopyMaxRelDiff > 1e-9）が
# 〈保有の床 held ≦ 1e-9 の塵の買い手を Pop.Take が拒む〉だけで説明できるかを、行ごとの向きと大きさで読む。
#   塵の買い手 1 人の払いの欠け＝min(φ·S_j/ΣS·P, S_j) ≦ S_j ≦ 1e-9＝行の欠け d＝写し−実際 は 0 以上で、売りの件数×(買い手の数)×1e-9 以下。
#   向きが逆（実際＞写し）の行、または d/売りの件数 が 1e-9×(州の POP の数の上限 MAXB) を超える行は〈塵の外〉。
# 使い方: python need997.py <生のフォルダ> [種の数=10] [種の起点=1] [MAXB=8]
import sys, os
d0 = sys.argv[1]; ns = int(sys.argv[2]) if len(sys.argv) > 2 else 10; s0 = int(sys.argv[3]) if len(sys.argv) > 3 else 1
MAXB = float(sys.argv[4]) if len(sys.argv) > 4 else 8.0
TOL = 1e-9; FLT = 1e-13   # 和の順の丸めの床（相対）
def f(x):
    try: return float(x)
    except: return float('nan')
viol = 0; neg = 0; over = 0; nonfinite = 0; dmax = 0.0; dpe_max = 0.0; zero_abs = 0; pos_abs = 0; ev_v = 0.0; rows = 0
neg_rows = []; over_rows = []
for s in range(s0, s0 + ns):
    with open(os.path.join(d0, f'landsale_s{s}.tsv'), encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in fh:
            v = ln.rstrip('\n').split('\t'); rows += 1
            r3 = f(v[ix['salePaidCopyMaxRelDiff']])
            if r3 <= TOL: continue
            viol += 1
            if not (r3 == r3) or r3 == float('inf'): nonfinite += 1; continue
            a = f(v[ix['saleActualPaidPerQuarter']]); c = f(v[ix['saleCopyPaidPerQuarter']]); ev = f(v[ix['saleEventCount']]); ev_v += ev
            d = c - a
            if d < -FLT * max(abs(a), abs(c)): neg += 1; neg_rows.append((s, v[ix['nation']], v[0], a, c)); continue
            if d > FLT * max(abs(a), abs(c)): pos_abs += 1
            else: zero_abs += 1
            dpe = d / max(ev, 1.0)   # 売りの件数は払いが立った売りだけ（全ての買い手が塵の売りは 0 件）＝少なくとも 1 件
            dmax = max(dmax, d); dpe_max = max(dpe_max, dpe)
            if not (dpe <= MAXB * 1e-9): over += 1; over_rows.append((s, v[ix['nation']], v[0], a, c, ev))
print(f'# need997 {d0}・種 {s0}〜{s0+ns-1}・行 {rows}')
print(f'G-L3 の違反行 {viol}（非有限 {nonfinite}）・その行の売りの件数 {ev_v:.0f}')
print(f'  向き：写し＞実際 {pos_abs} 行・差が丸めの内 {zero_abs} 行・実際＞写し（塵で説明できない）{neg} 行')
print(f'  大きさ：d の最大 {dmax:.3g}・d÷max(売りの件数,1) の最大 {dpe_max:.3g}（上限 {MAXB:.0f}×1e-9 を超える行 {over}）')
for x in neg_rows[:10]: print('  逆向き', x)
for x in over_rows[:10]: print('  大きすぎ', x)
print('判定:', '塵の外の行なし' if neg == 0 and over == 0 and nonfinite == 0 else '塵の外の行あり')
