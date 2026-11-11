# (887) c75ip（(876) c73ip の fw876.py の型・読む州と種と生の場所を引数にした）記録の台本：1 つの州の人口の軌道（10 年ごと・ピーク・持ち主）を、旗 0 と腕で種ごとに並べる。
#   python -I -X utf8 fw887.py <州の名> <種の起点> <種の終わり> <名>=<ins フォルダ> ...
#   region_s の populationK と nation を読む（有限の行だけ）。持ち主の交代の年（nation が変わった最初の年）も出す。読むだけ・エンジンの関数を呼ばない。
import sys, os
reg = sys.argv[1]; s0 = int(sys.argv[2]); s1 = int(sys.argv[3])
arms = [a.split('=', 1) for a in sys.argv[4:]]
for s in range(s0, s1 + 1):
    for tag, d in arms:
        p = os.path.join(d, f'region_s{s}.tsv')
        if not os.path.exists(p): print(f'種 {s}\t{tag}\t表なし'); continue
        ys = {}
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for line in f:
                x = line.rstrip('\n').split('\t')
                if x[ix['region']] != reg: continue
                try: v = float(x[ix['populationK']])
                except ValueError: continue
                if v != v: continue
                ys[int(x[ix['year']])] = (v, x[ix['nation']])
        if not ys: print(f'種 {s}\t{tag}\t行なし'); continue
        pk = max(ys.items(), key=lambda t: t[1][0])
        last = max(ys); mn_after = min((ys[y][0] for y in ys if y >= pk[0]), default=float('nan'))
        own = []; prev = None
        for y in sorted(ys):
            if ys[y][1] != prev: own.append(f'{y}:{ys[y][1]}'); prev = ys[y][1]
        sel = [y for y in sorted(ys) if y % 10 == 0 or y == last]
        print(f'種 {s}\t{tag}\tピーク {pk[1][0]:.2f}({pk[0]}・{pk[1][1]})\tピークの後の最小 {mn_after:.2f}（比 {mn_after / pk[1][0] if pk[1][0] > 0 else float("nan"):.3f}）\t持ち主 {" ".join(own)}\t' + ' '.join(f'{y}:{ys[y][0]:.1f}' for y in sel if y >= 1800))
