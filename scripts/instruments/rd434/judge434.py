# ★(434) 清算の片方向の錠・後半の判定の読み手（読むだけ・シミュを回さない）。事前登録 measurements/cl_20261019_prereg.txt の追記 2 の判定をそのまま印字する。
# 使い方：PYTHONUTF8=1 python judge434.py --A <A の計器の生> --B <B> [--R <N=1 の参照腕>] --N1 <ヌル腕 1> --N2 <ヌル腕 2> [--seeds 1,2,...,10] [--seedsR 1,2,3,4,5]
#   A・B・ヌル腕は --seeds、参照腕 R は --seedsR の種で読む（A・B・ヌル腕も R と比べる項では --seedsR の種だけで読み直す）。
# 拘束する判定（外れたら〈機構が狙いどおり働いていない〉）＝T1′・T2′・T3・T4・T6。記録＝T1（代数）・T2・T5・csc の偏り。
import sys, os, subprocess, json, math, argparse, csv, collections
ap = argparse.ArgumentParser()
for k in ['A', 'B', 'R', 'N1', 'N2']: ap.add_argument('--' + k)
ap.add_argument('--seeds', default='1,2,3,4,5,6,7,8,9,10'); ap.add_argument('--seedsR', default='1,2,3,4,5'); ap.add_argument('--N', type=int, default=4)
g = ap.parse_args()
HERE = os.path.dirname(os.path.abspath(__file__))
def port(folder, seeds):
    if not folder: return None
    r = subprocess.run([sys.executable, '-X', 'utf8', os.path.join(HERE, 'port434.py'), folder, '--seeds', seeds, '--json'], capture_output=True, text=True, encoding='utf-8', env={**os.environ, 'PYTHONUTF8': '1'})
    if r.returncode != 0: raise SystemExit(r.stderr)
    return json.loads(r.stdout)
ERA = ['1783-1850', '1851-1913', '1914-1949']
sub = g.seedsR
P = {k: port(getattr(g, k), g.seeds) for k in ['A', 'B', 'N1', 'N2']}
Ps = {k: port(getattr(g, k), sub) for k in ['A', 'B', 'N1', 'N2', 'R']}   # R と同じ種だけで読んだ物
def cell(d, e, key): return d['cell'][f'SUM|{e}'][key]
res = {}
print(f'A={g.A}\nB={g.B}\nR={g.R}\nN1={g.N1}\nN2={g.N2}\n種（A・B・ヌル）={g.seeds}　種（R と比べる項）={sub}\n')
def qualify(key):
    out = []
    for e in range(3):
        a = cell(Ps['A'], e, key); r = cell(Ps['R'], e, key); b = cell(Ps['B'], e, key)
        noise = max(abs(cell(Ps['N1'], e, key) - a), abs(cell(Ps['N2'], e, key) - a))
        out.append((e, a, b, r, noise, abs(r - a) > noise))
    return out
if Ps['R']:
    print('== T1′ 港の州の潜在量（清・露・英の和・四半期あたり）：B が A より N=1 の参照腕 R に近いか ==')
    q = qualify('pot'); ok_closer = 0; nq = 0; ok_over = True
    for e, a, b, r, noise, qual in q:
        closer = abs(math.log(b / r)) < abs(math.log(a / r)) if min(a, b, r) > 0 else False
        print(f'  {ERA[e]}: A={a:.4g} B={b:.4g} R={r:.4g} | ヌル腕の幅（|n−A| の最大）={noise:.3g}　|R−A|={abs(r-a):.3g} {"識別できる" if qual else "雑音の内（数えない）"} | B÷R={b/r:.2f} A÷R={a/r:.2f} B が近い={closer}')
        if qual:
            nq += 1; ok_closer += closer
            if r > a and not (0.8 <= b / r <= 1.35): ok_over = False
    need = nq if nq <= 2 else 2
    t1 = None if nq == 0 else (ok_closer >= need and ok_over)
    print(f'  → 識別できる時代 {nq}・B が近い {ok_closer}（要 {need}）・上振れ（B÷R が 0.8〜1.35）{"内" if ok_over else "外"} ⇒ T1′ = {"判定不能（記録）" if t1 is None else ("合格" if t1 else "外れ＝採らない")}')
    res['T1p'] = t1
    print('== T2′ 港の拡張の門を通った州×四半期の数（清・露・英の和・1 試合あたり）：B の動きが R の向きか ==')
    q = qualify('gate'); nq = 0; okd = 0
    for e, a, b, r, noise, qual in q:
        same = (b - a) * (r - a) > 0
        print(f'  {ERA[e]}: A={a:.1f} B={b:.1f} R={r:.1f} | ヌル腕の幅={noise:.1f} {"識別できる" if qual else "雑音の内（数えない）"} | 向きが同じ={same}')
        if qual: nq += 1; okd += same
    need = nq if nq <= 2 else 2
    t2 = None if nq == 0 else okd >= need
    print(f'  → 識別できる時代 {nq}・向きが同じ {okd}（要 {need}） ⇒ T2′ = {"判定不能（記録）" if t2 is None else ("合格" if t2 else "外れ＝採らない")}')
    res['T2p'] = t2
else:
    print('R が無い＝T1′・T2′ は判定できない')
cvA = P['A']['cv_all']; cvB = P['B']['cv_all']
print(f'== T3 潜在量の四半期間の変動 ==\n  A={cvA:.4f} B={cvB:.4f} N1={P["N1"]["cv_all"]:.4f} N2={P["N2"]["cv_all"]:.4f} 上限（A の 2 倍）={2*cvA:.4f} ⇒ {"合格" if cvB <= 2*cvA else "外れ＝採らない"}')
res['T3'] = cvB <= 2 * cvA
print('== T6 律速の状態の往復（持続比 ρ＝反転率÷(2p(1−p))・p＝律速の割合・清露英の和）：1.0 未満かつヌル腕 3 本の最大の 1.25 倍以下 ==')
def rho(d, e):
    c = d['cell'][f'SUM|{e}']; p = c['thr']; return c['flipRate'] / max(1e-9, 2 * p * (1 - p))
ok6 = True
for e in range(3):
    ra, r1, r2, rb = rho(P['A'], e), rho(P['N1'], e), rho(P['N2'], e), rho(P['B'], e)
    lim = 1.25 * max(ra, r1, r2); ok = rb < 1.0 and rb <= lim; ok6 &= ok
    print(f'  {ERA[e]}: A={ra:.3f} N1={r1:.3f} N2={r2:.3f} B={rb:.3f} 上限={min(1.0, lim):.3f}　律速の割合 A={100*cell(P["A"],e,"thr"):.1f}% B={100*cell(P["B"],e,"thr"):.1f}%　反転率 A={cell(P["A"],e,"flipRate"):.3f} B={cell(P["B"],e,"flipRate"):.3f} {"○" if ok else "×"}')
print(f'  ⇒ T6 = {"合格" if ok6 else "外れ＝採らない"}'); res['T6'] = ok6
print('== 記録 T2 港の容量・潜在量（清・露・英の和・四半期あたり）==')
for e in range(3):
    print(f'  {ERA[e]}: 潜在量 A={cell(P["A"],e,"pot"):.4g} N1={cell(P["N1"],e,"pot"):.4g} N2={cell(P["N2"],e,"pot"):.4g} B={cell(P["B"],e,"pot"):.4g} | 容量 A={cell(P["A"],e,"cap"):.4g} N1={cell(P["N1"],e,"cap"):.4g} N2={cell(P["N2"],e,"cap"):.4g} B={cell(P["B"],e,"cap"):.4g}')
def csc(folder, seeds):
    acc = collections.defaultdict(lambda: collections.defaultdict(float)); n = 0
    for s in seeds:
        try: f = open(f'{folder}/csc_s{s}.tsv', encoding='utf-8')
        except OSError: continue
        n += 1
        with f:
            for r in csv.DictReader(f, delimiter='\t'):
                if r['roleCode'] != '0': continue
                e = 0 if int(r['year']) < 1830 else 1 if int(r['year']) < 1870 else 2
                for key in ['rowsCount', 'throttledRowsCount', 'potAddedPerYear', 'potRawPerYear', 'qtyPerYear', 'fillsCount']:
                    if key in r: acc[(r['nation'], e, int(r['classCode']))][key] += float(r[key])
    return acc, n
SEEDS = [int(x) for x in g.seeds.split(',')]; SUB = [int(x) for x in sub.split(',')]
cA, nA = csc(g.A, SEEDS); cB, nB = csc(g.B, SEEDS)
print('== T4 律速の州の走査：B の種類 1 の律速の行（全ての国の和）÷ A（要 2.5〜8.0。下限＝(ii) が起こした・上限＝律速が増える分の許し 2×N・AI の置き）==')
ta = sum(a['throttledRowsCount'] for (n, e, c), a in cA.items() if c == 1); tb = sum(a['throttledRowsCount'] for (n, e, c), a in cB.items() if c == 1)
for e in range(3):
    xa = sum(a['throttledRowsCount'] for (n, ee, c), a in cA.items() if c == 1 and ee == e); xb = sum(a['throttledRowsCount'] for (n, ee, c), a in cB.items() if c == 1 and ee == e)
    print(f'  csc 時代{e}: A={xa/nA:.4g} B={xb/nB:.4g} 比={(xb/nB)/max(1e-9,xa/nA):.2f}')
ratio4 = (tb / nB) / max(1e-9, ta / nA)
ok4 = 2.5 <= ratio4 <= 8.0
print(f'  全体: A={ta/nA:.4g} B={tb/nB:.4g} 比={ratio4:.2f} ⇒ T4 = {"合格" if ok4 else "外れ＝採らない"}'); res['T4'] = ok4
print('== 記録 T1（代数）P_B÷P_A（csc の potAdded・種類 0＋1）と 1÷(1−L_A) ==')
for n in ['清', 'ロシア', 'イギリス']:
    for e in range(3):
        a0 = cA.get((n, e, 0), {}); a1 = cA.get((n, e, 1), {}); b0 = cB.get((n, e, 0), {}); b1 = cB.get((n, e, 1), {})
        if not a1 or not b1: continue
        pa = (a0.get('potAddedPerYear', 0) + a1.get('potAddedPerYear', 0)) / nA; pb = (b0.get('potAddedPerYear', 0) + b1.get('potAddedPerYear', 0)) / nB
        L = (g.N - 1) * a1['potRawPerYear'] / max(1e-300, a0['potRawPerYear'] + g.N * a1['potRawPerYear'])
        print(f'  {n} 時代{e}: P_B/P_A={pb/max(1e-300,pa):.2f}　期待 1/(1−L_A)={1/(1-L):.2f}')
if g.R:
    cR, nR = csc(g.R, SUB); cAs, nAs = csc(g.A, SUB); cBs, nBs = csc(g.B, SUB)
    print('== 記録 csc の潜在量の偏り（種類 0＋1 の potAdded・清露英の和・R と同じ種）：B÷R ＞ 1 なら N 倍が流れの行を過大に見積もる読み（点検の中 3）==')
    for e in range(3):
        def tot(c, nn): return sum(a.get('potAddedPerYear', 0) for (n, ee, cl), a in c.items() if n in ('清', 'ロシア', 'イギリス') and ee == e and cl in (0, 1)) / max(1, nn)
        pa, pb, pr = tot(cAs, nAs), tot(cBs, nBs), tot(cR, nR)
        print(f'  時代{e}: A={pa:.4g} B={pb:.4g} R={pr:.4g} | B÷R={pb/max(1e-300,pr):.2f} A÷R={pa/max(1e-300,pr):.2f}')
print('\n要約:', {k: ('不能' if v is None else ('合格' if v else '外れ')) for k, v in res.items()})
