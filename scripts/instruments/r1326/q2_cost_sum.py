# (1326) 問い2：q2_cost.tsv の集計（電灯網の投入費÷電化インフラの値段・内訳の割合）
import sys,statistics as st,collections
rows=[l.rstrip('\n').split('\t') for l in open(sys.argv[1],encoding='utf-8') if l.startswith('s')]
by=collections.defaultdict(list)
for r in rows:
    y=r[1]; P=float(r[3]); el=float(r[4]); gl=float(r[5]); parts=[float(x) for x in r[6].split(',')]
    by[y].append((el/P, gl/P, [p/el for p in parts]))
for y in sorted(by):
    a=by[y]; n=len(a)
    print(f'{y} n={n} 電灯網費÷P 中央={st.median(x[0] for x in a):.3g} (<1 の行={sum(x[0]<1 for x in a)}) ガス灯費÷P 中央={st.median(x[1] for x in a):.3g} (<1 の行={sum(x[1]<1 for x in a)}) 内訳の割合の中央 送電網={st.median(x[2][0] for x in a):.2f} 電化製品={st.median(x[2][1] for x in a):.2f} 鋼={st.median(x[2][2] for x in a):.2f} 労={st.median(x[2][3] for x in a):.2f} 高労={st.median(x[2][4] for x in a):.2f}')
