# (1005) c92tf 読むだけの台本：土地税の算定の基準 A-2b-1c′〈法の段（農奴解放）から T を導く形で、f を置く時点を 3 案〉の紙（盤面 m1001・keep_1001 の 4 腕）。
# 物差しは (987) の prereg_s0.md（φ* の影 s＝0.12・下の門＝0.5 をまたいだ種が 1 本以上・上の門＝Σ|Δφ*| の 10 種の中央≦5）をそのまま使う。
# 出所の注：load・num・follow・metrics・gates・clamp01・t_gamma・selfcheck・(987) 再現の束P は scripts/instruments/r987/gamma_read.py の写し（式は変えていない）。
# 足した物：START（φ* を動かし始める四半期・(987) は 20・エンジンの器は 0）と、f を置く時点の 3 案（F0・F1・F2≥・F2>）。
# 使い方：python -X utf8 -I latch_read.py <書き出すファイル> <腕の名前>=<basisx_s*.tsv と reform_s*.tsv のあるフォルダ> [...]
# エンジンの関数を呼ばない・生は読むだけ・新しい係数を置かない（0.5 は門の数え方の境・s は Coefficients.cs:506 の写し・段÷2 は ReformLevel.Full＝2 で割る正規化）。
import sys, os, glob, statistics

ANCHOR = {'フランス': ('P->F', 1790), 'プロイセン': ('P->F', 1861), '日本': ('P->F', 1873), '清': ('F->P', 1941)}
RECORD = ['イギリス', 'ロシア', 'オーストリア', 'オスマン', 'アメリカ']   # 記録だけ（門に入れない）
OPEN_FIXED = {'清', 'イギリス'}
S = 0.12; UP = 5.0
YEARS = [1790, 1800, 1830, 1850, 1861, 1873, 1900, 1941]
NAN = float('nan')

def load(path):
    data = open(path, 'rb').read().decode('utf-8').split('\n')
    head = data[0].rstrip('\r').split('\t')
    return [dict(zip(head, ln.rstrip('\r').split('\t'))) for ln in data[1:] if ln.strip()]

def num(s):
    try: return float(s)
    except (ValueError, TypeError): return NAN

def seed_of(p, pre):
    b = os.path.basename(p); return int(b[len(pre):-len('.tsv')])

def series(ins):
    out = {}
    for p in sorted(glob.glob(os.path.join(ins, 'basisx_s*.tsv')), key=lambda p: seed_of(p, 'basisx_s')):
        sd = seed_of(p, 'basisx_s'); rf = {}
        rp = os.path.join(ins, 'reform_s%d.tsv' % sd)
        if os.path.exists(rp):
            for r in load(rp): rf[(int(r['quarter']), r['nation'])] = num(r['serfLawCode'])
        byn = {}
        for r in load(p): byn.setdefault(r['nation'], []).append(r)
        for nat, rs in byn.items():
            rs.sort(key=lambda r: int(r['quarter']))
            q = [int(r['quarter']) for r in rs]
            out.setdefault(nat, []).append(dict(seed=sd, q=q, y=[int(r['year']) for r in rs],
                pi=[num(r['bxExpInflRulePerYear']) for r in rs], R=[num(r['bxRBase']) for r in rs],
                fz=[num(r['bxFrozenPbPerMonth']) for r in rs], rfz=[num(r['bxRentOfFrozenPbPerMonth']) for r in rs],
                serf=[rf.get((qq, nat), NAN) for qq in q]))
    return out

def clamp01(x): return min(1.0, max(0.0, x))

def t_gamma(R, pi, f):
    if not (R == R and pi == pi and f == f): return NAN
    den = R * (1 + pi)
    return clamp01(f / den) if den > 0 else 1.0

def follow(T, q, start, q0):
    phi = start; s = []
    for t, qq in zip(T, q):
        if qq >= q0 and t == t: phi = phi + S * (t - phi)
        s.append(phi)
    return s

def metrics(ph, q, q0):
    cross = 0; tv = 0.0
    for i in range(1, len(ph)):
        if q[i - 1] < q0: continue
        if (ph[i - 1] >= 0.5) != (ph[i] >= 0.5): cross += 1
        tv += abs(ph[i] - ph[i - 1])
    return cross, tv

def gates(ms):
    if not ms: return None, None, NAN, 0
    lo = sum(1 for c, _ in ms if c >= 1)
    med = statistics.median([t for _, t in ms])
    return lo >= 1, med <= UP, med, lo

# ---- f を置く時点（latch）の 3 案。段＝serfLawCode（0 なし・1 限定・2 全面）。----
def latch_ok(case, b, pi):
    if not (b == b): return False
    if case == 'F0': return b > 0
    if case == 'F1': return b >= 2
    c = clamp01(1.0 / (1.0 + pi)) if (pi == pi and 1.0 + pi > 0) else (1.0 if pi == pi else NAN)
    tp = (b / 2.0) * c
    if not (tp == tp): return False
    if case == 'F2ge': return tp >= 0.5
    if case == 'F2gt': return tp > 0.5
    raise ValueError(case)

def T_case(s, nat, case, q0, mode):
    """T＝(段÷2)×T_γ(f)。f は latch の四半期の R（開幕で定額の国は f＝R[q0 の行]＝エンジンの凍結が助走の末＝q=0 の直前なのと揃える）。latch の前は T＝0。
    mode＝'987'：(987) の束P をそのまま（開幕で定額の国は f＝R[0]・他は q≥20 で段＞0 の最初の行）。f は一度置いたら替えない（エンジンの FrozenLandRentPerMonth と同じ）。"""
    out = []; latched = False; f = NAN; lq = None
    fixed0 = nat in OPEN_FIXED
    if fixed0:
        latched = True; f = s['R'][0]; lq = s['q'][0]
        if mode != '987':
            for r, qq in zip(s['R'], s['q']):
                if qq >= q0 and r == r: f = r; lq = qq; break
    for b, r, x, qq in zip(s['serf'], s['R'], s['pi'], s['q']):
        if not latched and qq >= q0 and r == r and latch_ok(case, b, x):
            latched = True; f = r; lq = qq
        g = t_gamma(r, x, f) if latched else 0.0
        out.append((b / 2.0 if b == b else 0.0) * g if g == g else NAN)
    return out, f, lq

def year_at(s, y0):
    for i, y in enumerate(s['y']):
        if y >= y0: return i
    return None

def fmt_rng(v):
    v = [x for x in v if x == x]
    if not v: return '-'
    return '%.2f[%.2f〜%.2f]' % (statistics.median(v), min(v), max(v))

def selfcheck(W):
    W('## 0. 自己検査')
    n = 600; q = list(range(n)); ok = True
    cases = [('何もしない', [0.0] * n, (False, True)), ('毎四半期に反転', [float(i % 2) for i in range(n)], (True, False)),
             ('1 回だけ 0→1', [1.0 if i >= 100 else 0.0 for i in range(n)], (True, True)),
             ('往復 2 回', [1.0 if (i >= 20 and ((i - 20) // 40) % 2 == 0 and i < 180) else 0.0 for i in range(n)], (True, True)),
             ('往復 3 回', [1.0 if (i >= 20 and ((i - 20) // 40) % 2 == 0 and i < 260) else 0.0 for i in range(n)], (True, False))]
    for nm, T, exp in cases:
        m = metrics(follow(T, q, 0.0, 20), q, 20); lo, up, med, _ = gates([m] * 10)
        good = (lo, up) == exp; ok &= good
        W('門\t%s\t下=%s\t上=%s\tΣ=%.3g\t期待=%s\t%s' % (nm, lo, up, med, exp, '合格' if good else '★外れ'))
    # latch の自己検査：段 0→1 を q=10・1→2 を q=50・π は q<30 で 0・以後 0.05（手で置いた系列）
    syn = dict(q=q, y=[1783 + i // 4 for i in q], R=[100.0] * n, pi=[0.0 if i < 30 else 0.05 for i in q],
               serf=[0.0 if i < 10 else (1.0 if i < 50 else 2.0) for i in q])
    exp = {'F0': 10, 'F1': 50, 'F2ge': 10, 'F2gt': 50}
    for c, e in exp.items():
        _, _, lq = T_case(syn, 'X', c, 0, 'main'); good = lq == e; ok &= good
        W('置く時点\t%s\tlatch=%s\t期待=%s\t%s' % (c, lq, e, '合格' if good else '★外れ'))
    syn2 = dict(syn); syn2['pi'] = [0.05] * n   # 限定（段 1）で π＞0 なら F2≥ は全面まで置かない＝刃の上
    _, _, lq = T_case(syn2, 'X', 'F2ge', 0, 'main'); good = lq == 50; ok &= good
    W('置く時点\tF2ge（π＞0）\tlatch=%s\t期待=50\t%s' % (lq, '合格' if good else '★外れ'))
    W('自己検査：%s' % ('合格' if ok else '★外れ')); W('')
    return ok

def main():
    out = sys.argv[1]; arms = [a.split('=', 1) for a in sys.argv[2:]]
    L = []; W = L.append
    W('# latch_read（(1005) c92tf）：盤面 m1001・物差しは (987) prereg_s0.md（s＝%g・上の門 Σ≦%g・下の門 0.5 をまたいだ種≥1）。' % (S, UP))
    if not selfcheck(W):
        open(out, 'wb').write(('\n'.join(L) + '\n').encode('utf-8')); sys.exit(1)
    D = {name: series(ins) for name, ins in arms}
    W('## 1. 農奴解放の段（serfLawCode）の道筋（腕ごと・国ごと・種の数＝列の数）')
    W('腕\t国\tq=0 の段(種ごと)\t限定(≥1)に最初に達した年\t全面(2)に最初に達した年\t段が下がった回数の和\t下がった種の数')
    for name, _ in arms:
        for nat in list(ANCHOR) + RECORD:
            ss = D[name].get(nat, []); op = []; y1 = []; y2 = []; down = 0; dseeds = 0
            for s in ss:
                op.append('%g' % s['serf'][0] if s['serf'][0] == s['serf'][0] else '-')
                a1 = '-'; a2 = '-'
                for v, y in zip(s['serf'], s['y']):
                    if a1 == '-' and v == v and v >= 1: a1 = str(y)
                    if a2 == '-' and v == v and v >= 2: a2 = str(y)
                y1.append(a1); y2.append(a2)
                d = sum(1 for i in range(1, len(s['serf'])) if s['serf'][i] == s['serf'][i] and s['serf'][i - 1] == s['serf'][i - 1] and s['serf'][i] < s['serf'][i - 1])
                down += d; dseeds += 1 if d else 0
            W('%s\t%s\t%s\t%s\t%s\t%d\t%d' % (name, nat, ','.join(op), ','.join(y1), ','.join(y2), down, dseeds))
    W('')
    combos = [('987 再現（束P・q≥20・開幕で定額は f＝R[0]）', 'F0', 20, '987'),
              ('F0 q≥20', 'F0', 20, 'main'), ('F1 q≥20', 'F1', 20, 'main'), ('F2≥ q≥20', 'F2ge', 20, 'main'), ('F2> q≥20', 'F2gt', 20, 'main'),
              ('F0 q≥0（エンジンの器の形）', 'F0', 0, 'main'), ('F1 q≥0', 'F1', 0, 'main'), ('F2≥ q≥0', 'F2ge', 0, 'main'), ('F2> q≥0', 'F2gt', 0, 'main')]
    W('## 2. 両側の門（錨の 4 国・腕ごと）')
    W('形\t腕\t仏 下/上Σ\t普 下/上Σ\t日 下/上Σ\t清 下/上Σ\t両側を通る錨の国')
    detail = {}
    for cname, case, q0, mode in combos:
        for name, _ in arms:
            cells = []; npass = 0
            for nat in list(ANCHOR) + RECORD:
                ss = D[name].get(nat, []); ms = []; rec = []
                for s in ss:
                    T, f, lq = T_case(s, nat, case, q0, mode)
                    st = 1.0 if nat in OPEN_FIXED else 0.0
                    ph = follow(T, s['q'], st, q0)
                    ms.append(metrics(ph, s['q'], q0))
                    pos = '-'; up_y = '-'; dn_y = '-'
                    for i in range(len(ph)):
                        if pos == '-' and s['q'][i] >= q0 and ph[i] > 0 and nat not in OPEN_FIXED: pos = str(s['y'][i])
                        if i and s['q'][i - 1] >= q0:
                            if up_y == '-' and ph[i - 1] < 0.5 <= ph[i]: up_y = str(s['y'][i])
                            if dn_y == '-' and ph[i - 1] >= 0.5 > ph[i]: dn_y = str(s['y'][i])
                    ly = '-' if lq is None else str(s['y'][s['q'].index(lq)])
                    phy = {}; fr = {}
                    for y0 in YEARS:
                        i = year_at(s, y0)
                        if i is None: continue
                        phy[y0] = ph[i]
                        fr[y0] = (f if lq is not None and s['q'][i] >= lq else NAN, s['R'][i])
                    rec.append(dict(seed=s['seed'], ly=ly, pos=pos, up=up_y, dn=dn_y, phy=phy, fr=fr))
                detail[(cname, name, nat)] = rec
                if nat in ANCHOR:
                    lo, up, med, nlo = gates(ms)
                    if lo and up: npass += 1
                    cells.append('%d/%d・%.3g%s' % (nlo, len(ms), med, '' if (lo and up) else '✗'))
            W('%s\t%s\t%s\t%d/4' % (cname, name, '\t'.join(cells), npass))
    W('')
    W('## 3. 案ごとの時期と割合（国ごと・4 腕を合わせた種）')
    W('（f を置いた年＝latch・φ* が正になった年は開幕で比例の国だけ・0.5 を上へ/下へまたいだ最初の年・φ* は中央[最小〜最大]・f÷R は和÷和〔f を置いた種だけ・Σf÷ΣR〕）')
    for cname, case, q0, mode in combos:
        W('### %s' % cname)
        W('国\t種\tf を置いた年(中央[最小〜最大])\tφ*＞0 の年(中央)\t0.5 を上へ(中央・またいだ種)\t0.5 を下へ(中央・またいだ種)\t' + '\t'.join('φ*@%d' % y for y in YEARS) + '\t' + '\t'.join('f÷R@%d' % y for y in YEARS))
        for nat in list(ANCHOR) + RECORD:
            rec = [r for name, _ in arms for r in detail[(cname, name, nat)]]
            def yy(key):
                v = [int(r[key]) for r in rec if r[key] != '-']
                return ('%d[%d〜%d]' % (statistics.median(v), min(v), max(v)) if v else '-') + '・%d' % len(v)
            phc = [fmt_rng([r['phy'].get(y, NAN) for r in rec]) for y in YEARS]
            frc = []
            for y in YEARS:
                pr = [r['fr'][y] for r in rec if y in r['fr'] and r['fr'][y][0] == r['fr'][y][0] and r['fr'][y][1] == r['fr'][y][1]]
                frc.append('%.2f(%d)' % (sum(a for a, _ in pr) / sum(b for _, b in pr), len(pr)) if pr and sum(b for _, b in pr) > 0 else '-')
            W('%s\t%d\t%s\t%s\t%s\t%s\t%s\t%s' % (nat, len(rec), yy('ly'), yy('pos'), yy('up'), yy('dn'), '\t'.join(phc), '\t'.join(frc)))
        W('')
    W('## 4. 開幕で定額の国（清・イギリス）の f の揃え方：エンジンの凍結の写し（bxFrozenPbPerMonth・q=0 から一定か）と bxRBase の比')
    W('腕\t国\t凍結の写しが q=0 から全行で同じ値の種\tf_eng÷R[0](和÷和)\tR[20]÷R[0](和÷和)\tf_eng÷rfz[0](和÷和)')
    for name, _ in arms:
        for nat in sorted(OPEN_FIXED):
            ss = D[name].get(nat, [])
            const = sum(1 for s in ss if all(v == s['fz'][0] for v in s['fz'] if v == v))
            i20 = [year_at(s, 0) for s in ss]
            a = sum(s['fz'][0] for s in ss); b = sum(s['R'][0] for s in ss)
            r20 = sum(s['R'][s['q'].index(20)] for s in ss if 20 in s['q'])
            c = sum(s['rfz'][0] for s in ss)
            W('%s\t%s\t%d/%d\t%.3f\t%.3f\t%.3f' % (name, nat, const, len(ss), a / b if b else NAN, r20 / b if b else NAN, a / c if c else NAN))
    open(out, 'wb').write(('\n'.join(L) + '\n').encode('utf-8'))

if __name__ == '__main__':
    main()
