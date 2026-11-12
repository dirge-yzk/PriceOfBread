# (1082) 記録の読み手（門ではない・腕の後に足した＝〈結果の後の記録〉と名乗る）：同じ盤面の新÷旧とヌル腕の幅。
#   使い方: python -X utf8 rec1082.py B=<ins> base=<ins> N1=<ins> N2=<ins>
#   ①清・ロシアの人口（千人・nation_s の populationK・その年の最初の行・種の中央）1850・1950
#   ②清の 3 州（アイグン・ハミ・ホータン）とカシュガルの pressureIndex の全期間の平均（10 種の平均）
#   ③全ての州の離脱の発火（secession_s の goFlag=1 の行）の合計
import sys, os, csv, statistics as st
csv.field_size_limit(1 << 30)
arms = [a.split('=', 1) for a in sys.argv[1:]]
REG = ["アイグン", "ハミ", "ホータン", "カシュガル"]
out = {}
for name, ins in arms:
    pop = {}; pr = {r: [] for r in REG}; fires = 0
    for s in range(1, 11):
        p = os.path.join(ins, f"nation_s{s}.tsv")
        seen = set()
        with open(p, encoding='utf-8') as f:
            rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
            for r in rd:
                y = int(r[ix['year']]); n = r[ix['nation']]
                if y in (1850, 1950) and n in ("清", "ロシア") and (y, n) not in seen:
                    seen.add((y, n)); pop.setdefault((n, y), []).append(float(r[ix['populationK']]))
        p = os.path.join(ins, f"secession_s{s}.tsv")
        acc = {r: [] for r in REG}
        with open(p, encoding='utf-8') as f:
            rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
            for r in rd:
                if r[ix['goFlag']] == "1": fires += 1
                if r[ix['nation']] == "清" and r[ix['region']] in acc: acc[r[ix['region']]].append(float(r[ix['pressureIndex']]))
        for k in REG:
            if acc[k]: pr[k].append(sum(acc[k]) / len(acc[k]))
    out[name] = (pop, pr, fires)
    print(f"{name}：" + "・".join(f"{n}{y} {st.median(v):.0f}" for (n, y), v in sorted(pop.items())) + "・圧力の平均 " + "・".join(f"{k} {st.mean(v):.4g}" for k, v in pr.items() if v) + f"・発火 {fires}")
if 'B' in out and 'base' in out:
    pb, rb, fb = out['B']; p0, r0, f0 = out['base']
    print("新÷旧（B÷base）：" + "・".join(f"{n}{y} {st.median(pb[(n, y)]) / st.median(p0[(n, y)]):.4f}" for (n, y) in sorted(pb)) + "・"
          + "・".join(f"圧力 {k} {(st.mean(rb[k]) / st.mean(r0[k])) if r0[k] and st.mean(r0[k]) > 0 else float('nan'):.3g}" for k in REG))
