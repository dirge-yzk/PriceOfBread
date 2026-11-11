# (906) c78gz 止める線の記録の段〈名目 GDP が 0 のまま 4 四半期以上続く国〉の読み：m808/gdp_zero10.py の写し（読むだけ・元は変えない）。
#   引数なし（既定）＝旧の台本と同じ出力（旧の既定の件数は変えない）。
#   --founding ＝別の行に〈系列の最初の 4 行から始まる続き〉を出す（旧の台本は i >= 4 の行だけを数えるので、建国から続く 0 は最大で 3 行しか残らず見えない）。
#   使い方: python -X utf8 -I gdp_zero906.py [--founding] 名前=ins ...
import csv, sys, glob, os
sys.stdout.reconfigure(encoding='utf-8')
FOUNDING = '--founding' in sys.argv[1:]
I0 = 0 if FOUNDING else 4      # 旧の既定は 4（系列の最初の 4 行を除く）
for arg in [a for a in sys.argv[1:] if not a.startswith('--')]:
    k, d = arg.split('=', 1)
    runs = []; rows_in_runs = 0
    for f in sorted(glob.glob(os.path.join(d, 'nation_s*.tsv'))):
        s = os.path.basename(f)[8:-4]
        r = csv.reader(open(f, encoding='utf-8'), delimiter='\t'); h = next(r)
        iy, inn, ig, ip = h.index('year'), h.index('nation'), h.index('nominalGdpPerYear'), h.index('populationK')
        iu = h.index('nationUid')
        seq = {}
        for row in r:
            seq.setdefault((row[inn], row[iu]) if FOUNDING else row[inn], []).append(row)   # 906：--founding は国の識別子ごと（旧の既定は名前ごと＝同名の別の国の行がつながる）
        for nm, rs in seq.items():
            if FOUNDING: nm = nm[0]
            cur = []; st = [0]
            def close():
                global rows_in_runs
                if len(cur) >= 4: runs.append((s, nm, cur[0][iy], cur[-1][iy], len(cur), st[0] < 4)); rows_in_runs += len(cur)
            for i, row in enumerate(rs):
                try: g = float(row[ig]); p = float(row[ip])
                except ValueError: g, p = float('nan'), 0.0
                if i >= I0 and p >= 10 and not (g > 0):
                    if not cur: st[0] = i
                    cur.append(row)
                else:
                    close(); cur = []
            close()
    if FOUNDING:
        fr = [x for x in runs if x[5]]
        print(f'{k}: 〈建国から続く 0〉（系列の最初の 4 行から始まる区間・人口 10 千人以上）{len(fr)}・国×四半期 {sum(x[4] for x in fr)}',
              '・'.join(f'種{s} {nm} {a}〜{b} {n}四半期' for s, nm, a, b, n, _ in sorted(fr, key=lambda x: -x[4])[:8]))
    else:
        print(f'{k}: 名目 GDP が正でない人口 10 千人以上の国が 4 四半期以上続く区間 {len(runs)}・国×四半期 {rows_in_runs}',
              '・'.join(f'種{s} {nm} {a}〜{b} {n}四半期' for s, nm, a, b, n, _ in sorted(runs, key=lambda x: -x[4])[:8]))
