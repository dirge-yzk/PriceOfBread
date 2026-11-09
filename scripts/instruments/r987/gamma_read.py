# (987) c89tc 読むだけの台本：土地税の算定の基準 A-2b-1c〈規則の選び〉の紙の数え直し（盤面 m981・keep_981 の 4 腕）。
# 物差しは draft.md §0（prereg_s0.md に写した）＝φ* の影（s＝0.12・q≥20 だけ動かす）・下の門（0.5 をまたいだ種が 1 本以上）・上の門（全長の Σ|Δφ*| の 10 種の中央≦5）。
# 使い方：python -X utf8 -I gamma_read.py <書き出すファイル> <腕の名前>=<basisx_s*.tsv と reform_s*.tsv と modsig_s*.tsv のあるフォルダ> [...]
# エンジンの関数を呼ばない・生は読むだけ・新しい係数を置かない（0.5 は下の門の数え方の境＝(979) §6 のまま・s は Coefficients.cs:506 の値を写しただけ）。
import sys, os, glob, statistics

ANCHOR = {'フランス': ('P->F', 1790), 'プロイセン': ('P->F', 1861), '日本': ('P->F', 1873), '清': ('F->P', 1941)}
OPEN_FIXED = {'清', 'イギリス'}   # 名簿で Fixed の国（world.json の landTaxBasis）
OPEN_Q = 20; S = 0.12; UP = 5.0; WIN = 5
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

def files(ins, pre):
    return sorted(glob.glob(os.path.join(ins, pre + '*.tsv')), key=lambda p: seed_of(p, pre))

def series(ins):
    """nation -> list of dict(seed, q, y, pi, R, fz, rfz, tA, tB, st, serf, lref)"""
    out = {}
    for p in files(ins, 'basisx_s'):
        sd = seed_of(p, 'basisx_s')
        rf = {}
        rp = os.path.join(ins, 'reform_s%d.tsv' % sd)
        if os.path.exists(rp):
            for r in load(rp): rf[(int(r['quarter']), r['nation'])] = (num(r['serfLawCode']), num(r['landReformLawCode']))
        byn = {}
        for r in load(p): byn.setdefault(r['nation'], []).append(r)
        for nat, rs in byn.items():
            rs.sort(key=lambda r: int(r['quarter']))
            q = [int(r['quarter']) for r in rs]
            out.setdefault(nat, []).append(dict(seed=sd, q=q, y=[int(r['year']) for r in rs],
                pi=[num(r['bxExpInflRulePerYear']) for r in rs], R=[num(r['bxRBase']) for r in rs],
                fz=[num(r['bxFrozenPbPerMonth']) for r in rs], rfz=[num(r['bxRentOfFrozenPbPerMonth']) for r in rs],
                tA=[num(r['bxTargetA']) for r in rs], tB=[num(r['bxTargetB']) for r in rs],
                st=[int(num(r['bxStance'])) if num(r['bxStance']) == num(r['bxStance']) else -1 for r in rs],
                serf=[rf.get((qq, nat), (NAN, NAN))[0] for qq in q], lref=[rf.get((qq, nat), (NAN, NAN))[1] for qq in q]))
    return out

def follow(T, q, start):
    """φ*_{q+1}＝φ*＋s×(T−φ*)。q<OPEN_Q の行では動かさない。T が nan の行は動かさない。"""
    phi = start; s = []
    for t, qq in zip(T, q):
        if qq >= OPEN_Q and t == t: phi = phi + S * (t - phi)
        s.append(phi)
    return s

def metrics(ph, q):
    cross = 0; tv = 0.0
    for i in range(1, len(ph)):
        if q[i - 1] < OPEN_Q: continue
        if (ph[i - 1] >= 0.5) != (ph[i] >= 0.5): cross += 1
        tv += abs(ph[i] - ph[i - 1])
    return cross, tv

def gates(ms):
    """ms＝種ごとの (cross, tv)。下の門＝cross≥1 の種が 1 本以上・上の門＝tv の中央≦UP"""
    if not ms: return None, None, NAN, 0
    lo = sum(1 for c, _ in ms if c >= 1)
    med = statistics.median([t for _, t in ms])
    return lo >= 1, med <= UP, med, lo

def win_switch(ph, q, y, d, ay):
    n = 0
    for i in range(1, len(ph)):
        if q[i] < OPEN_Q or abs(y[i] - ay) > WIN: continue
        a, b = ph[i - 1], ph[i]
        if d == 'P->F' and a < 0.5 <= b: n += 1
        if d == 'F->P' and a >= 0.5 > b: n += 1
    return n

def clamp01(x): return min(1.0, max(0.0, x))

def t_gamma(R, pi, f):
    if not (R == R and pi == pi and f == f): return NAN
    den = R * (1 + pi)
    return clamp01(f / den) if den > 0 else 1.0

# ---- 規則ごとの T の列（s は 1 本の種の dict・nat は国） ----
def T_A(s, nat): return s['tA']
def T_B(s, nat): return s['tB']
def T_alpha(s, nat):   # α は φ* に依る＝follow の中で書けないので特別扱い（None を返す）
    return None
def T_gamma_main(s, nat):
    """純粋な γ（主の行）：開幕で定額の国は f＝最初の行の bxRBase・R＝bxRBase。他の国は駆動なし＝T＝0。"""
    if nat not in OPEN_FIXED: return [0.0] * len(s['q'])
    f = s['R'][0]
    return [t_gamma(r, x, f) for r, x in zip(s['R'], s['pi'])]
def T_gamma_eng(s, nat):
    """γ′：f・R をエンジンの凍結の写しと同じ RGO の今の地代に。"""
    if nat not in OPEN_FIXED: return [0.0] * len(s['q'])
    return [t_gamma(r, x, f) if r > 0 else NAN for f, r, x in zip(s['fz'], s['rfz'], s['pi'])]

def B_stance(s): return [1.0 if st == 0 else 0.0 for st in s['st']]
def B_serf(s): return [v / 2.0 if v == v else NAN for v in s['serf']]
def B_serfland(s): return [max(a, b) / 2.0 if (a == a and b == b) else NAN for a, b in zip(s['serf'], s['lref'])]

def bundle(form, Bf):
    def T(s, nat):
        B = Bf(s); out = []
        fixed0 = nat in OPEN_FIXED
        f = s['R'][0] if fixed0 else NAN; latched = fixed0
        for b, r, x, qq in zip(B, s['R'], s['pi'], s['q']):
            if form == 'W':   # 束の行が毎四半期 T を書く（他の税の目標と同じ口）
                out.append(b); continue   # 開幕で定額の国も束が 0 なら比例へ戻す（束が戻す側の駆動にもなる形＝そのまま数える）
            if not latched and b == b and b > 0 and r == r and qq >= OPEN_Q:   # f は助走の後の最初の行で置く（エンジンの凍結が助走の末なのと揃える・反証の前の直し）
                latched = True; f = r
            g = t_gamma(r, x, f) if latched else 0.0
            if form == 'P': out.append((b if b == b else 0.0) * g)   # 積＝束が 0 に戻ると T も 0（開幕で定額の国も同じ）
            elif form == 'L': out.append(g)
        return out
    return T

def run_rule(name, Tfn, D, arms, W):
    W('規則\t国\t腕\t下の門(またいだ種/種)\t上の門(Σ|Δφ*| の中央)\t判定(下/上)\t錨の窓の錨の向きの切り替え(種ごと・記録)\t全長の 0.5 またぎの中央')
    summary = {}
    for name_a, _ in arms:
        npass = 0
        for nat, (d, ay) in ANCHOR.items():
            ss = D[name_a].get(nat, []); ms = []; ws = []; fy = []
            for s in ss:
                st = 1.0 if nat in OPEN_FIXED else 0.0
                if name == 'α':
                    phi = st; ph = []
                    for x, qq in zip(s['pi'], s['q']):
                        if qq >= OPEN_Q and x == x: phi = clamp01(phi + S * (clamp01(phi - x) - phi))
                        ph.append(phi)
                else:
                    ph = follow(Tfn(s, nat), s['q'], st)
                ms.append(metrics(ph, s['q'])); ws.append(win_switch(ph, s['q'], s['y'], d, ay))
                up_y = '-'; dn_y = '-'   # 記録（数えた後に足した列・門は替えない）：φ* が 0.5 を最初に上へ・下へまたいだ年
                for i in range(1, len(ph)):
                    if s['q'][i - 1] < OPEN_Q: continue
                    if up_y == '-' and ph[i - 1] < 0.5 <= ph[i]: up_y = str(s['y'][i])
                    if dn_y == '-' and ph[i - 1] >= 0.5 > ph[i]: dn_y = str(s['y'][i])
                fy.append(up_y + '/' + dn_y)
            lo, up, med, nlo = gates(ms)
            if lo and up: npass += 1
            W('%s\t%s\t%s\t%d/%d\t%.3g\t%s/%s\t%s\t%g\t%s' % (name, nat, name_a, nlo, len(ms), med, '通る' if lo else '落ちる', '通る' if up else '落ちる',
              ','.join(str(v) for v in ws), statistics.median([c for c, _ in ms]) if ms else NAN, ','.join(fy)))
        summary[name_a] = npass
    W('%s\t両側を通る錨の国の数\t%s' % (name, '\t'.join('%s=%d/4' % (a, summary[a]) for a, _ in arms)))
    W('')
    return summary

def selfcheck(W):
    W('## 0. 自己検査（偽の規則・draft.md §0-2）')
    n = 600; q = list(range(n))
    def mk(fn): return [fn(i) for i in range(n)]
    cases = [('何もしない', mk(lambda i: 0.0), (False, True)),
             ('毎四半期に反転', mk(lambda i: float(i % 2)), (True, False)),
             ('1 回だけ 0→1', mk(lambda i: 1.0 if i >= 100 else 0.0), (True, True)),
             ('往復 2 回', mk(lambda i: 1.0 if (i >= 20 and ((i - 20) // 40) % 2 == 0 and i < 180) else 0.0), (True, True)),
             ('往復 3 回', mk(lambda i: 1.0 if (i >= 20 and ((i - 20) // 40) % 2 == 0 and i < 260) else 0.0), (True, False))]
    ok = True
    for nm, T, exp in cases:
        ph = follow(T, q, 0.0); m = metrics(ph, q)
        lo, up, med, _ = gates([m] * 10)
        good = (lo, up) == exp; ok &= good
        W('%s\t下=%s\t上=%s\tΣ=%.3g\t期待(下,上)=%s\t%s' % (nm, lo, up, med, exp, '合格' if good else '★外れ'))
    W('自己検査：%s' % ('合格' if ok else '★外れ（台本を直す）'))
    W('')
    return ok

def main():
    out = sys.argv[1]; arms = [a.split('=', 1) for a in sys.argv[2:]]
    L = []; W = L.append
    W('# gamma_read（(987) c89tc）：物差しは draft.md §0（prereg_s0.md）。s＝%g・q≥%d・上の門 Σ≦%g・下の門 0.5 をまたいだ種≥1。' % (S, OPEN_Q, UP))
    if not selfcheck(W):
        open(out, 'wb').write(('\n'.join(L) + '\n').encode('utf-8')); sys.exit(1)
    D = {name: series(ins) for name, ins in arms}
    W('## 1. 姿勢の数（bxStance＝0＝近代化の行・modsig の stance＝0 の行）')
    W('腕\t国\t種の数\tbasisx の行\tbxStance＝0 の行\tbxStance＝-1 の行\tmodsig の行\tmodsig stance＝0\tmodsig の姿勢の内訳(0..4)')
    for name, ins in arms:
        ms = {}
        for p in files(ins, 'modsig_s'):
            for r in load(p):
                v = num(r['stance']); ms.setdefault(r['nation'], []).append(int(v) if v == v else -1)
        allms = [v for vs in ms.values() for v in vs]
        for nat in list(ANCHOR) + ['イギリス', '(全ての国)']:
            if nat == '(全ての国)':
                ss = [s for vs in D[name].values() for s in vs]; mv = allms
            else:
                ss = D[name].get(nat, []); mv = ms.get(nat, [])
            rows = sum(len(s['st']) for s in ss); z = sum(s['st'].count(0) for s in ss); m1 = sum(s['st'].count(-1) for s in ss)
            W('%s\t%s\t%d\t%d\t%d\t%d\t%d\t%d\t%s' % (name, nat, len(ss), rows, z, m1, len(mv), mv.count(0), ','.join(str(mv.count(k)) for k in range(5))))
    W('')
    W('## 2. 束の信号の候補：農奴解放（serfLawCode）と土地改革（landReformLawCode）の段が最初に上がった年（錨の国・種ごと・- は上がらない）')
    W('腕\t国\t開幕の serf/landReform\t農奴解放が最初に上がった年\t土地改革が最初に上がった年\t錨の年')
    for name, _ in arms:
        for nat, (d, ay) in ANCHOR.items():
            ss = D[name].get(nat, []); y1 = []; y2 = []; op = []
            for s in ss:
                op.append('%s/%s' % (('%g' % s['serf'][0]) if s['serf'][0] == s['serf'][0] else '-', ('%g' % s['lref'][0]) if s['lref'][0] == s['lref'][0] else '-'))
                for key, acc in (('serf', y1), ('lref', y2)):
                    v0 = s[key][0]; yy = '-'
                    for v, y in zip(s[key], s['y']):
                        if v == v and v0 == v0 and v > v0: yy = str(y); break
                    acc.append(yy)
            W('%s\t%s\t%s\t%s\t%s\t%d' % (name, nat, ','.join(sorted(set(op))), ','.join(y1), ','.join(y2), ay))
    W('')
    W('## 3. 規則ごとの両側の門（φ* の影・s＝%g・q≥%d）' % (S, OPEN_Q))
    rules = [('A', T_A), ('B', T_B), ('α', None), ('γ（純粋・主）', T_gamma_main), ('γ′（エンジンの凍結の写し）', T_gamma_eng),
             ('束P 姿勢×γ', bundle('P', B_stance)), ('束L 姿勢→γ', bundle('L', B_stance)), ('束W 姿勢', bundle('W', B_stance)),
             ('束P 農奴×γ', bundle('P', B_serf)), ('束L 農奴→γ', bundle('L', B_serf)), ('束W 農奴', bundle('W', B_serf)),
             ('束L 農奴か土地改革→γ', bundle('L', B_serfland))]
    for nm, fn in rules:
        run_rule(nm, fn, D, arms, W)
    W('## 4. 純粋な γ の目標 T_γ が 0.5 を最初に下回った年（開幕で定額の国・種ごと）')
    W('腕\t国\t主（bxRBase）\tγ′（凍結の写し）')
    for name, _ in arms:
        for nat in ('清', 'イギリス'):
            a1 = []; a2 = []
            for s in D[name].get(nat, []):
                for acc, T in ((a1, T_gamma_main(s, nat)), (a2, T_gamma_eng(s, nat))):
                    yy = '-'
                    for t, y, qq in zip(T, s['y'], s['q']):
                        if qq >= OPEN_Q and t == t and t < 0.5: yy = str(y); break
                    acc.append(yy)
            W('%s\t%s\t%s\t%s' % (name, nat, ','.join(a1), ','.join(a2)))
    open(out, 'wb').write(('\n'.join(L) + '\n').encode('utf-8'))

if __name__ == '__main__':
    main()
