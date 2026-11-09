# (995) c91lc 紙の台本（読むだけ・腕は回さない）：keep_994_rej の c90sl の G-L3 の違反行を、直した式（塵の買い手の払い 0）で読み直す。
# 生は四半期×国の合計だけ（買い手ごとの貯蓄 S_j は無い）＝厳密な再計算はできない。読むのは次の上界：
#  旧の写し＝φ·P（塵の買い手の分を含む）・新の写し＝塵（保有 1e-9 以下）の買い手の分を除く。1 回の売りの塵の買い手は最大 3（私の枠は 4・売り手を除く）・1 人あたり払いの予言は ≦ S_j ≦ 1e-9。
#  ゆえに 旧の写し−実際 ≦ 3e-9×売りの件数（塵だけで説明できる最大）。この上界の内なら〈説明できる〉・外なら〈塵だけでは説明できない〉。
# 使い方: python paper995.py <生のフォルダ（landsale_s<種>.tsv）> <種の起点> [種の数=10]
import sys, os
d = sys.argv[1]; s0 = int(sys.argv[2]); ns = int(sys.argv[3]) if len(sys.argv) > 3 else 10
TOL = 1e-9; DUST = 1e-9; MAXB = 3
def f(x):
    try: return float(x)
    except: return float('nan')
rows = viol = expl = unexpl = 0; worst = 0.0; samples = []
for s in range(s0, s0 + ns):
    with open(os.path.join(d, f'landsale_s{s}.tsv'), encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in fh:
            v = ln.rstrip('\n').split('\t'); rows += 1
            r3 = f(v[ix['salePaidCopyMaxRelDiff']])
            if r3 <= TOL: continue
            viol += 1
            a = f(v[ix['saleActualPaidPerQuarter']]); c = f(v[ix['saleCopyPaidPerQuarter']]); ev = f(v[ix['saleEventCount']])
            gap = c - a; bound = MAXB * DUST * max(ev, 1.0)
            worst = max(worst, gap / bound) if bound > 0 else worst
            if r3 == r3 and 0 <= gap <= bound * (1 + 1e-9): expl += 1
            else:
                unexpl += 1
                if len(samples) < 5: samples.append((s, v[ix['nation']], v[ix['quarter']], a, c, ev, r3))
print(f'行 {rows}・G-L3 違反 {viol}・塵の上界の内 {expl}・外 {unexpl}・(旧の写し−実際)÷上界 の最大 {worst:.3g}')
for x in samples: print('外の例', x)
