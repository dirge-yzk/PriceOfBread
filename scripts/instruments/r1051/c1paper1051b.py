# -*- coding: utf-8 -*-
# (1051) c99gr 紙の台本 2：乙の振れ（X の出入り 中央 約 65 回/世紀）を見た後に、同じ段の〈量で割る置き方〉丙を同じ生で数える（読むだけ）。
# 呼び方：python -I -X utf8 c1paper1051b.py <出力ファイル> <腕のフォルダ>...
# ⚠この台本は c1paper1051.py の結果（乙の振れ）を見た後に書いた＝丙は〈結果の後に足した置き方〉と名乗る（⑫：母集合と窓は 1 本目と同じ・広げない）。
# ---- 事前に決めた物（この台本の数を見る前に書いた）----
#   乙：f＝1−x̄・x̄＝E÷(I＋E)（I＋E≦0 は 1）。
#   丙：f＝1−max(0, E−I)÷(Y＋I)（Y＝grainProducedValue・Y＋I≦0 は 1）＝純輸出の量を国内の供給（産出＋輸入）で割る。
#   滑らかさ：国×種の系列（cornint_s の行の順）で連続する行の |Δf| の平均。系列ごとの値の中央と p90。
#   〈半分を割る〉の出入り：f が 0.5 を跨いだ回数 回/世紀（系列ごと）の中央と p90。
#   乙の X（x̄≧0.5）の行での 丙 の f の中央と p90。全ての行で f≦0.5 の割合（乙・丙）。
#   主な国×窓の f の中央（乙・丙）。母集合 P・窓 W0〜W2・主な国は 1 本目と同じ。
import sys, os, csv, statistics as st

MAJ = ['イギリス', 'プロイセン', 'アメリカ', 'ロシア', 'フランス', 'オーストリア']
WIN = {'W0': (1783, 1950), 'W1': (1815, 1846), 'W2': (1870, 1913)}


def fnum(s):
    try:
        return float(s)
    except Exception:
        return float('nan')


def med(a):
    return st.median(a) if a else float('nan')


def q(a, p):
    if not a:
        return float('nan')
    b = sorted(a)
    return b[min(len(b) - 1, int(p * (len(b) - 1) + 0.5))]


def arm(d):
    rows, ser_stats = [], []
    for s in range(1, 11):
        p = os.path.join(d, f'cornint_s{s}.tsv')
        if not os.path.exists(p):
            continue
        ser = {}
        with open(p, encoding='utf-8', newline='') as fh:
            r = csv.reader(fh, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
            for row in r:
                old = fnum(row[ix['cornPrefNow0']]); I = fnum(row[ix['grainImportValue']]); E = fnum(row[ix['grainExportValue']]); Y = fnum(row[ix['grainProducedValue']])
                if not (old == old and old > 0 and I == I and E == E):
                    continue
                yr = int(row[ix['year']]); nat = row[ix['nation']]
                fy = 1.0 if not (I + E > 0) else 1.0 - E / (I + E)
                den = (Y if Y == Y else 0.0) + I
                fh_ = 1.0 if not (den > 0) else 1.0 - max(0.0, E - I) / den
                t = (yr, nat, fy, fh_, E / (I + E) if I + E > 0 else 0.0)
                rows.append(t); ser.setdefault(nat, []).append(t)
        for nat, v in ser.items():
            if len(v) < 8:
                continue
            yrs = max(1.0, (v[-1][0] - v[0][0] + 1) / 100.0)
            dy = sum(abs(b[2] - a[2]) for a, b in zip(v, v[1:])) / (len(v) - 1)
            dh = sum(abs(b[3] - a[3]) for a, b in zip(v, v[1:])) / (len(v) - 1)
            cy = sum(1 for a, b in zip(v, v[1:]) if (a[2] > 0.5) != (b[2] > 0.5)) / yrs
            ch = sum(1 for a, b in zip(v, v[1:]) if (a[3] > 0.5) != (b[3] > 0.5)) / yrs
            ser_stats.append((dy, dh, cy, ch))
    return rows, ser_stats


def main():
    out = ['# c1paper1051b（読むだけ・乙の振れを見た後に足した丙）・入力 ' + ' '.join(sys.argv[2:])]
    for d in sys.argv[2:]:
        rows, ss = arm(d)
        out.append(f'== 腕 {os.path.basename(d.rstrip("/"))}  行 {len(rows)}  系列 {len(ss)}')
        out.append(f'  |Δf| 平均（系列の中央 / p90）乙 {med([a[0] for a in ss]):.4f} / {q([a[0] for a in ss],0.9):.4f}  丙 {med([a[1] for a in ss]):.4f} / {q([a[1] for a in ss],0.9):.4f}')
        out.append(f'  0.5 を跨ぐ 回/世紀（中央 / p90 / 最大）乙 {med([a[2] for a in ss]):.2f} / {q([a[2] for a in ss],0.9):.2f} / {max(a[2] for a in ss):.2f}  丙 {med([a[3] for a in ss]):.2f} / {q([a[3] for a in ss],0.9):.2f} / {max(a[3] for a in ss):.2f}')
        for wn, (a, b) in WIN.items():
            R = [t for t in rows if a <= t[0] <= b]
            X = [t for t in R if t[4] >= 0.5]
            out.append(f'  {wn} f≦0.5 の割合 乙 {sum(1 for t in R if t[2]<=0.5)/len(R):.3f} 丙 {sum(1 for t in R if t[3]<=0.5)/len(R):.3f} ｜乙の X の行の丙 f 中央 {med([t[3] for t in X]):.4f} p10 {q([t[3] for t in X],0.1):.4f}')
            for m in MAJ:
                M = [t for t in R if t[1].endswith(m)]
                if M:
                    out.append(f'     {m}: f 中央 乙 {med([t[2] for t in M]):.3f} 丙 {med([t[3] for t in M]):.3f} 丙 p10 {q([t[3] for t in M],0.1):.3f}')
    with open(sys.argv[1], 'wb') as f:
        f.write(('\n'.join(out) + '\n').encode('utf-8'))


main()
