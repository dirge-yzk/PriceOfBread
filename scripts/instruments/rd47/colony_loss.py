"""(698) rd47：1 国の植民地（populationK−homePopulationK・stock_s の年末の行）が開幕の半分を最初に割った年を、腕ごと・種ごとに出す（読むだけ）。
使い方：python -X utf8 -I colony_loss.py <国の名> <腕名>=<計器の出力フォルダ>[:<種の起点>] ...
限界：stock_s は国の名で引く（改名で途切れる）・4 四半期ごとの断面・植民地は人口の差で数える（州の格の替わりも含む）。
"""
import csv, sys, statistics
csv.field_size_limit(10**9)
nat = sys.argv[1]
for a in sys.argv[2:]:
    name, rest = a.split('=', 1)
    d, s0 = (rest.rsplit(':', 1) if ':' in rest[2:] else (rest, '1'))
    out = []
    for s in range(int(s0), int(s0) + 10):
        c0 = None; hit = None
        with open(f'{d}/stock_s{s}.tsv', encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
            for x in r:
                if len(x) != len(h) or x[ix['nation']] != nat: continue
                try: col = float(x[ix['populationK']]) - float(x[ix['homePopulationK']])
                except ValueError: continue
                if c0 is None: c0 = col
                elif hit is None and col < 0.5 * c0: hit = int(x[ix['year']])
        out.append(hit)
    ys = [y for y in out if y]
    print(f"{name}\t" + ' '.join(f's{int(s0)+i}:{y or "—"}' for i, y in enumerate(out)) + f"\t割った種 {len(ys)}/10・1820 年までに {sum(1 for y in ys if y <= 1820)}・中央 {statistics.median(ys) if ys else '—'}")
