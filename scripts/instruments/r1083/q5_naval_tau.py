# (1083) 問い 4・5 の補い：①配りの多重度（1 国から／複数の国から受ける行）②冬に首都からの距離が inf の州の τ（四半期 0 で解いて以後固定）を同じ RuleInit の州と比べる
# ③海軍：navalAllocIndex＞0 の行の数と、海軍を F_f に足した版（記録）で区分が変わる行の数（四半期 0・100・300・600）④国ごとの 海軍力÷陸軍力（同じ表の 2 列・種の中央）
# 式の写し（名乗る）：区分＝RulePowerClasses（InstrumentsRulePower.cs:45〜57）・持ち主と無統治の比べに相対の許容差 1e-9（同点は持ち主）。海軍を足す版＝ΣF_f＋navalAllocIndex×1（配りの重みに ρ が入っている＝ρ 1 回の形）。
# 使い方: python -X utf8 -I q5_naval_tau.py <表のフォルダ> > q5_out.txt
import sys, os, collections, statistics as st
import sys as _s; _s.stdout.reconfigure(newline='\n')  # 出力は LF
d = sys.argv[1]; QS = [0, 100, 300, 600]
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
def fl(x):
    try: return float(x)
    except: return float('nan')
mult = collections.Counter(); tau = collections.defaultdict(list); nav = collections.defaultdict(lambda: collections.Counter())
ratio = collections.defaultdict(lambda: collections.defaultdict(list)); moved = collections.defaultdict(lambda: collections.Counter())
INF = {'カナダ', 'ナイン', 'イカルイト', 'アラスカ', 'セントマイケル', 'バロー', 'フォートユーコン', 'ペトロパヴロフスク'}
SAME = {'バンクーバー', 'エドモントン', 'フォートシンプソン', 'チャーチル', 'フォートウィリアム', 'ヌーク'}
for s in range(1, 11):
    fi = open(os.path.join(d, f'rulepower_s{s}.tsv'), encoding='utf-8'); h = fi.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    seen = set()
    for line in fi:
        c = line.rstrip('\n').split('\t'); q = int(c[0])
        if q > max(QS): break
        if q not in QS: continue
        g = lambda k: fl(c[ix[k]])
        if q == 0:
            sm, tp = g('fForeignSumIndex'), g('fForeignTopIndex')
            if sm > 0: mult['1 国だけから' if tp >= sm * (1 - 1e-9) else '複数の国から'] += 1
            r = c[ix['region']]
            if r in INF or r in SAME: tau[('冬に inf' if r in INF else '同じ RuleInit で inf でない', c[ix['nation']], r)].append(g('tauIndex'))
        na = g('navalAllocIndex')
        nav[q]['行'] += 1
        if na > 0: nav[q]['navalAlloc＞0'] += 1
        b0 = classes(g('fStateIndex'), g('fInterIndex'), g('fResistIndex'), g('fForeignSumIndex'), g('fUngovIndex'), g('kappaIndex'))
        bn = classes(g('fStateIndex'), g('fInterIndex'), g('fResistIndex'), g('fForeignSumIndex') + (na if na > 0 else 0), g('fUngovIndex'), g('kappaIndex'))
        if b0 != bn: moved[q][(b0, bn)] += 1
        k = (q, c[ix['nation']])
        if k not in seen:
            seen.add(k); L, N = g('lNationIndex'), g('navalNationIndex')
            ratio[c[ix['nation']]][q].append(N / L if L > 0 else float('nan'))
print('[配りの多重度] 四半期 0 に外国の力を受ける行', dict(mult))
print('\n[冬の凍結と τ] 四半期 0 の τ（以後固定）：10 種の中央')
for k, v in sorted(tau.items()): print(f'  {k[0]}\t{k[1]}\t{k[2]}\tτ {st.median(v):.4g}')
print('\n[海軍] 四半期ごと：行・navalAlloc＞0 の行・海軍を F_f に足すと区分が変わる行（前→後）')
for q in QS: print(f'  {q}\t{nav[q]["行"]}\t{nav[q]["navalAlloc＞0"]}\t{dict(moved[q])}')
print('\n[海軍力÷陸軍力]（国ごと・種の中央）四半期 ' + ' '.join(map(str, QS)))
for n in ('イギリス', 'フランス', 'スペイン', 'オランダ', 'ロシア', 'アメリカ', '日本', '清', 'オスマン', 'プロイセン'):
    print(f'  {n}\t' + '\t'.join((f'{st.median([v for v in ratio[n][q] if v == v]):.3g}' if [v for v in ratio[n][q] if v == v] else '-') for q in QS))
