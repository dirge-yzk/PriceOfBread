# (1437) 問い2 の名簿の州の要約：<out>/<tag>_q2a.txt の名簿の行（1870 年以後）を型 K・型 C に分け、lastDemand÷want・在庫・輸入・生産・値段の中央と、輸入 0 の割合
import sys, csv, statistics as st
out, tag = sys.argv[1], sys.argv[2]
typ = {}
for r in csv.DictReader(open(f"{out}/q1c_rows.tsv", encoding='utf-8'), delimiter='\t'): typ[(r['seed'], r['region'])] = 'K' if r['tier'] in ('Core', 'Integrated') else 'C'
L = open(f"{out}/{tag}_q2a.txt", encoding='utf-8').read().split('\n')
i = next(k for k, x in enumerate(L) if x.startswith('名簿の州'))
R = {'K': [], 'C': []}
for x in L[i + 1:]:
    c = x.split('\t')
    if len(c) < 12 or int(c[1]) < 1870: continue
    t = typ.get((c[0], c[2]))
    if t and c[4] == '線路': R[t].append([float(v) for v in c[5:12]])
for t, rows in R.items():
    if not rows: continue
    m = lambda j: st.median(r[j] for r in rows)
    print(f"型 {t}（線路・1870 年以後の 10 年ごとの州×種×年 {len(rows)}）：lastDemand÷want の中央 {m(2):.2f}・在庫の中央 {m(3):.3g}・輸入 0 の割合 {sum(1 for r in rows if r[4] == 0) / len(rows):.2f}・生産の中央 {m(5):.3g}・値段の中央 {m(6):.3g}")
