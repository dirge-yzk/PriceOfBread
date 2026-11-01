"""(754) c4pl：事前登録 m754 の門 P1・P2・P3 を計器 portch_s から読む（読むだけ）。
python -X utf8 gate754.py <計器の出力ディレクトリ(ins)> [種...]
  P1 ＝全ての行で portUsed==portLand かつ railUsedKm==landNewKm（相対 1e-8 以内・計器の値は G10＝有効 10 桁）。外れた行の数（0 で通る）。
  P2 ＝種ごとの〈年あたり Σ_内陸州(1−railDecayUsedIndex÷railDecayOldIndex)〉（行のある年の平均）の種の中央値（≧0.40 で通る）。
  P3 ＝種ごとの〈年あたり portUsed≠portGc の州の数〉（行のある年の平均）の種の中央値（≧3 で通る）。
  記録＝海盆が変わる州×年の年平均・新÷旧の分布（中央値・最小・0.99 未満の割合）・国ごとの港が変わる州（年平均）。
列は見出しの名前で読む（列の数で行を捨てない）。portch_s の無い出力（旧の規則の基線）は何も出さない。"""
import sys, os, statistics as st, collections
d = sys.argv[1]
seeds = [int(x) for x in sys.argv[2:]] or sorted(int(f[len('portch_s'):-4]) for f in os.listdir(d) if f.startswith('portch_s') and f.endswith('.tsv'))
if not seeds:
    print('portch_s なし'); sys.exit(0)
def num(x):
    try: return float(x)
    except ValueError: return float('nan')
bad_total = rows_total = 0
P2s, P3s, BAS = [], [], []
byNat = collections.defaultdict(list)
allRatios = []
for sd in seeds:
    path = os.path.join(d, f'portch_s{sd}.tsv')
    with open(path, encoding='utf-8') as fh:
        head = fh.readline().rstrip('\n').split('\t'); ix = {h: i for i, h in enumerate(head)}
        sumLoss = collections.defaultdict(float); chg = collections.Counter(); bas = collections.Counter(); yrs = set()
        natChg = collections.Counter()
        bad = 0; n = 0
        for ln in fh:
            c = ln.rstrip('\n').split('\t')
            if len(c) < len(head): continue
            n += 1
            y = int(c[ix['year']]); yrs.add(y)
            pu, pl, pg = c[ix['portUsed']], c[ix['portLand']], c[ix['portGc']]
            ru, ln_ = num(c[ix['railUsedKm']]), num(c[ix['landNewKm']])
            ok = pu == pl and abs(ru - ln_) <= 1e-8 * max(1.0, abs(ln_))
            if not ok: bad += 1
            do, du = num(c[ix['railDecayOldIndex']]), num(c[ix['railDecayUsedIndex']])
            if do > 0:
                r = du / do; sumLoss[y] += 1 - r; allRatios.append(r)
            if pu != pg: chg[y] += 1; natChg[c[ix['nation']]] += 1
            if c[ix['basinOld']] != c[ix['basinNew']]: bas[y] += 1
        ny = max(1, len(yrs))
        P2 = sum(sumLoss.values()) / ny; P3 = sum(chg.values()) / ny; B = sum(bas.values()) / ny
        P2s.append(P2); P3s.append(P3); BAS.append(B); bad_total += bad; rows_total += n
        for k, v in natChg.items(): byNat[k].append(v / ny)
        print(f'seed {sd}\trows={n}\tyears={len(yrs)}\tP1外れ={bad}\tP2={P2:.3f}\tP3={P3:.2f}\t海盆が変わる州/年={B:.2f}')
mP2, mP3 = st.median(P2s), st.median(P3s)
print(f'P1 外れた行 {bad_total} / {rows_total}  -> {"通る" if bad_total == 0 and rows_total > 0 else "落ちる"}')
print(f'P2 種の中央値 {mP2:.3f} (>=0.40) -> {"通る" if mP2 >= 0.40 else "落ちる"}')
print(f'P3 種の中央値 {mP3:.2f} (>=3) -> {"通る" if mP3 >= 3 else "落ちる"}')
if allRatios:
    s = sorted(allRatios)
    print(f'記録：鉄道の減衰の新÷旧 中央値 {st.median(s):.4f} 最小 {s[0]:.4f} 0.99未満の割合 {sum(1 for x in s if x < 0.99) / len(s):.3f}　海盆が変わる州/年（種の中央値）{st.median(BAS):.2f}')
    top = sorted(byNat.items(), key=lambda kv: -st.mean(kv[1]))[:8]
    print('記録：港が変わる州/年（国・種の平均）' + '　'.join(f'{k} {st.mean(v):.2f}' for k, v in top))
