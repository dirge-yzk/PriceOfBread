"""(770) c6pr：事前登録 m770 の門 P1・P2・P3・U1・U2 を計器 portch_s から読む（読むだけ）。
python -X utf8 gate770.py <計器の出力ディレクトリ(ins)> [種...]
  P1 ＝全ての行で portUsed==portNew かつ railUsedKm==landKmNew（相対 1e-8 以内）。外れた行の数（0 で通る）。配線しか拾わない。
  P2 ＝種ごとの〈年あたり Σ_州(1−railDecayUsedIndex÷railDecayOldIndex)〉（行のある年の平均）の種の中央値（≧0.231 で通る）。
  P3 ＝種ごとの〈年あたり portUsed≠portGc の州の数〉（行のある年の平均）の種の中央値（≧2.67 で通る）。
  U1 ＝全ての行で landKmNew ≦ landKmOld+1e-9。違反の行の数（0 で通る）。
  U2 ＝portUsed≠portGc の行のうち landKmNew ＞ 2×gcKm の行の割合（≦0.15 で通る）。母集団 30 行未満の種は種ごとの判定から外し合算で読む。
  記録＝海盆が変わる州×年の年平均・railDecay の新÷旧の分布・fareLand の新÷旧の分布・国ごとの港が変わる州（年平均）。
列は見出しの名前で読む（列の数で行を捨てない）。portch_s の無い出力は何も出さない。
(792) portch_s の列 portNew は (792) で消えた（portlm_s の portA と 10 種 202,943 行で全行一致）。(798) c11dm：portNew が無い出力では、同じ種の portlm_s の portA を（year,nation,region）で突き合わせて portNew の代わりに読む（portlm_s も無ければ P1 の港の側は読めない＝その旨を出して終わる）。"""
import sys, os, statistics as st, collections
d = sys.argv[1]
seeds = [int(x) for x in sys.argv[2:]] or sorted(int(f[len('portch_s'):-4]) for f in os.listdir(d) if f.startswith('portch_s') and f.endswith('.tsv'))
if not seeds:
    print('portch_s なし'); sys.exit(0)
def num(x):
    if x == 'inf': return float('inf')
    try: return float(x)
    except ValueError: return float('nan')
P2_MIN, P3_MIN, U2_MAX = 0.231, 2.67, 0.15
bad_total = rows_total = u1_total = 0
chg_total = u2_total = 0
P2s, P3s, BAS = [], [], []
byNat = collections.defaultdict(list)
dec, fare = [], []
for sd in seeds:
    path = os.path.join(d, f'portch_s{sd}.tsv')
    with open(path, encoding='utf-8') as fh:
        head = fh.readline().rstrip('\n').split('\t'); ix = {h: i for i, h in enumerate(head)}
        sumLoss = collections.defaultdict(float); chg = collections.Counter(); bas = collections.Counter(); yrs = set()
        natChg = collections.Counter()
        bad = u1 = n = nchg = nu2 = 0
        portA = None   # (798) portNew の列が無いとき：key=(year,nation,region) -> [portA,...]（行の順）
        if 'portNew' not in ix:
            lmPath = os.path.join(d, f'portlm_s{sd}.tsv')
            if not os.path.exists(lmPath):
                print(f'seed {sd}\tportNew も portlm_s も無い＝P1 の港の側は読めない'); continue
            portA = collections.defaultdict(list)
            with open(lmPath, encoding='utf-8') as lf:
                lh = lf.readline().rstrip('\n').split('\t'); lx = {h: i for i, h in enumerate(lh)}
                for lln in lf:
                    lc = lln.rstrip('\n').split('\t')
                    if len(lc) < len(lh): continue
                    portA[(lc[lx['year']], lc[lx['nation']], lc[lx['region']])].append(lc[lx['portA']])
            seen = collections.Counter()
        for ln in fh:
            c = ln.rstrip('\n').split('\t')
            if len(c) < len(head): continue
            n += 1
            y = int(c[ix['year']]); yrs.add(y)
            if portA is None: pn = c[ix['portNew']]
            else:
                key = (c[ix['year']], c[ix['nation']], c[ix['region']]); lst = portA.get(key); k = seen[key]; seen[key] += 1
                pn = lst[k] if lst is not None and k < len(lst) else None   # 突き合わせられない行は P1 の外れに数える
            pu, pg = c[ix['portUsed']], c[ix['portGc']]
            ru, lnw, lold, gc = num(c[ix['railUsedKm']]), num(c[ix['landKmNew']]), num(c[ix['landKmOld']]), num(c[ix['gcKm']])
            if not (pn is not None and pu == pn and abs(ru - lnw) <= 1e-8 * max(1.0, abs(lnw))): bad += 1
            if not (lnw <= lold + 1e-9): u1 += 1
            do, du = num(c[ix['railDecayOldIndex']]), num(c[ix['railDecayUsedIndex']])
            if do > 0:
                r = du / do; sumLoss[y] += 1 - r; dec.append(r)
            fo, fn = num(c[ix['fareLandOldIndex']]), num(c[ix['fareLandNewIndex']])
            if fo > 0: fare.append(fn / fo)
            if pu != pg:
                chg[y] += 1; natChg[c[ix['nation']]] += 1; nchg += 1
                if lnw > 2 * gc: nu2 += 1
            if c[ix['basinOld']] != c[ix['basinNew']]: bas[y] += 1
        ny = max(1, len(yrs))
        P2 = sum(sumLoss.values()) / ny; P3 = sum(chg.values()) / ny; B = sum(bas.values()) / ny
        P2s.append(P2); P3s.append(P3); BAS.append(B); bad_total += bad; rows_total += n; u1_total += u1
        chg_total += nchg; u2_total += nu2
        for k, v in natChg.items(): byNat[k].append(v / ny)
        u2s = f'{nu2 / nchg:.3f}' if nchg >= 30 else f'(母集団 {nchg} 行＜30)'
        print(f'seed {sd}\trows={n}\tyears={len(yrs)}\tP1外れ={bad}\tP2={P2:.3f}\tP3={P3:.2f}\tU1違反={u1}\tU2={nu2}/{nchg} {u2s}\t海盆が変わる州/年={B:.2f}')
mP2, mP3 = st.median(P2s), st.median(P3s)
u2 = (u2_total / chg_total) if chg_total else 0.0
print(f'P1 外れた行 {bad_total} / {rows_total}  -> {"通る" if bad_total == 0 and rows_total > 0 else "落ちる"}')
print(f'P2 種の中央値 {mP2:.3f} (>={P2_MIN}) -> {"通る" if mP2 >= P2_MIN else "落ちる"}')
print(f'P3 種の中央値 {mP3:.2f} (>={P3_MIN}) -> {"通る" if mP3 >= P3_MIN else "落ちる"}')
print(f'U1 違反の行 {u1_total} / {rows_total}  -> {"通る" if u1_total == 0 and rows_total > 0 else "落ちる"}')
print(f'U2 港が変わった行 {chg_total} のうち landKmNew>2×gcKm {u2_total} 行＝{u2:.3f} (<={U2_MAX}) -> {"通る" if u2 <= U2_MAX else "落ちる"}')
def dist(name, v, lt=0.99):
    if not v: return
    s = sorted(v); print(f'記録：{name} 中央値 {st.median(s):.4f} 最小 {s[0]:.4f} 最大 {s[-1]:.4f} {lt}未満の割合 {sum(1 for x in s if x < lt) / len(s):.3f}')
dist('鉄道の減衰の新÷旧', dec); dist('fareLand の新÷旧（landKmNew÷landKmOld）', fare)
print(f'記録：海盆が変わる州/年（種の中央値）{st.median(BAS):.2f}')
top = sorted(byNat.items(), key=lambda kv: -st.mean(kv[1]))[:8]
print('記録：港が変わる州/年（国・種の平均）' + '　'.join(f'{k} {st.mean(v):.2f}' for k, v in top))
