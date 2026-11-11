# (1031) c97mg 商人の門の逆向き（merchFallGateIn=1 かつ merchFallOut<=1e-9）の帰属（読むだけ）
# 使い方：python -X utf8 -I mg1031.py <ins フォルダ> <種の一覧 例 1-10> <出力の頭> [cpop を読む 1/0]
# 区分の定義は D:/PoB_work/r1031_work/prereg.txt
import sys, os, csv, collections, math

d = sys.argv[1]
lo, hi = (int(x) for x in sys.argv[2].split('-'))
outp = sys.argv[3]
use_cpop = len(sys.argv) > 4 and sys.argv[4] == '1'
csv.field_size_limit(1 << 30)
EPS = 1e-9
RATE, F, THR = 0.03, 0.3, 0.4   # MiddleFallRatePerQuarter・FallAnchorReform・MiddleFallThreshold（Coefficients.cs:721・722・835）

def fnum(s):
    try:
        return float(s)
    except ValueError:
        return float('nan')

cnt = collections.Counter()
rev = []   # 逆向きの行
for s in range(lo, hi + 1):
    p = os.path.join(d, f'merch_s{s}.tsv')
    with open(p, encoding='utf-8', newline='') as f:
        rd = csv.reader(f, delimiter='\t')
        h = next(rd); ix = {n: i for i, n in enumerate(h)}
        for r in rd:
            tier = r[ix['tier']]
            gate = r[ix['merchFallGateIn']]
            fo = fnum(r[ix['merchFallOut']])
            cnt[('rows', tier)] += 1
            cnt[('gate_' + gate, tier)] += 1
            if fo > EPS:
                cnt[('fell', tier)] += 1
                if gate != '1': cnt[('fellGateNot1', tier)] += 1
            if gate == '1' and not (fo > EPS):
                mk = fnum(r[ix['merchantK']]); pin = fnum(r[ix['merchPettyIn']]); cro = fnum(r[ix['merchCapRiseOut']])
                sg = mk + pin - cro
                rev.append(dict(seed=s, q=r[ix['quarter']], year=r[ix['year']], nation=r[ix['nation']], region=r[ix['region']], tier=tier,
                                mk=mk, pin=pin, cro=cro, sg=sg, fo=fo, wk=fnum(r[ix['workerK']]), popk=fnum(r[ix['popK']]),
                                avg=fnum(r[ix['avgPerCap']]), pc=fnum(r[ix['merchSavPerCapAtGate']]),
                                me=fnum(r[ix['merchEarnedPerCap']]), eme=fnum(r[ix['emptyMerchantEarnedPerMonth']]),
                                debt=fnum(r[ix['merchantDebt']]), mempty=r[ix['merchantEmpty']]))

# cpop：植民地格の逆向きの行の (種, 四半期, 国, 州) の POP の頭数
low = {}
keys = {(x['seed'], x['q'], x['nation'], x['region']) for x in rev}
cls_seen = collections.Counter()
if use_cpop:
    for s in range(lo, hi + 1):
        p = os.path.join(d, f'cpop_s{s}.tsv')
        if not os.path.exists(p): continue
        with open(p, encoding='utf-8', newline='') as f:
            rd = csv.reader(f, delimiter='\t')
            h = next(rd); ix = {n: i for i, n in enumerate(h)}
            for r in rd:
                k = (s, r[ix['quarter']], r[ix['nation']], r[ix['region']])
                if k not in keys: continue
                cn = r[ix['className']]; pk = fnum(r[ix['popK']])
                cls_seen[cn] += 1
                e = low.setdefault(k, {})
                e[cn] = pk

LOWER = ('農民', '工場労働者', '使用人')
def classify(x):
    sg = x['sg']
    # 流量の帯
    if not (sg > EPS):
        return 'x_門の外の人数（矛盾）'
    if RATE * sg <= EPS:
        return 'c_流量Eps以下（確実）'
    if not (RATE * (1 - F) * sg > EPS):
        # 消去法：(b)(d)＝0（ソース）・fo＝0・下流あり（cpop）＝(a) でない＝残るのは (c) だけ
        kk = (x['seed'], x['q'], x['nation'], x['region'])
        if x['tier'] == 'Colonial' and use_cpop and x['fo'] == 0 and kk in low and any(low[kk].get(c, 0.0) > EPS for c in LOWER):
            return 'c_流量Eps以下（消去法で確実）'
        # 間＝depth を近似（所得は当月だけ・前月の欠け）
        E = x['me'] * x['mk'] if x['mk'] > EPS else x['eme']
        ann = 12.0 * max(0.0, E if E == E else 0.0) / max(EPS, sg)
        thr = THR * ((1 - F) * x['avg'] + F * ann)
        dep = min(1.0, max(0.0, (thr - x['pc']) / thr)) if thr > EPS else 1.0
        flow = RATE * ((1 - F) + F * dep) * sg
        return 'c_流量Eps以下（近似）' if flow <= EPS else 'c?_帯（近似で流量>Eps）'
    k = (x['seed'], x['q'], x['nation'], x['region'])
    if x['tier'] == 'Colonial' and use_cpop:
        e = low.get(k)
        if e is None:
            return 'u_cpopの行なし'
        if all(not (e.get(c, 0.0) > EPS) for c in LOWER):
            return 'a_下流が空（cpop）'
        return 'e_それ以外（下流あり）'
    if x['wk'] > EPS:
        return 'e_それ以外（労働者あり）'
    return 'u_判定できない（本国格・労働者0）'

lowerPresent = collections.Counter()
for x in rev:
    if x['tier'] == 'Colonial' and use_cpop:
        kk = (x['seed'], x['q'], x['nation'], x['region'])
        e = low.get(kk)
        lowerPresent['cpopなし' if e is None else ('下流あり' if any(e.get(c, 0.0) > EPS for c in LOWER) else '下流が空')] += 1
res = collections.Counter(); bytier = collections.Counter(); byseed = collections.Counter()
sub = collections.Counter()
ex = {}
for x in rev:
    c = classify(x)
    x['cls'] = c
    res[c] += 1; bytier[(x['tier'], c)] += 1; byseed[(x['seed'], c)] += 1
    sub[('fo0' if x['fo'] == 0 else 'fo_tiny', x['tier'])] += 1
    sub[('生まれたて（mk<=Eps かつ pin>0）' if (not (x['mk'] > EPS) and x['pin'] > 0) else '既存の商人', c)] += 1
    if c.startswith('e') and c not in ex: ex[c] = x

with open(outp + '.txt', 'w', encoding='utf-8') as o:
    P = lambda *a: print(*a, file=o)
    tiers = sorted({t for (_, t) in cnt})
    P(f'(1031) mg1031 生={d} 種={lo}-{hi} cpop={int(use_cpop)}')
    for t in tiers:
        P(f'T0 {t} 行={cnt[("rows", t)]} 門1={cnt[("gate_1", t)]} 門0={cnt[("gate_0", t)]} 門nan={cnt[("gate_nan", t)]} 落ちた(fo>1e-9)={cnt[("fell", t)]} 落ちたのに門が1でない={cnt[("fellGateNot1", t)]}')
    N = len(rev)
    P(f'T1 逆向き（門1 かつ fo<=1e-9）={N}  格別：' + '・'.join(f'{t} {sum(1 for x in rev if x["tier"] == t)}' for t in tiers))
    P('T1 fo の内訳：' + '・'.join(f'{k[0]}/{k[1]} {v}' for k, v in sorted(sub.items()) if k[0] in ('fo0', 'fo_tiny')))
    P(f'T2 区分（全体）：和={sum(res.values())}（全体 {N} と一致={sum(res.values()) == N}）')
    for c, v in sorted(res.items()):
        P(f'T2 {c} {v} ({v / N:.4f})' if N else f'T2 {c} {v}')
    P('T3 格×区分')
    for t in tiers:
        tot = sum(v for (tt, c), v in bytier.items() if tt == t)
        P(f'T3 {t} 計={tot}  ' + '・'.join(f'{c} {v}' for (tt, c), v in sorted(bytier.items()) if tt == t))
    P('T4 種×区分')
    for s in range(lo, hi + 1):
        tot = sum(v for (ss, c), v in byseed.items() if ss == s)
        P(f'T4 種{s} 計={tot}  ' + '・'.join(f'{c} {v}' for (ss, c), v in sorted(byseed.items()) if ss == s))
    P('T5 生まれたて×区分：' + '・'.join(f'{k[0]}/{k[1]} {v}' for k, v in sorted(sub.items()) if k[0] not in ('fo0', 'fo_tiny')))
    sgs = sorted(x['sg'] for x in rev)
    if sgs:
        qq = lambda f: sgs[min(len(sgs) - 1, int(f * len(sgs)))]
        P(f'T6 門の時点の人数 Sg（千人）の分位：最小 {sgs[0]:.3g}・10% {qq(.1):.3g}・50% {qq(.5):.3g}・90% {qq(.9):.3g}・最大 {sgs[-1]:.3g}')
    if use_cpop:
        P('T7 cpop で見た階級名と件数：' + '・'.join(f'{k} {v}' for k, v in sorted(cls_seen.items())))
        P('T8 植民地格の逆向きの全行の下流（農民・工場労働者・使用人）の有無（区分の順と独立）：' + '・'.join(f'{k} {v}' for k, v in sorted(lowerPresent.items())))
    for c, x in ex.items():
        P(f'EX {c}：' + ' '.join(f'{k}={v}' for k, v in x.items()))
with open(outp + '_rows.tsv', 'w', encoding='utf-8', newline='') as o:
    cols = ['seed', 'q', 'year', 'nation', 'region', 'tier', 'mk', 'pin', 'cro', 'sg', 'fo', 'wk', 'popk', 'avg', 'pc', 'me', 'eme', 'debt', 'mempty', 'cls']
    o.write('\t'.join(cols) + '\n')
    for x in rev:
        o.write('\t'.join(str(x[c]) for c in cols) + '\n')
print('done', N)
