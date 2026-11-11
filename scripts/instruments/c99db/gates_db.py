# (1048) c99db 地主の政治 D-1b＋D-2 の門の台本（両側・旗 0 で落ちる）。事前登録 measurements/m1048_20261111_prereg_db.txt。
#   python scripts/instruments/c99db/gates_db.py <lordstat_s のあるフォルダ> [--paper]
#   腕の生（旗 1・計器の新しい列 d2LordLossLandExp・d2LordGainLandExp がある）では実際の列で門を読む。
#   --paper：旗 0 の生（基線 keep_1044/base_ins）から、正しい実装・旗 0・突然変異の〈実際の側〉を紙で作り、門ごとの赤と緑の表を出す。
#     旗 0 の生には新しい 2 列が無い＝土地改革の地代の損と得は旗 0 の実際（−2×(0.5＋損)）から逆算する（紙だけの近似・腕では計器の列を読む）。
import glob, csv, sys, math

TOL = 1e-9
NMIN = 200

def spearman(x, y):
    n = len(x)
    if n < 3: return float('nan')
    def ranks(v):
        idx = sorted(range(n), key=lambda i: v[i]); r = [0.0] * n; i = 0
        while i < n:
            j = i
            while j + 1 < n and v[idx[j + 1]] == v[idx[i]]: j += 1
            a = (i + j) / 2.0
            for k in range(i, j + 1): r[idx[k]] = a
            i = j + 1
        return r
    rx, ry = ranks(x), ranks(y)
    mx, my = sum(rx) / n, sum(ry) / n
    sxy = sum((a - mx) * (b - my) for a, b in zip(rx, ry)); sx = sum((a - mx) ** 2 for a in rx); sy = sum((b - my) ** 2 for b in ry)
    if not (sx > 0 and sy > 0): return 0.0   # 片側が定数＝相関が立たない＝0 とみなす（旗 0 で落ちる）
    return sxy / math.sqrt(sx * sy)

def load(d):
    rows = []
    for f in sorted(glob.glob(d + '/lordstat_s*.tsv')):
        with open(f, encoding='utf-8') as fh:
            rd = csv.reader(fh, delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
            for x in rd:
                g = lambda c: float(x[ix[c]])
                r = dict(rel=int(x[ix['stateReligionCode']]), bur=int(x[ix['bureaucracyCode']]),
                         clg=g('relClergyIncome0'), inc=g('relIncome0'), su=g('relStakeUpExp'), sd=g('relStakeDownExp'),
                         relUp=g('relPrefUpAct'), relDn=g('relPrefDownAct'),
                         cut=g('d2CutOverIncome0'), rent=g('d2Rent0OverIncome0'), rho=g('d2Rho'),
                         smB=g('d2SMidBurExp'), smL=g('d2SMidLandExp'), sgB=g('d2SGainBurExp'), sgL=g('d2SGainLandExp'),
                         burUp=g('d2PrefBurUpAct'), landUp=g('d2PrefLandUpAct'), burDn=g('d2PrefBurDownAct'), landDn=g('d2PrefLandDownAct'),
                         estN=g('dbuEstateNow'), estX=g('dbuEstateNext'), imN=g('dbuIntermediaryNow'))
                if 'd2LordLossLandExp' in ix: r['lossL'] = g('d2LordLossLandExp'); r['gainL'] = g('d2LordGainLandExp'); r['src'] = 'ins'
                else: r['lossL'] = -r['landUp'] / 2 - 0.5; r['gainL'] = -r['landDn'] / 2 - 0.5; r['src'] = 'flag0'
                rows.append(r)
    return rows

def frac(rows, pred):
    n = len(rows); k = sum(1 for r in rows if pred(r)); return (k / n if n else float('nan')), n

def gates(rows, a):
    """a(r) → dict(relUp, relDn, burUp, landUp, burDn, landDn)＝その腕（か紙の変異）の実際の側。返り値＝門の名→(値, N, 合否)。"""
    out = {}
    A = [a(r) for r in rows]
    # ---- D-1 国教 ----
    p = [(r, x) for r, x in zip(rows, A) if r['rel'] == 0 and r['clg'] > 0 and r['inc'] > 0]
    rho = spearman([x['relUp'] for _, x in p], [r['clg'] / r['inc'] for r, _ in p]); out['R-G1 相関≦−0.9'] = (rho, len(p), rho <= -0.9 and len(p) >= NMIN)
    bad = sum(1 for x in A if not (abs(x['relUp']) <= 2.0 + TOL and abs(x['relDn']) <= 3.0 + TOL)); out['R-G2 上限の破れ＝0'] = (bad, len(A), bad == 0)
    p = [(r, x) for r, x in zip(rows, A) if r['su'] > 0]
    k = sum(1 for r, x in p if abs(x['relUp'] - (-2 * (0.5 + r['su']))) <= TOL); v = k / len(p) if p else float('nan'); out['R-G3 一致≧0.999'] = (v, len(p), v >= 0.999 and len(p) >= NMIN)
    p = [(r, x) for r, x in zip(rows, A) if r['sd'] > 0]
    k = sum(1 for r, x in p if abs(x['relDn'] - (-2 * (0.5 + r['sd']))) <= TOL); v = k / len(p) if p else float('nan'); out['R-G4 向き≧0.999'] = (v, len(p), v >= 0.999 and len(p) >= NMIN)
    # ---- D-2 仲介の取り分 ----
    p = [(r, x) for r, x in zip(rows, A) if r['smB'] > 0]
    rho = spearman([x['burUp'] for _, x in p], [r['smB'] for r, _ in p]); out['I-G1官 相関≦−0.9'] = (rho, len(p), rho <= -0.9 and len(p) >= NMIN)
    p = [(r, x) for r, x in zip(rows, A) if r['smL'] > 0]
    rho = spearman([x['landUp'] - (-2 * (0.5 + r['lossL'])) for r, x in p], [r['smL'] for r, _ in p]); out['I-G1土 相関≦−0.9'] = (rho, len(p), rho <= -0.9 and len(p) >= NMIN)
    p = [(r, x) for r, x in zip(rows, A) if not (r['smB'] > 0)]
    k = sum(1 for r, x in p if abs(x['burUp'] - (-1.0)) <= TOL); v = k / len(p) if p else float('nan'); out['I-G2官 底のまま≧0.999'] = (v, len(p), v >= 0.999)
    p = [(r, x) for r, x in zip(rows, A) if not (r['smL'] > 0)]
    k = sum(1 for r, x in p if abs(x['landUp'] - (-2 * (0.5 + r['lossL']))) <= TOL); v = k / len(p) if p else float('nan'); out['I-G2土 旧の値のまま≧0.999'] = (v, len(p), v >= 0.999)
    p = [(r, x) for r, x in zip(rows, A) if r['smB'] > 0]
    k = sum(1 for r, x in p if abs(x['burUp'] - (-2 * (0.5 + r['smB']))) <= TOL); v = k / len(p) if p else float('nan'); out['I-G3官 一致≧0.999'] = (v, len(p), v >= 0.999 and len(p) >= NMIN)
    p = [(r, x) for r, x in zip(rows, A) if r['smL'] > 0]
    k = sum(1 for r, x in p if abs(x['landUp'] - (-2 * (0.5 + r['lossL'] + r['smL']))) <= TOL); v = k / len(p) if p else float('nan'); out['I-G3土 一致≧0.999'] = (v, len(p), v >= 0.999 and len(p) >= NMIN)
    k = sum(1 for r, x in p if abs((x['landUp'] - (-2 * (0.5 + r['lossL']))) / -2.0 - r['smL']) <= TOL); v = k / len(p) if p else float('nan'); out['I-G4土 足したのは取り分だけ≧0.999'] = (v, len(p), v >= 0.999 and len(p) >= NMIN)
    p = [(r, x) for r, x in zip(rows, A) if r['sgB'] > 0]
    k = sum(1 for r, x in p if abs(x['burDn'] - (-2 * (0.5 + r['sgB']))) <= TOL); v = k / len(p) if p else float('nan'); out['I-G5官 減らす向き≧0.999'] = (v, len(p), v >= 0.999 and len(p) >= NMIN)
    p = [(r, x) for r, x in zip(rows, A) if r['sgL'] > 0]
    k = sum(1 for r, x in p if abs(x['landDn'] - (-2 * (0.5 + r['gainL'] + r['sgL']))) <= TOL); v = k / len(p) if p else float('nan'); out['I-G5土 減らす向き≧0.999'] = (v, len(p), v >= 0.999 and len(p) >= NMIN)
    return out

ACT = lambda r: dict(relUp=r['relUp'], relDn=r['relDn'], burUp=r['burUp'], landUp=r['landUp'], burDn=r['burDn'], landDn=r['landDn'])

def correct(r, smB=None, smL=None, sgB=None, sgL=None, lossL=None, gainL=None, su=None, sd=None, floor=0.5):
    smB = r['smB'] if smB is None else smB; smL = r['smL'] if smL is None else smL; sgB = r['sgB'] if sgB is None else sgB; sgL = r['sgL'] if sgL is None else sgL
    lossL = r['lossL'] if lossL is None else lossL; gainL = r['gainL'] if gainL is None else gainL; su = r['su'] if su is None else su; sd = r['sd'] if sd is None else sd
    return dict(relUp=-2 * (0.5 + su), relDn=-2 * (0.5 + sd), burUp=-2 * (floor + smB), burDn=-2 * (floor + sgB),
                landUp=-2 * (0.5 + lossL + smL), landDn=-2 * (0.5 + gainL + sgL))

def degfac_next2(code):   # M-b：段を 1 つずらす（国教→信仰の自由 0.3＝(1−0.3)/1・内心の自由→信仰の自由のまま・ほかは 0）
    return {0: 0.7, 1: 0.5}.get(code, 0.0)
def degfac_next(code):
    return {0: 0.4, 1: 0.5}.get(code, 0.0)

def paper(rows):
    def cshare(r): return r['clg'] / r['inc'] if r['inc'] > 1e-9 else 0.0
    def by_rent(s, r): return s / r['rent'] if r['rent'] > 1e-9 else 0.0
    def no_rho(s, r): return s / r['rho'] if r['rho'] > 0 else 0.0
    def caseC(r): return r['cut'] * r['rho'] * (r['estN'] - r['estX']) / r['estN'] if r['estN'] > 1e-9 else 0.0
    def loss6(r): return r['lossL'] * (r['rent'] + r['cut']) / r['rent'] if r['rent'] > 1e-9 else 0.0
    def gain6(r): return r['gainL'] * (r['rent'] + r['cut']) / r['rent'] if r['rent'] > 1e-9 else 0.0
    V = [
        ('正しい実装', lambda r: correct(r)),
        ('旗 0（基線の実際）', ACT),
        ('D2-M1 ρ＝1', lambda r: correct(r, smB=no_rho(r['smB'], r), smL=no_rho(r['smL'], r), sgB=no_rho(r['sgB'], r), sgL=no_rho(r['sgL'], r))),
        ('D2-M2 max でなく軸ごと（官僚制度の身分の項）', lambda r: correct(r, smB=caseC(r))),
        ('D2-M3 底を外す（官僚制度）', lambda r: correct(r, floor=0.0)),
        ('D2-M4 旗を読まない', ACT),
        ('D2-M5 土地改革に足し忘れ', lambda r: correct(r, smL=0.0, sgL=0.0)),
        ('D2-M6 取り分を地代の側にも足す', lambda r: correct(r, lossL=loss6(r), gainL=gain6(r))),
        ('D2-M7 分母を Rent0', lambda r: correct(r, smB=by_rent(r['smB'], r), smL=by_rent(r['smL'], r))),
        ('D2-M8 減らす向きの得を落とす', lambda r: correct(r, sgB=0.0, sgL=0.0)),
        ('D2-M9 DirectionAware に官僚制度を足し忘れ', lambda r: correct(r)),
        ('D1-Ma 比でなく生の月額', lambda r: correct(r, su=(r['su'] / cshare(r) * r['clg'] if cshare(r) > 0 else 0.0), sd=(r['sd'] / cshare(r) * r['clg'] if cshare(r) > 0 else 0.0))),
        ('D1-Mb 段を 1 つずらす', lambda r: correct(r, su=cshare(r) * degfac_next2(r['rel']) if r['su'] > 0 else 0.0)),
        ('D1-Mc 旗を読まない', ACT),
        ('D1-M2 分母を Rent0', lambda r: correct(r, su=by_rent(r['su'], r), sd=by_rent(r['sd'], r))),
        ('D1-M6b 減らす向きも損で読む', lambda r: correct(r, sd=r['su'])),
        ('D1-M6a DirectionAware に国教を足し忘れ', lambda r: correct(r)),
    ]
    names = None
    for nm, f in V:
        g = gates(rows, f)
        if names is None:
            names = list(g.keys()); print('変異\t' + '\t'.join(names))
        print(nm + '\t' + '\t'.join(('g' if g[k][2] else 'R') for k in names))
    print()
    for nm, f in V[:2]:
        g = gates(rows, f)
        print('--', nm)
        for k, (v, n, ok) in g.items(): print(f'   {k}\t値 {v}\tN {n}\t{"緑" if ok else "赤"}')

def counts(rows):
    n = len(rows)
    print('行（国×四半期）', n)
    print('官僚制度の一段で仲介の取り分を失う行（d2SMidBurExp>0）', sum(1 for r in rows if r['smB'] > 0))
    print('土地改革の一段で仲介の取り分を失う行（d2SMidLandExp>0）', sum(1 for r in rows if r['smL'] > 0))
    print('どちらも（AND）', sum(1 for r in rows if r['smB'] > 0 and r['smL'] > 0), '・どちらか（OR）', sum(1 for r in rows if r['smB'] > 0 or r['smL'] > 0))
    print('官僚制度の一段で I が下がる行（取り分 0 を含む・dbuIntermediaryNow>Next の代わりに 段の差）', sum(1 for r in rows if r['imN'] > 0 and r['estN'] > r['estX'] and r['imN'] == r['estN'] and r['estN'] > 0))
    print('官僚制度を戻すと得る行（d2SGainBurExp>0）', sum(1 for r in rows if r['sgB'] > 0), '・土地改革（d2SGainLandExp>0）', sum(1 for r in rows if r['sgL'] > 0))
    print('Cut÷Income0 が正の行', sum(1 for r in rows if r['cut'] > 0), '・ρ の外れ（0〜1 の外）', sum(1 for r in rows if not (0 <= r['rho'] <= 1)))
    print('国教 段 0 で聖職者の所得が正の行（R-G1 の母集合）', sum(1 for r in rows if r['rel'] == 0 and r['clg'] > 0 and r['inc'] > 0))
    print('国教の上げで失う行（relStakeUpExp>0）', sum(1 for r in rows if r['su'] > 0), '・戻して得る行（relStakeDownExp>0）', sum(1 for r in rows if r['sd'] > 0))
    for k in ('smB', 'smL', 'sgB', 'sgL', 'su', 'sd'):
        v = sorted(r[k] for r in rows if r[k] > 0)
        if v: print(f'  {k} の正の行の中央 {v[len(v)//2]:.5f}・p90 {v[int(len(v)*0.9)]:.5f}・最大 {v[-1]:.5f}')

if __name__ == '__main__':
    d = sys.argv[1]; rows = load(d)
    print('出所', d, '・列', rows[0]['src'] if rows else '-')
    counts(rows)
    print()
    if '--paper' in sys.argv: paper(rows)
    else:
        g = gates(rows, ACT); allok = True
        for k, (v, n, ok) in g.items(): print(f'{k}\t値 {v}\tN {n}\t{"緑" if ok else "赤"}'); allok &= ok
        print('門の判定:', '全て緑' if allok else '赤あり')
