# -*- coding: utf-8 -*-
# (1051) c99gr 設計の材料：穀物法 C-1 の最小の段の紙の台本（読むだけ・腕 0・エンジンを呼ばない）。
# 呼び方：python -I -X utf8 c1paper1051.py <出力ファイル> <腕のフォルダ>...（例 D:/PoB_work/keep_1044/base_ins D:/PoB_work/keep_1044/n1 D:/PoB_work/keep_1044/n2）
#
# ---- 事前に決めた物（数を見る前に書いた・⑫）----
# 予言の式（AI の案・確証なし）：
#   x̄＝E÷(I＋E)（E＝grainExportValue・I＝grainImportValue＝cornint_s の穀物の客体〔主食＋麦の連鎖〕の越境の額・清算 1 回分）。I＋E≦0 は x̄＝0（貿易なし＝今の値のまま）。
#   乙（走査なし）：pred0＝cornPrefNow0×(1−x̄)。
#   甲′（産地の走査あり・財ごとの流れが四半期の表に無いので穀物の合計の x̄ で代える近似）：pred0＝cornPrefNow0×g0×(1−x̄)・g0＝Σ_穀物 cfRentProd0_g÷cfRentAll0（corncf_s）。
#   旧（旗 0）：cornPrefNow0（＝RentProtectionWeightCoef×Rent[0]・係数 1.0）。
# 母集合 P：cornPrefNow0 が有限かつ＞0・grainImportValue と grainExportValue が有限の国×四半期。
# 輸出の行 X：x̄≧0.5。輸出でない行 N：x̄＜0.5（貿易なし x̄＝0 を含む・別に数える）。
# 窓：W0 1783〜1950 の全て・W1 1815〜1846（英の穀物法の時期）・W2 1870〜1913（穀物の侵入の時期）。窓はこの 3 つだけ（結果を見て足さない）。
# 主な国：名の末尾が イギリス・プロイセン・アメリカ・ロシア・フランス・オーストリア（政体の接頭語つきの名も同じ国に数える）。
# 門の素案の統計：
#   G1（働いた・下側）：X の行の pred0÷旧 の中央（旗 0 は 1）と、中央(N の pred0)−中央(X の pred0) の差の、旧の同じ差からの増分。
#   G2（形に固有の誤り・上側）：N のうち貿易なしの行で、乙の pred0＝旧 が 1e-12 以内の割合（乙は構造で 1・甲′ は g0 で動く）。
#   G3（輸出の 0・両側）：X かつ穀物法が限定以上（cornLawsCode≧1）の行の件数と、pred0＜0.05 の割合。
#   振れ（記録）：国×種の系列で X の出入りの回数 回/世紀。
#   配線の門の母集合：x̄＞0 の行の件数（旗 0 では実際＝旧≠期待になる行＝旗 0 で落ちる側の件数）。
#   法の動き（記録・創発）：穀物法が限定以上の行のうち、同じ国×種の系列で次の 4 行（4 四半期）以内に段が下がった割合を X と N で。段の上下の反転（上げの後に下げ・下げの後に上げ）回/世紀。
#   薄い貿易（記録）：0＜I＋E＜0.01×grainProducedValue の行の割合と、そのうち X の割合（小さな流れで x̄ が 0 と 1 の間を跳ぶ所）。
# 計算量（甲の産地の走査）：landshare_s の rgoNullCount＋rgoFieldCount の国×四半期の平均（基線のみ）。
import sys, os, csv, statistics as st

MAJ = ['イギリス', 'プロイセン', 'アメリカ', 'ロシア', 'フランス', 'オーストリア']
WIN = {'W0': (1783, 1950), 'W1': (1815, 1846), 'W2': (1870, 1913)}
GR = [15, 42, 54, 93, 98, 105, 106]


def fnum(s):
    try:
        v = float(s)
    except Exception:
        return float('nan')
    return v


def rd(path):
    with open(path, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t')
        h = next(r)
        ix = {k: i for i, k in enumerate(h)}
        for row in r:
            yield ix, row


def med(a):
    return st.median(a) if a else float('nan')


def q(a, p):
    if not a:
        return float('nan')
    b = sorted(a)
    return b[min(len(b) - 1, int(p * (len(b) - 1) + 0.5))]


def arm(d):
    rows = []
    flips = []
    rgo = []
    for s in range(1, 11):
        cfp = os.path.join(d, f'corncf_s{s}.tsv')
        cip = os.path.join(d, f'cornint_s{s}.tsv')
        if not (os.path.exists(cfp) and os.path.exists(cip)):
            continue
        g0 = {}
        for ix, r in rd(cfp):
            ra = fnum(r[ix['cfRentAll0']])
            gs = sum(fnum(r[ix[f'cfRentProd0_g{g}']]) for g in GR)
            g0[(r[ix['quarter']], r[ix['nation']])] = gs / ra if ra > 0 else float('nan')
        ser = {}
        for ix, r in rd(cip):
            yr = int(r[ix['year']]); nat = r[ix['nation']]
            old = fnum(r[ix['cornPrefNow0']]); I = fnum(r[ix['grainImportValue']]); E = fnum(r[ix['grainExportValue']])
            law = int(fnum(r[ix['cornLawsCode']])) if r[ix['cornLawsCode']] not in ('', 'nan') else -1
            if not (old == old and old > 0 and I == I and E == E):
                continue
            notrade = not (I + E > 0)
            x = 0.0 if notrade else E / (I + E)
            g = g0.get((r[ix['quarter']], nat), float('nan'))
            Y = fnum(r[ix['grainProducedValue']])
            thin = (not notrade) and Y == Y and (I + E) < 0.01 * Y
            t = [s, yr, nat, law, old, x, notrade, g, thin, None]
            rows.append(t)
            ser.setdefault(nat, []).append(t)
        for nat, v in ser.items():
            for i, t in enumerate(v):
                if t[3] >= 1:
                    t[9] = any(u[3] >= 0 and u[3] < t[3] for u in v[i + 1:i + 5])
            if len(v) < 8:
                continue
            n = sum(1 for a, b in zip(v, v[1:]) if (a[5] >= 0.5) != (b[5] >= 0.5))
            yrs = max(1.0, (v[-1][1] - v[0][1] + 1) / 100.0)
            steps = [b[3] - a[3] for a, b in zip(v, v[1:]) if a[3] >= 0 and b[3] >= 0 and b[3] != a[3]]
            rev = sum(1 for a, b in zip(steps, steps[1:]) if (a > 0) != (b > 0))
            flips.append((n / yrs, rev / yrs))
        lsp = os.path.join(d, f'landshare_s{s}.tsv')
        if os.path.exists(lsp) and s <= 3:
            for ix, r in rd(lsp):
                rgo.append(fnum(r[ix['rgoNullCount']]) + fnum(r[ix['rgoFieldCount']]))
    return rows, flips, rgo


def summ(out, name, rows, flips, rgo):
    out.append(f'== 腕 {name}  P の行 {len(rows)}')
    for wn, (a, b) in WIN.items():
        R = [t for t in rows if a <= t[1] <= b]
        X = [t for t in R if t[5] >= 0.5]; N = [t for t in R if t[5] < 0.5]; NT = [t for t in N if t[6]]
        if not R:
            continue
        oldX = [t[4] for t in X]; oldN = [t[4] for t in N]
        yiX = [t[4] * (1 - t[5]) for t in X]; yiN = [t[4] * (1 - t[5]) for t in N]
        koX = [t[4] * t[7] * (1 - t[5]) for t in X if t[7] == t[7]]; koN = [t[4] * t[7] * (1 - t[5]) for t in N if t[7] == t[7]]
        ratX = [(1 - t[5]) for t in X]
        dOld = med(oldN) - med(oldX); dYi = med(yiN) - med(yiX); dKo = med(koN) - med(koX)
        out.append(f'  {wn} 行 {len(R)}  X {len(X)} ({len(X)/len(R):.3f})  N {len(N)}  貿易なし {len(NT)} ({len(NT)/len(R):.3f})  x̄ の中央 {med([t[5] for t in R]):.3f} p90 {q([t[5] for t in R],0.9):.3f}')
        out.append(f'     旧 中央 N {med(oldN):.4f} X {med(oldX):.4f} 差 {dOld:.4f} ｜乙 N {med(yiN):.4f} X {med(yiX):.4f} 差 {dYi:.4f} 増分 {dYi-dOld:.4f} ｜甲′ N {med(koN):.4f} X {med(koX):.4f} 差 {dKo:.4f}')
        out.append(f'     G1 X の乙÷旧 中央 {med(ratX):.4f} p90 {q(ratX,0.9):.4f}  ｜g0（旧支配層の地代のうち穀物）中央 {med([t[7] for t in R if t[7]==t[7]]):.4f} p10 {q([t[7] for t in R if t[7]==t[7]],0.1):.4f} p90 {q([t[7] for t in R if t[7]==t[7]],0.9):.4f}')
        G3 = [t for t in X if t[3] >= 1]
        out.append(f'     G3 X かつ限定以上 {len(G3)}  乙＜0.05 の割合 {(sum(1 for t in G3 if t[4]*(1-t[5])<0.05)/len(G3)) if G3 else float("nan"):.3f}  ｜G2 貿易なしの乙＝旧 {(sum(1 for t in NT if abs(t[4]*(1-t[5])-t[4])<=1e-12)/len(NT)) if NT else float("nan"):.4f}')
        dX = [t[9] for t in X if t[9] is not None]; dN = [t[9] for t in N if t[9] is not None]
        out.append(f'     配線 x̄＞0 の行 {sum(1 for t in R if t[5]>0)}  ｜限定以上で 4 四半期内に下げ X {(sum(dX)/len(dX)) if dX else float("nan"):.3f}（{len(dX)}） N {(sum(dN)/len(dN)) if dN else float("nan"):.3f}（{len(dN)}）')
        TH = [t for t in R if t[8]]
        out.append(f'     薄い貿易 {len(TH)/len(R):.3f}  そのうち X {(sum(1 for t in TH if t[5]>=0.5)/len(TH)) if TH else float("nan"):.3f}  ｜X のうち薄い {(sum(1 for t in X if t[8])/len(X)) if X else float("nan"):.3f}')
        lc = [sum(1 for t in R if t[3] == k) / len(R) for k in (0, 1, 2)]
        out.append(f'     穀物法 なし {lc[0]:.3f} 限定 {lc[1]:.3f} 全面 {lc[2]:.3f}  ｜X の中の なし {(sum(1 for t in X if t[3]==0)/len(X)) if X else float("nan"):.3f}')
        for m in MAJ:
            M = [t for t in R if t[2].endswith(m)]
            if not M:
                continue
            out.append(f'     {m}: 行 {len(M)} X の割合 {sum(1 for t in M if t[5]>=0.5)/len(M):.3f} 貿易なし {sum(1 for t in M if t[6])/len(M):.3f} x̄ 中央 {med([t[5] for t in M]):.3f} 旧 {med([t[4] for t in M]):.3f} 乙 {med([t[4]*(1-t[5]) for t in M]):.3f} g0 {med([t[7] for t in M if t[7]==t[7]]):.3f} 限定以上 {sum(1 for t in M if t[3]>=1)/len(M):.3f}')
    fx = [a for a, _ in flips]; fr = [b for _, b in flips]
    out.append(f'  振れ X の出入り 回/世紀 中央 {med(fx):.2f} p90 {q(fx,0.9):.2f} 最大 {max(fx) if fx else float("nan"):.2f}（系列 {len(fx)}）｜穀物法の段の反転 回/世紀 中央 {med(fr):.2f} p90 {q(fr,0.9):.2f} 最大 {max(fr) if fr else float("nan"):.2f}')
    if rgo:
        out.append(f'  RGO の数（国×四半期・種 1〜3）平均 {sum(rgo)/len(rgo):.1f} 中央 {med(rgo):.0f} p90 {q(rgo,0.9):.0f}')


def main():
    outp = sys.argv[1]
    out = ['# c1paper1051（読むだけ）・入力 ' + ' '.join(sys.argv[2:])]
    for d in sys.argv[2:]:
        rows, flips, rgo = arm(d)
        summ(out, os.path.basename(d.rstrip('/')), rows, flips, rgo)
    with open(outp, 'wb') as f:
        f.write(('\n'.join(out) + '\n').encode('utf-8'))


main()
