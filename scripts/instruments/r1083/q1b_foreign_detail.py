# (1083) 問い 1：外国 110 行の力の内訳と、O の開幕の大きさを決める入力（L・ΣN・q・W）。
# 使い方: python -X utf8 -I q1b_foreign_detail.py open_rows.tsv > q1b_out.txt
# 式の写し（名乗る）：O_n＝max(0, L−min(W_A,L)−H)・H＝min(L−W, ΣN)・ΣN＝Σ人口K×q（InstrumentsRulePower.cs:159〜178）。ρ_f＝fForeignSum÷fForeignRaw は 1 国だけから受ける行でだけ正しい。
import csv, sys, collections, statistics as st
import sys as _s; _s.stdout.reconfigure(newline='\n')  # 出力は LF
rows = list(csv.DictReader(open(sys.argv[1], encoding='utf-8'), delimiter='\t'))
def f(x):
    try: return float(x)
    except: return float('nan')
nat = {}
for x in rows:
    nat[(x['seed'], x['nation'])] = dict(L=f(x['lNationIndex']), WA=f(x['wNationIndex']), H=f(x['hNationIndex']), O=f(x['oNationIndex']), sh=f(x['oWorldShare']), navy=f(x['navalNationIndex']), q=f(x['qIndex']))
popn = collections.defaultdict(float)
for x in rows: popn[(x['seed'], x['nation'])] += f(x['popK'])
print('[O の入力] 開幕に W>0（前線がある）国の数（10 種の延べ）', sum(1 for v in nat.values() if v['WA'] > 0))
# 種ごとの ΣO÷ΣL と O の国
for s in [str(i) for i in range(1, 11)]:
    ks = [k for k in nat if k[0] == s]
    L = sum(nat[k]['L'] for k in ks); O = sum(nat[k]['O'] for k in ks)
    if s in ('1', '5', '10'): print(f'  種{s} ΣO÷ΣL', round(O / L, 3), 'O>0 の国', sum(1 for k in ks if nat[k]['O'] > 0))
agg = collections.defaultdict(list)
for k, v in nat.items():
    sumN = popn[k] * v['q']
    agg[k[1]].append((v['L'], sumN, v['L'] / sumN if sumN > 0 else float('nan'), v['O'], v['sh']))
print('国ごと（10 種の中央）：L・ΣN・被覆 L÷ΣN・O・O の世界の取り分（開幕）＝O の取り分の上位 10')
top = sorted(agg.items(), key=lambda kv: -st.median(t[4] for t in kv[1]))[:10]
for n, ts in top:
    print(f'  {n}\tL {st.median(t[0] for t in ts):.4g}\tΣN {st.median(t[1] for t in ts):.4g}\t被覆 {st.median(t[2] for t in ts):.3f}\tO {st.median(t[3] for t in ts):.4g}\t取り分 {st.median(t[4] for t in ts):.3f}')
for n in ('清', 'イギリス', 'スペイン', 'オスマン', 'モロッコ', 'ヴェネツィア'):
    ts = agg.get(n)
    if ts: print(f'  （持ち主の側）{n}\tL {st.median(t[0] for t in ts):.4g}\tΣN {st.median(t[1] for t in ts):.4g}\t被覆 {st.median(t[2] for t in ts):.3f}\tO {st.median(t[3] for t in ts):.4g}')

print('\n[外国の行] 州ごと（10 種の中央）：F_S・κF_I・F_U・ΣF_f(ρ²)・ΣF_f の配り前・ρ_持ち主・ρ_f（1 国だけの行）・複数の国から受ける種の数')
D = [x for x in rows if x['bestClass'] == 'foreign' and x['nowBestClass'] != 'ungov' and f(x['fStateIndex']) > 0]
byr = collections.defaultdict(list)
for x in D: byr[(x['nation'], x['region'], x['foreignTopKey'])].append(x)
for (n, r, k), xs in sorted(byr.items(), key=lambda kv: -len(kv[1])):
    fs = st.median(f(x['fStateIndex']) for x in xs); fi = st.median(f(x['kappaIndex']) * f(x['fInterIndex']) for x in xs)
    fu = st.median(f(x['fUngovIndex']) for x in xs); ff = st.median(f(x['fForeignSumIndex']) for x in xs); raw = st.median(f(x['fForeignRawIndex']) for x in xs)
    rho = st.median(f(x['rhoNewIndex']) for x in xs)
    single = [x for x in xs if f(x['fForeignTopIndex']) >= f(x['fForeignSumIndex']) * (1 - 1e-9)]
    rf = st.median(f(x['fForeignSumIndex']) / f(x['fForeignRawIndex']) for x in single) if single else float('nan')
    print(f'  {n}\t{r}\t{k}\t種{len(xs)}\tF_S {fs:.3g}\tκF_I {fi:.3g}\tF_U {fu:.3g}\tF_f {ff:.3g}\t配り前 {raw:.3g}\tρ持ち主 {rho:.3f}\tρ_f {rf:.3f}\t複数 {len(xs) - len(single)}')
