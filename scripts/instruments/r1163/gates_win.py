# (1163) 上側の門の空振り（事前登録の下書き §2）：窓＝四半期 0〜79・種の組 5 つ・ヌル腕 3 本の一つ抜き。表を読むだけ・エンジンを呼ばない。
# 使い方: python -X utf8 -I gates_win.py <出力.txt>
import sys, os, collections, statistics as st, math
sys.stdout.reconfigure(newline='\n')
WIN = 80
ARMS = {'M57': 'D:/PoB_work/keep_1157/base_ins', 'N1_50': 'D:/PoB_work/keep_1150/n1', 'N2_50': 'D:/PoB_work/keep_1150/n2',
        'CTL': 'D:/PoB_work/keep_1157_rej/c113cr_mut/CTL/ins'}
PAIRS = [(1, 2), (3, 4), (5, 6), (7, 8), (9, 10)]
NULLS = ['M57', 'N1_50', 'N2_50']

def fl(x):
    try: return float(x)
    except ValueError: return float('nan')

def load_seed(d, s, eng):
    """1 種の窓の値：U1・U2・U3 の四半期ごとの値と、C5 の ΣO÷ΣL・maxO÷ΣO の四半期ごとの値・G1′ と C15 の材料（rponset_s）。"""
    war = set()
    with open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: j for j, k in enumerate(h)}
        for line in f:
            c = line.rstrip('\n').split('\t'); q = int(c[0])
            if q >= WIN: break
            if fl(c[ix['atWarMonths']]) > 0: war.add((q, c[ix['nation']]))
    u5 = collections.defaultdict(list); u1 = collections.defaultdict(list); u2 = collections.Counter(); u3 = collections.Counter(); nat = collections.defaultdict(dict)
    nrows = collections.Counter(); natnames = set()
    with open(os.path.join(d, f'rulepower_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: j for j, k in enumerate(h)}
        iO = ix['oEngNationIndex'] if eng else ix['oNationIndex']
        for line in f:
            c = line.rstrip('\n').split('\t'); q = int(c[0])
            if q >= WIN: break
            n = c[ix['nation']]; natnames.add(n); x = fl(c[ix['nowRule2Share']])
            if not math.isnan(x):
                u1[q].append(x)
                if c[ix['tier']] == 'Core' and (q, n) not in war:
                    u2[q] += x
                    if x >= 0.15: u3[q] += 1
                    x0 = fl(c[ix['nowRule0Share']])
                    if not math.isnan(x0): u5[q].append(x0)
            nrows[q] += 1
            v = nat[q].get(n)
            if v is None: nat[q][n] = v = [fl(c[ix['lNationIndex']]), fl(c[ix['wNationIndex']]), 0.0, fl(c[iO])]
            pk = fl(c[ix['popK']]); v[2] += pk if not math.isnan(pk) else 0.0
    c5 = {}
    for q, nn in nat.items():
        L = sum(v[0] for v in nn.values()); P = sum(v[2] for v in nn.values())
        if not (L > 0 and P > 0): continue
        if eng:
            Os = [v[3] for v in nn.values() if not math.isnan(v[3])]
        else:
            qt = L / P; Os = []
            for l, w, p, _ in nn.values():
                wu = min(w, l); Os.append(max(0.0, l - wu - min(max(0.0, l - wu), p * qt)))
        so = sum(Os); c5[q] = (so / L, max(Os) / so if so > 0 else 0.0)
    # rponset_s：G1′（classHead＝1・Core・ownerAtWarFlag＝0・sEnd≧0.25・データの州でない）と C15（tailFlag＝1・データの州でない）
    g1 = collections.defaultdict(set); tail = collections.Counter()
    with open(os.path.join(d, f'rponset_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: j for j, k in enumerate(h)}
        for line in f:
            c = line.rstrip('\n').split('\t'); q = int(c[0])
            if q >= WIN: break
            if c[ix['rowKind']] != 'pair' or c[ix['dataStateFlag']] == '1': continue
            if c[ix['tailFlag']] == '1': tail[q] += fl(c[ix['tailMass']])
            if c[ix['classHead']] == '1' and c[ix['tier']] == 'Core' and c[ix['ownerAtWarFlag']] == '0' and fl(c[ix['sEnd']]) >= 0.25: g1[q].add(c[ix['region']])
    qs = range(WIN)
    return {
        'U1': st.mean(st.mean(u1[q]) for q in qs if u1[q]),
        'U2': sum(u2[q] for q in qs) / WIN,
        'U5': st.mean(st.mean(u5[q]) for q in qs if u5[q]),
        'U3': max((u3[q] for q in qs), default=0),
        'G1p': max((len(g1[q]) for q in qs), default=0),
        'C15': sum(tail.values()) / WIN,
        'c5': c5, 'rows': sum(nrows.values()), 'war': len(war), 'nat': len(natnames),
    }

def upper(vals):
    mx, mn = max(vals), min(vals)
    if not (mn > 0): return 2 * mx
    return mx * (1 + 2 * (mx / mn - 1))

data = {}
for a, d in ARMS.items():
    for s in (range(1, 3) if a == 'CTL' else range(1, 11)):
        data[(a, s)] = load_seed(d, s, a == 'CTL')

def armval(a, pr, k):
    v = [data[(a, s)][k] for s in pr]
    return sum(v) if k in ('U3', 'G1p') else st.mean(v)   # U3・G1′ は種ごとの最大の和（2 回目の G1′ と同じ）・他は種の平均

out = []
P = lambda *x: out.append(' '.join(str(y) for y in x))
P('# (1163) 窓 0〜79・種の組 5・ヌル腕 3 本の一つ抜き。値は組ごと（U1・U2・C15＝種の平均・U3・G1′＝種の和）')
P('# 行の数（種 1）：' + ' '.join(f"{a}={data[(a, 1)]['rows']}・交戦の国×四半期 {data[(a, 1)]['war']}・国 {data[(a, 1)]['nat']}" for a in ARMS))
KEYS = ['U1', 'U2', 'U3', 'G1p', 'C15']
for k in KEYS:
    P(f'## {k}')
    for pr in PAIRS:
        P(f'  組 {pr}: ' + ' '.join(f'{a}={armval(a, pr, k):.6g}' for a in NULLS))
    miss = 0; trials = []
    for pr in PAIRS:
        for a in NULLS:
            others = [armval(b, pr, k) for b in NULLS if b != a]
            th = upper(others); v = armval(a, pr, k); hit = v > th * (1 + 1e-12)
            miss += hit; trials.append(f'{a}{pr}:{v:.4g}>{th:.4g}' if hit else '')
    P(f'  空振り {miss}/15  ' + ' '.join(t for t in trials if t))
    ctl = armval('CTL', (1, 2), k); th3 = upper([armval(b, (1, 2), k) for b in NULLS])
    P(f'  CTL 組 (1, 2) {ctl:.6g}・閾値（3 本） {th3:.6g}・{"赤" if ctl > th3 else "緑"}')
# U4＝新÷旧（基準の役を Y・試しを X・閾値は残りの Z と Y の比 Z/Y・Y/Z の上側）
for k in ('U1', 'U2'):
    P(f'## U4（{k} の新÷旧）')
    miss = 0; n = 0; det = []
    for pr in PAIRS:
        for x in NULLS:
            for y in NULLS:
                if y == x: continue
                z = [b for b in NULLS if b not in (x, y)][0]
                r = armval(x, pr, k) / armval(y, pr, k); zy = armval(z, pr, k) / armval(y, pr, k)
                th = upper([zy, 1 / zy]); n += 1; hit = r > th * (1 + 1e-12); miss += hit
                if hit: det.append(f'{x}/{y}{pr}:{r:.4g}>{th:.4g}')
    P(f'  空振り {miss}/{n}  ' + ' '.join(det))
    rs = [armval('CTL', (1, 2), k) / armval(y, (1, 2), k) for y in NULLS]
    nr = [armval(a, (1, 2), k) / armval(b, (1, 2), k) for a in NULLS for b in NULLS if a != b]
    P(f'  CTL 組 (1, 2) の新÷旧（基準 3 本）{[round(v, 4) for v in rs]}・閾値（ヌル腕どうしの比 6 つの上側）{upper(nr):.4g}')
# U5＝平時の中核の州の持ち主の取り分 nowRule0Share の平均（下側の門・下側＝最小×(1−2×(幅−1))・新÷旧も）
def lower(vals):
    mx, mn = max(vals), min(vals)
    return mn * (1 - 2 * (mx / mn - 1)) if mn > 0 else 0.0
P('## U5（下側）')
for pr in PAIRS:
    P(f'  組 {pr}: ' + ' '.join(f"{a}={armval(a, pr, 'U5'):.6g}" for a in NULLS))
miss = 0; det = []
for pr in PAIRS:
    for a in NULLS:
        th = lower([armval(b, pr, 'U5') for b in NULLS if b != a]); v = armval(a, pr, 'U5')
        if v < th * (1 - 1e-12): miss += 1; det.append(f'{a}{pr}:{v:.4g}<{th:.4g}')
P(f'  空振り {miss}/15  ' + ' '.join(det))
ctl = armval('CTL', (1, 2), 'U5'); th3 = lower([armval(b, (1, 2), 'U5') for b in NULLS])
P(f'  CTL 組 (1, 2) {ctl:.6g}・閾値（3 本） {th3:.6g}・{"赤" if ctl < th3 else "緑"}・新÷旧 {[round(ctl / armval(y, (1, 2), "U5"), 4) for y in NULLS]}')
# C5・C9′ の窓の平均の形（両側・一つ抜き）：組の 2 種の平均の窓の平均を、残りの 2 本の同じ組の値の上側・下側に当てる
for i, nm in ((0, 'C5agg ΣO÷ΣL'), (1, 'C9agg maxO÷ΣO')):
    def av(a, pr): return st.mean(st.mean(data[(a, s)]['c5'][q][i] for q in range(WIN)) for s in pr)
    miss = 0; det = []
    for pr in PAIRS:
        for a in NULLS:
            ot = [av(b, pr) for b in NULLS if b != a]; v = av(a, pr)
            if v > upper(ot) * (1 + 1e-12) or v < lower(ot) * (1 - 1e-12): miss += 1; det.append(f'{a}{pr}:{v:.4g}')
    ctl = av('CTL', (1, 2)); n3 = [av(b, (1, 2)) for b in NULLS]
    P(f'## {nm}（窓の平均・両側）空振り {miss}/15 {" ".join(det)}・CTL {ctl:.4g}・幅（3 本）{lower(n3):.4g}〜{upper(n3):.4g}・{"赤" if not (lower(n3) <= ctl <= upper(n3)) else "緑"}・ヌル腕 組 (1, 2) {[round(x, 4) for x in n3]}')
# C5〈壊さない〉・C9′：試しの組の 2 種の中央が、残りの 2 本の帯（(a) 10 種の最小〜最大・(b) 同じ 2 種の最小〜最大）の外の四半期の数
for i, (nm, th) in enumerate((('C5 ΣO÷ΣL', 1), ('C9′ maxO÷ΣO', 2))):
    for mode in ('a', 'b'):
        P(f'## {nm}（帯 {mode}・門 ≦{th}）')
        miss = 0; det = []
        for pr in PAIRS:
            for a in NULLS:
                others = [b for b in NULLS if b != a]; nout = 0
                for q in range(WIN):
                    m = st.median(data[(a, s)]['c5'][q][i] for s in pr)
                    seeds_b = range(1, 11) if mode == 'a' else pr
                    vals = [data[(b, s)]['c5'][q][i] for b in others for s in seeds_b]
                    nout += not (min(vals) <= m <= max(vals))
                miss += nout > th; det.append(nout)
        P(f'  空振り {miss}/15・幅の外の四半期 {det}')
        nouts = []
        for pair in (('N1_50', 'N2_50'), ('M57', 'N2_50'), ('M57', 'N1_50')):
            nout = 0
            for q in range(WIN):
                m = st.median(data[('CTL', s)]['c5'][q][i] for s in (1, 2))
                seeds_b = range(1, 11) if mode == 'a' else (1, 2)
                vals = [data[(b, s)]['c5'][q][i] for b in pair for s in seeds_b]
                nout += not (min(vals) <= m <= max(vals))
            nouts.append(nout)
        P(f'  CTL の幅の外 {nouts}（最大 {max(nouts)}）')
with open(sys.argv[1], 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(out) + '\n')
