# (808) 止める線の記録の段の先頭〈名目 GDP が 0 のまま 4 四半期以上続く国（人口 1 万以上）〉を数える（読むだけ）。
#   m802/gdp_zero.py は人口 100 千人以上・延べの件数だけ＝決まり（人口 1 万＝populationK 10 以上・続く四半期）と合わない（c12sv の反証役の指摘）。
#   数え方：nation_s の国ごとの行の並び（四半期）で、名目 GDP が正でなく人口 10 千人以上の行が 4 行以上続く区間。系列の最初の 4 行（建国の直後）は除く（c12rd の数え方）。
#   使い方: python -X utf8 -I gdp_zero10.py 名前=ins ...
import csv, sys, glob, os
sys.stdout.reconfigure(encoding='utf-8')
for arg in sys.argv[1:]:
    k, d = arg.split('=', 1)
    runs = []; rows_in_runs = 0
    for f in sorted(glob.glob(os.path.join(d, 'nation_s*.tsv'))):
        s = os.path.basename(f)[8:-4]
        r = csv.reader(open(f, encoding='utf-8'), delimiter='\t'); h = next(r)
        iy, inn, ig, ip = h.index('year'), h.index('nation'), h.index('nominalGdpPerYear'), h.index('populationK')
        seq = {}
        for row in r:
            seq.setdefault(row[inn], []).append(row)
        for nm, rs in seq.items():
            cur = []
            def close():
                global rows_in_runs
                if len(cur) >= 4: runs.append((s, nm, cur[0][iy], cur[-1][iy], len(cur))); rows_in_runs += len(cur)
            for i, row in enumerate(rs):
                try: g = float(row[ig]); p = float(row[ip])
                except ValueError: g, p = float('nan'), 0.0
                if i >= 4 and p >= 10 and not (g > 0): cur.append(row)
                else:
                    close(); cur = []
            close()
    print(f'{k}: 名目 GDP が正でない人口 10 千人以上の国が 4 四半期以上続く区間 {len(runs)}・国×四半期 {rows_in_runs}',
          '・'.join(f'種{s} {nm} {a}〜{b} {n}四半期' for s, nm, a, b, n in sorted(runs, key=lambda x: -x[4])[:8]))
