# (971) c87hd 門の台本（読むだけ・エンジンの関数を呼ばない・旗を読まない・事前登録 measurements/m971_20261108_prereg_hd.txt [4] の定義をそのまま）
#   python -X utf8 -I gate971.py <名>=<ins フォルダ> ... [--seeds 1 10]
#   [A] W1・W2・U1・U2＝c82hd/gate936.py をそのまま呼ぶ（定義は 1 字も変えない）
#   [B] 記録：Σ billWithHeirDebtClearings（旗 0＝相続人の債務があるのに手形を振り出した清算／旗 1＝その条件の清算＝門が閉じた清算〔計器は旗を読まない＝意味が反転する〕）・Σ heirDebtAfterSettleSum
#   [C] 塵の帯：cpop_s の人の居る POP（popK ＞1e-12）の行の債務を (0,1e-12]・(1e-12,1e-9]・＞1e-9 に分けて数える／州×四半期の人の居る POP の債務の和を同じ帯で数える
#   [D] 底の比（上の門・尾）：nation_s の priceLevel ≦0.4＋1e-6 が 4 四半期以上続く区間（F・r950_type.py と同じ定義）ごとに、
#       区間の中で国の州の貯蓄の和（cearn_s の savings）が最小の四半期（貯蓄 ＞0）の outImport÷貯蓄（imp）と（outImport−inExport）÷貯蓄（net）。
#       区間の値の最大を盤面の値にする（全ての F・分かれた国〔cederule_s の how＝Seceded・tierBefore＝Colonial の receiver〕を除いた〈それ以外〉の F の両方）。heirSet は足さない。
#   [E] ① の記録：1810 年より前の植民地格の州の分離（cederule_s の how＝Seceded・tierBefore＝Colonial）の行の数（送り手ごと）
import sys, os, subprocess
args = sys.argv[1:]
s0, s1 = 1, 10
if '--seeds' in args:
    i = args.index('--seeds'); s0, s1 = int(args[i + 1]), int(args[i + 2]); del args[i:i + 3]
HERE = os.path.dirname(os.path.abspath(__file__))
G936 = os.path.join(HERE, '..', 'c82hd', 'gate936.py')
DEAD = 1e-12; FLOOR = 0.4 + 1e-6; K = 4
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
def runs(qs, k):
    out = []; st = pv = None
    for q in qs:
        if st is not None and q == pv + 1: pv = q; continue
        if st is not None and pv - st + 1 >= k: out.append((st, pv))
        st = pv = q
    if st is not None and pv - st + 1 >= k: out.append((st, pv))
    return out
for tag, d in [a.split('=', 1) for a in args]:
    print(f'===== {tag}・{d}・種 {s0}〜{s1}')
    r = subprocess.run([sys.executable, '-X', 'utf8', '-I', G936, f'{tag}={d}', '--seeds', str(s0), str(s1)], capture_output=True, text=True, encoding='utf-8')
    print('[A] gate936：'); print(r.stdout.rstrip()); sys.stdout.flush()
    bw = bwrows = 0; hds = 0.0
    band = [0, 0, 0]; rband = [0, 0, 0]
    Bimp = []; Bnet = []; early = {}; ncearn = 0
    for s in range(s0, s1 + 1):
        pc = os.path.join(d, f'cearn_s{s}.tsv'); pp = os.path.join(d, f'cpop_s{s}.tsv'); pn = os.path.join(d, f'nation_s{s}.tsv'); pr = os.path.join(d, f'cederule_s{s}.tsv')
        sec = set()
        if os.path.exists(pr):
            with open(pr, encoding='utf-8') as f:
                h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
                for line in f:
                    x = line.rstrip('\n').split('\t')
                    if x[ix['how']] == 'Seceded' and x[ix['tierBefore']] == 'Colonial':
                        sec.add(x[ix['receiver']])
                        if int(x[ix['year']]) < 1810: early[x[ix['sender']]] = early.get(x[ix['sender']], 0) + 1
        agg = {}
        if os.path.exists(pc):
            ncearn += 1
            with open(pc, encoding='utf-8') as f:
                h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
                iq, inn, isv, ioi, iie, ibw, ihd = ix['quarter'], ix['nation'], ix['savings'], ix['outImportPerQuarter'], ix['inExportPerQuarter'], ix['billWithHeirDebtClearings'], ix['heirDebtAfterSettleSum']
                for line in f:
                    x = line.rstrip('\n').split('\t')
                    n = int(x[ibw]); bw += n
                    if n: bwrows += 1
                    v = fl(x[ihd]); hds += v if v == v else 0.0
                    a = agg.setdefault((x[inn], int(x[iq])), [0.0, 0.0, 0.0])
                    for j, i in enumerate((isv, ioi, iie)):
                        v = fl(x[i]); a[j] += v if v == v else 0.0
        if os.path.exists(pp):
            with open(pp, encoding='utf-8') as f:
                h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
                iq, ir, ipk, idb = ix['quarter'], ix['region'], ix['popK'], ix['debt']
                cur = None; acc = 0.0
                def rb(acc):
                    if acc > 1e-9: rband[2] += 1
                    elif acc > 1e-12: rband[1] += 1
                    elif acc > 0: rband[0] += 1
                for line in f:
                    x = line.rstrip('\n').split('\t')
                    k = (x[iq], x[ir])
                    if k != cur:
                        if cur is not None: rb(acc)
                        cur = k; acc = 0.0
                    if fl(x[ipk]) > DEAD:
                        db = fl(x[idb])
                        if db > 1e-9: band[2] += 1
                        elif db > 1e-12: band[1] += 1
                        elif db > 0: band[0] += 1
                        if db == db: acc += db
                if cur is not None: rb(acc)
        if os.path.exists(pn) and agg:
            byn = {}
            with open(pn, encoding='utf-8') as f:
                h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
                for line in f:
                    x = line.rstrip('\n').split('\t')
                    if fl(x[ix['priceLevel']]) <= FLOOR: byn.setdefault(x[ix['nation']], []).append(int(x[ix['quarter']]))
            for n, qs in byn.items():
                for st, en in runs(sorted(qs), K):
                    best = None
                    for q in range(st, en + 1):
                        a = agg.get((n, q))
                        if a is None or not (a[0] > 0): continue
                        if best is None or a[0] < best[0]: best = (a[0], a[1], a[2], q)
                    if best is None: continue
                    sv, oi, ie, q = best
                    Bimp.append((oi / sv, n in sec, s, n, st, en, q)); Bnet.append(((oi - ie) / sv, n in sec, s, n, st, en, q))
    print(f'[B] 記録 Σ billWithHeirDebtClearings {bw}（＞0 の行 {bwrows}）・Σ heirDebtAfterSettleSum {hds:.6g}・cearn の種 {ncearn}')
    print(f'[C] 塵の帯 人の居る POP の行 (0,1e-12] {band[0]}・(1e-12,1e-9] {band[1]}・＞1e-9 {band[2]}／州×四半期の和 (0,1e-12] {rband[0]}・(1e-12,1e-9] {rband[1]}・＞1e-9 {rband[2]}')
    def top(L, cond):
        v = sorted((x for x in L if cond(x)), key=lambda x: -x[0])
        return v
    for nm, L in (('imp', Bimp), ('net', Bnet)):
        al = top(L, lambda x: True); ot = top(L, lambda x: not x[1])
        f = lambda v: f'{v[0][0]:.6g}（種 {v[0][2]} {v[0][3]} F {v[0][4]}〜{v[0][5]} 底 Q{v[0][6]}）' if v else 'nan（F なし）'
        print(f'[D] 底の比 {nm}：F の区間 {len(al)}（分かれた国 {len(al) - len(ot)}・それ以外 {len(ot)}）・全ての F の最大 {f(al)}・それ以外の最大 {f(ot)}')
        print(f'    上位 5（全て）：' + '・'.join(f'{x[0]:.4g}/{"分" if x[1] else "他"}/種{x[2]}/{x[3]}' for x in al[:5]))
    print(f'[E] ① 1810 年より前の植民地格の州の分離（送り手：行）：' + ('・'.join(f'{k}:{v}' for k, v in sorted(early.items())) or '0'))
    sys.stdout.flush()
