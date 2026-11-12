# (1083) 問い 4 の合わせた紙：開幕 3080 行に ①同点の許容差 ②冬の凍結を外す ③ρ を力の側だけ（Vb）を順に重ねたときの影の最大の区分。
# 使い方: python -X utf8 -I q6_combined.py open_rows.tsv <表のフォルダ> > q6_out.txt
# 式の写し（名乗る）：区分＝RulePowerClasses（InstrumentsRulePower.cs:45〜57）・許容差は相対 1e-9（同点は持ち主）。
# ②の近似：四半期 0 に首都からの km が inf の州は、τ が ρ に比例して解かれる（:244・:252＝τ＝(H÷ΣN)×ρ÷RuleInit[0]）ので、
#   ρ＞0 の州は F_S・F_I・F_U を同じ種の四半期 1 の ρ との比 k＝ρ1÷ρ0 で掛ける（F_R と F_f は変えない＝版 u1／F_f だけ四半期 1 の行の値に替える＝版 u2）。
#   ρ＝0 の州（F_S＝0・τ は国の中央）は τ′＝F_S(四半期 1)÷(N×RuleInit[0]) で解き直す。RuleInit は places.json:267（イカルイト 0.15/0.35/0/0.5）と world.json:899（ペトロパヴロフスク 0.25/0.35/0/0.4）から写した。
# ③の近似：q4_rho_once.py と同じ（ρ̄_n は 1 国だけから受ける行から・複数の国から受ける行は幅）。
import csv, sys, os, collections
import sys as _s; _s.stdout.reconfigure(newline='\n')  # 出力は LF
rows = list(csv.DictReader(open(sys.argv[1], encoding='utf-8'), delimiter='\t')); d = sys.argv[2]
def f(x):
    try: return float(x)
    except: return float('nan')
def classes(fs, fi, fr, fsum, fu, k, tol=1e-9):
    k = 1 if k > 1 else k if k > 0 else 0
    fsum = fsum if fsum > 0 else 0
    tot = fs + fi + fr + fsum + fu
    if not (tot > 0): return 'none'
    o, l, fo, u = (fs + k * fi) / tot, ((1 - k) * fi + fr) / tot, fsum / tot, fu / tot
    best, bv = 'owner', o
    if l > bv * (1 + tol): best, bv = 'local', l
    if fo > bv * (1 + tol): best, bv = 'foreign', fo
    if u > bv * (1 + tol): best = 'ungov'
    return best
RI = {'イカルイト': (0.15, 0.35, 0.0, 0.5), 'ペトロパヴロフスク': (0.25, 0.35, 0.0, 0.4)}
infr = {(x['seed'], x['region']) for x in rows if x['directKm'] == 'inf'}
q1 = {}
for s in range(1, 11):
    fi = open(os.path.join(d, f'rulepower_s{s}.tsv'), encoding='utf-8'); h = fi.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    for line in fi:
        c = line.rstrip('\n').split('\t'); q = int(c[0])
        if q > 1: break
        if q == 1 and (str(s), c[ix['region']]) in infr: q1[(str(s), c[ix['region']])] = {k: c[ix[k]] for k in ('rhoNewIndex', 'fStateIndex', 'fForeignSumIndex', 'directKm')}
# ρ̄_n
alloc = collections.defaultdict(float); inv = collections.defaultdict(float); single = {}
for x in rows:
    sm, tp, rw = f(x['fForeignSumIndex']), f(x['fForeignTopIndex']), f(x['fForeignRawIndex'])
    if sm > 0:
        one = tp >= sm * (1 - 1e-9); single[(x['seed'], x['region'])] = one
        if one and rw > 0: k = (x['seed'], x['foreignTopKey']); alloc[k] += rw; inv[k] += rw / (sm / rw)
rbar = {k: alloc[k] / inv[k] for k in alloc if inv[k] > 0}
rmin = {s: min(v for k, v in rbar.items() if k[0] == s) for s in {k[0] for k in rbar}}
res = collections.Counter(); unf = collections.Counter()
for x in rows:
    key = (x['seed'], x['region'])
    fs, fi_, fr, fu, kap = f(x['fStateIndex']), f(x['fInterIndex']), f(x['fResistIndex']), f(x['fUngovIndex']), f(x['kappaIndex'])
    ff, raw = f(x['fForeignSumIndex']), f(x['fForeignRawIndex'])
    res[('① 許容差', classes(fs, fi_, fr, ff, fu, kap))] += 1
    ff2 = ff
    if key in infr and key in q1 and q1[key]['directKm'] != 'inf':
        r0, r1 = f(x['rhoNewIndex']), f(q1[key]['rhoNewIndex'])
        if r0 > 0:
            k = r1 / r0; fs, fi_, fu = fs * k, fi_ * k, fu * k; unf['ρ＞0 の州（比 k の中央に入る）'] += 1; unf[('k', x['region'])] = round(k, 1)
        else:
            ri = RI[x['region']]; n = f(x['nIndex']); fs1 = f(q1[key]['fStateIndex']); tau = fs1 / (n * ri[0])
            fs, fi_, fu = fs1, n * tau * ri[1] * f(x['interRatioIndex']), n * tau * ri[3]; unf['ρ＝0 の州（τ を解き直す）'] += 1
        ff2 = f(q1[key]['fForeignSumIndex'])
    res[('①＋② u1（F_f は四半期 0）', classes(fs, fi_, fr, ff, fu, kap))] += 1
    res[('①＋② u2（F_f は四半期 1）', classes(fs, fi_, fr, ff2, fu, kap))] += 1
    if ff > 0 and single.get(key):
        lo = hi = raw * rbar[(x['seed'], x['foreignTopKey'])]
    elif ff > 0: lo, hi = raw * rmin[x['seed']], raw
    else: lo = hi = 0.0
    res[('①＋② u1＋③ 下', classes(fs, fi_, fr, lo, fu, kap))] += 1
    res[('①＋② u1＋③ 上', classes(fs, fi_, fr, hi, fu, kap))] += 1
print('凍結を外した行', {k: v for k, v in unf.items() if not isinstance(k, tuple)}, '／ρ の比 k（州：最後の種の値）', {k[1]: v for k, v in unf.items() if isinstance(k, tuple)})
for v in ('① 許容差', '①＋② u1（F_f は四半期 0）', '①＋② u2（F_f は四半期 1）', '①＋② u1＋③ 下', '①＋② u1＋③ 上'):
    print(f'{v}\t' + '\t'.join(f'{b} {res[(v, b)]}' for b in ('owner', 'local', 'foreign', 'ungov', 'none')) + f'\t持ち主でない {3080 - res[(v, "owner")]}')
