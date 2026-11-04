#!/usr/bin/env python
# (823) c65sv 紙の判定 P1〜P4（事前登録 measurements/m823_20261104_prereg_sv.txt [5]・統計と閾値は腕の前に固定）。
#   使い方: python scripts/instruments/c65sv/paper823.py <ident の出力/out_ib_10/ins> [出力の tsv]
#   読む表: silverwt_s<種>.tsv（種 1〜10・旗 0 の盤面・助走の注入も含む 1 注入 1 行）。D:/… の形で渡す。
#   判定: A′（relMinRef）と A（relMinVal）を別々に P1〜P4 に当て、対照（落ちるはずの形）が落ちることを同時に確かめる。選び方＝A′ が全て通れば A′・A′ が落ち A が通れば A・両方落ちたら採らない。
import sys, os, glob, math, statistics as st
try: sys.stdout.reconfigure(encoding='utf-8')
except Exception: pass
FLOOR, CEIL = 0.3, 3.0          # Coefficients の ExchangeRateFloor・Ceil（World.cs の挟み）
P2_MAX, P3_MAX, P4_FRAC, P4_SHARE = 0.05, 0.05, 0.01, 0.5

def load(d):
    rows = []
    for f in sorted(glob.glob(os.path.join(d, 'silverwt_s*.tsv'))):
        seed = os.path.basename(f)[len('silverwt_s'):-4]
        with open(f, encoding='utf-8') as fh:
            h = fh.readline().rstrip('\n').split('\t')
            for line in fh:
                p = line.rstrip('\n').split('\t')
                if len(p) != len(h): continue
                r = dict(zip(h, p)); r['_seed'] = seed
                rows.append(r)
    return rows

def num(r, k):
    v = r.get(k)
    if v is None: return float('nan')
    if v == 'nan': return float('nan')
    if v == 'inf': return float('inf')
    if v == '-inf': return float('-inf')
    return float(v)

def outside(v): return not (FLOOR <= v <= CEIL)    # nan も外（違反）に数える

def q(xs, p):
    xs = sorted(x for x in xs if not math.isnan(x))
    if not xs: return float('nan')
    i = min(len(xs) - 1, max(0, int(round(p * (len(xs) - 1)))))
    return xs[i]

def main():
    d = sys.argv[1]; outp = sys.argv[2] if len(sys.argv) > 2 else None
    rows = load(d)
    out = []
    def P(s=''): out.append(s); print(s)
    seeds = sorted(set(r['_seed'] for r in rows))
    warm = [r for r in rows if r['frozen'] == '1']; main_ = [r for r in rows if r['frozen'] == '0']
    P(f'読んだ行 {len(rows)}（種 {len(seeds)} 本: {",".join(seeds)}）・助走 {len(warm)}・本走 {len(main_)}')
    # 新しい列が無い生では読めない
    need = ['relMinRefIndex', 'relMinValIndex', 'minRefOneSidedShare', 'enterShareMinRef', 'minRefMaxShare']
    if not rows or any(k not in rows[0] for k in need):
        P('判定: 未了（生に新しい列が無い）'); sys.exit(2)
    forms = {  # 形: (rel の列, 片側の列, 入った州の列, 1 州の最大の列)
        'A′(minRef)': ('relMinRefIndex', 'minRefOneSidedShare', 'enterShareMinRef', 'minRefMaxShare'),
        'A(minVal)': ('relMinValIndex', 'minValOneSidedShare', 'enterShareMinVal', 'minValMaxShare'),
        '対照 等重み': ('relEqualIndex', 'equalOneSidedShare', 'enterShareEqual', None),
        '対照 w1': ('relW1Index', 'w1OneSidedShare', 'enterShareW1', 'w1MaxShare'),
        '対照 二乗': ('relSqIndex', None, None, 'sqMaxShare'),
        '対照 産出': ('relProdIndex', None, None, 'prodMaxShare'),
        '対照 max': ('relMaxRefIndex', 'maxRefOneSidedShare', 'enterShareMaxRef', 'maxRefMaxShare'),
    }
    res = {}
    P(); P('形 | P1 本走の挟み外(nan含む)/本走行 | P2 助走の片側の票の割合の最大(nan数) | P3 本走 seq>1 の入った州の票の割合の最大(nan数) | P4 1州>0.5 の注入の割合(全行)')
    for name, (relc, onec, entc, maxc) in forms.items():
        p1 = sum(1 for r in main_ if outside(num(r, relc)))
        p2v = [num(r, onec) for r in warm] if onec else None
        p2 = max((x for x in p2v if not math.isnan(x)), default=float('nan')) if p2v else None
        p2n = sum(1 for x in p2v if math.isnan(x)) if p2v else None
        p3v = [num(r, entc) for r in main_ if int(r['seq']) > 1] if entc else None
        p3 = max((x for x in p3v if not math.isnan(x)), default=float('nan')) if p3v else None
        p3n = sum(1 for x in p3v if math.isnan(x)) if p3v else None
        if maxc:
            mv = [num(r, maxc) for r in rows]
            p4 = sum(1 for x in mv if x > P4_SHARE) / len(mv)
        else: p4 = None
        res[name] = (p1, p2, p2n, p3, p3n, p4)
        f = lambda v, fmt: ('—' if v is None else fmt.format(v))
        P(f'{name} | {p1}/{len(main_)} | {f(p2,"{:.4f}")}({f(p2n,"{}")}) | {f(p3,"{:.4f}")}({f(p3n,"{}")}) | {f(p4,"{:.4f}")}')
    P()
    def passes(name):
        p1, p2, p2n, p3, p3n, p4 = res[name]
        g1 = p1 == 0
        g2 = p2 is not None and p2n == 0 and p2 <= P2_MAX
        g3 = p3 is not None and p3n == 0 and p3 <= P3_MAX
        g4 = p4 is not None and p4 <= P4_FRAC
        return g1, g2, g3, g4
    for name in ('A′(minRef)', 'A(minVal)'):
        g = passes(name)
        P(f'{name}: P1={"通る" if g[0] else "落ちる"} P2={"通る" if g[1] else "落ちる"} P3={"通る" if g[2] else "落ちる"} P4={"通る" if g[3] else "落ちる"} → {"全て通る" if all(g) else "落ちる"}')
    # 対照が落ちること（門が何もしない実装でも通る形でない確かめ）
    ctrl = []
    c2 = res['対照 等重み'][1]; ctrl.append(('P2', '等重み', c2 is not None and c2 > P2_MAX, c2))
    c3 = res['対照 w1'][3]; ctrl.append(('P3', 'w1', c3 is not None and c3 > P3_MAX, c3))
    c4 = res['対照 二乗'][5]; ctrl.append(('P4', '二乗', c4 is not None and c4 > P4_FRAC, c4))
    c1 = res['対照 w1'][0]; ctrl.append(('P1', 'w1', c1 > 0, c1))
    P()
    for g, nm, fell, v in ctrl:
        P(f'対照 {g} {nm}: {"落ちる(門は働く)" if fell else "落ちない(この門は何でも通る＝採否に使わず記録へ)"} 値={v}')
    gA1, gA = passes('A′(minRef)'), passes('A(minVal)')
    P()
    if all(gA1): sel = 'A′'
    elif all(gA): sel = 'A'
    else: sel = '採らない'
    P(f'選び方の結論: {sel}')
    # 記録
    P(); P('記録（門にしない）')
    for name, (relc, onec, entc, maxc) in forms.items():
        v = [num(r, relc) for r in main_]
        P(f'  本走の rel {name}: p5={q(v,.05):.3f} p50={q(v,.5):.3f} p95={q(v,.95):.3f} 最小={min((x for x in v if not math.isnan(x)), default=float("nan")):.3f} 最大={max((x for x in v if not math.isnan(x)), default=float("nan")):.3f}')
    for name, (relc, onec, entc, maxc) in forms.items():
        if not maxc: continue
        v = [num(r, maxc) for r in rows]
        P(f'  1 州の最大の票の割合 {name}: p50={q(v,.5):.3f} p90={q(v,.9):.3f} p99={q(v,.99):.3f} 最大={max((x for x in v if not math.isnan(x)), default=float("nan")):.3f}')
    for k in ('enterCount', 'leaveCount', 'minRefZeroCount', 'minValZeroCount'):
        v = [num(r, k) for r in rows]
        P(f'  {k}: 平均={st.mean(v):.2f} p90={q(v,.9):.0f} 最大={max(v):.0f} 0 より大きい行={sum(1 for x in v if x>0)}')
    rr = [num(r, 'relMinRefIndex') / num(r, 'relW1Index') for r in main_ if num(r, 'relW1Index') > 0]
    ra = [num(r, 'relMinValIndex') / num(r, 'relW1Index') for r in main_ if num(r, 'relW1Index') > 0]
    P(f'  本走の A′÷w1 の rel の比: p5={q(rr,.05):.3f} p50={q(rr,.5):.3f} p95={q(rr,.95):.3f}・A÷w1: p5={q(ra,.05):.3f} p50={q(ra,.5):.3f} p95={q(ra,.95):.3f}')
    bad = [r for r in rows if outside(num(r, 'relMinRefIndex')) or outside(num(r, 'relMinValIndex')) or outside(num(r, 'relW1Index'))]
    P(f'  rel が挟みの外の注入（A′・A・w1 のどれか・助走も含む）: {len(bad)}')
    for r in bad[:40]:
        P(f'    種{r["_seed"]} seq{r["seq"]} 年{r["year"]} frozen{r["frozen"]} A′={num(r,"relMinRefIndex"):.3f} A={num(r,"relMinValIndex"):.3f} w1={num(r,"relW1Index"):.3f} 等重み={num(r,"relEqualIndex"):.3f}')
    # 読みの自己確認: 自走査とエンジンの渡した値の差
    for k in ('stateCountDiff', 'w1SumDiff', 'relW1ReadDiff'):
        v = [num(r, k) for r in rows]
        P(f'  読みの差 {k}: 最大={max(v)} 0 でない行={sum(1 for x in v if x != 0)}')
    P(f'判定: {"完了" if rows else "未了"}')
    if outp:
        with open(outp, 'w', encoding='utf-8', newline='\n') as fh: fh.write('\n'.join(out) + '\n')

main()
