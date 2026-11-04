#!/usr/bin/env python
# (827) c66sv 銀の注入の重み A′（旗 SilverParityDemandWeighted の値 2）の門の読み手（読むだけ）。事前登録 measurements/m827_20261104_prereg_sv.txt。
#   使い方: python -I scripts/instruments/c66sv/paper827.py <計器の出力 ins（D:/… の形）> [--mode paper|arm] [--out 出力]
#   paper＝旗 0 の盤面の反実仮想の列（計器の自走査）で形ごとに門を当てる（腕の前の予言・旗 0 で門が落ちることの確かめ）。
#   arm  ＝腕 B（旗 2）の生でエンジンが実際に使った票の列（used*）で門を当て、S1（計器の自走査 A′ とエンジンのビット一致）を読む。
#   門（腕の前に固定・数は事前登録 [3]）：
#     W1  助走（frozen=1）の全ての注入で、片側の州（金鉱の需要＜産出の 1%）の票の割合の最大 ≦0.05
#     W1m 助走の全ての注入で、銀の側の片側（銀の需要＜銀の産出の 1%）の票の割合の最大 ≦0.05（★(773) の鏡・列は (827) で足した）
#     W2b 本走（frozen=0）の全ての注入で、rel が挟み [0.3, 3.0] の外（nan を含む）の件数 0
#     E1  本走の seq＞1 の全ての注入で、入った州の票の割合の最大 ≦0.05
#     U1  全ての注入のうち、1 州の票の割合が 0.5 を超える注入の割合 ≦0.01
#     S1  （arm だけ）ratioOkFlag=1 の全ての注入で usedRelIndex・usedMaxShare・usedOneSidedShare が計器の自走査の A′（relMinRefIndex・minRefMaxShare・minRefOneSidedShare）と文字列で一致（=ビット一致）
#     狙い T1  1786〜1790 年の英の州の w1 の票の割合の最大が 0.1 超（paper の前提）で、A′ の票の割合の最大が ≦0.05
#     狙い T2  （記録）本走の rel の新÷旧（A′÷等重み）の分位
import sys, os, glob, math, argparse
try: sys.stdout.reconfigure(encoding='utf-8')
except Exception: pass
FLOOR, CEIL = 0.3, 3.0
W1_MAX, W1M_MAX, E1_MAX, U1_FRAC, U1_SHARE = 0.05, 0.05, 0.05, 0.01, 0.5
UK = 'イギリス'

ap = argparse.ArgumentParser()
ap.add_argument('ins'); ap.add_argument('--mode', default='paper'); ap.add_argument('--out', default=None)
ap.add_argument('--seeds', default='1-10')
a = ap.parse_args()
lo, hi = (int(x) for x in a.seeds.split('-'))
seeds = list(range(lo, hi + 1))

def load(prefix, s):
    p = os.path.join(a.ins, f'{prefix}{s}.tsv')
    if not os.path.exists(p): return None
    out = []
    with open(p, encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t')
        for line in fh:
            q = line.rstrip('\n').split('\t')
            if len(q) == len(h): out.append(dict(zip(h, q)))
    return out

def num(r, k):
    v = r.get(k)
    if v is None or v == 'nan': return float('nan')
    if v == 'inf': return float('inf')
    if v == '-inf': return float('-inf')
    return float(v)

def outside(v): return not (FLOOR <= v <= CEIL)
def q(xs, p):
    xs = sorted(x for x in xs if not math.isnan(x))
    if not xs: return float('nan')
    return xs[min(len(xs) - 1, max(0, int(round(p * (len(xs) - 1)))))]
def mx(xs):
    v = [x for x in xs if not math.isnan(x)]
    return (max(v) if v else float('nan')), sum(1 for x in xs if math.isnan(x))

out = []
def P(s=''): out.append(s); print(s)

rows, st, missing = [], [], []
for s in seeds:
    r = load('silverwt_s', s)
    if r is None: missing.append(s); continue
    for x in r: x['_seed'] = s
    rows += r
    t = load('silverst_s', s) or []
    for x in t: x['_seed'] = s
    st += t
warm = [r for r in rows if r['frozen'] == '1']; main_ = [r for r in rows if r['frozen'] == '0']
P(f'# paper827 mode={a.mode} ins={a.ins} 種={seeds[0]}-{seeds[-1]} 欠けた種={missing}')
P(f'読んだ行 silverwt_s {len(rows)}（助走 {len(warm)}・本走 {len(main_)}）・silverst_s {len(st)}')
has_new = bool(rows) and 'usedAgOneSidedShare' in rows[0]
P(f'(827) の列（usedAgOneSidedShare ほか）: {"あり" if has_new else "なし（W1m・エンジン側の E1 は読めない）"}')

def gates(name, relc, onec, onemc, entc, maxc):
    w1, w1n = mx([num(r, onec) for r in warm]) if onec else (None, None)
    w1m, w1mn = mx([num(r, onemc) for r in warm]) if (onemc and has_new) else (None, None)
    w2 = sum(1 for r in main_ if outside(num(r, relc))) if relc else None
    e1, e1n = mx([num(r, entc) for r in main_ if int(r['seq']) > 1]) if (entc and (has_new or not entc.startswith('used'))) else (None, None)
    u1 = (sum(1 for r in rows if num(r, maxc) > U1_SHARE) / len(rows)) if maxc else None
    return dict(W1=(w1, w1n), W1m=(w1m, w1mn), W2b=w2, E1=(e1, e1n), U1=u1)

def judge(g):
    j = {}
    j['W1'] = None if g['W1'][0] is None else (g['W1'][1] == 0 and g['W1'][0] <= W1_MAX)
    j['W1m'] = None if g['W1m'][0] is None else (g['W1m'][1] == 0 and g['W1m'][0] <= W1M_MAX)
    j['W2b'] = None if g['W2b'] is None else g['W2b'] == 0
    j['E1'] = None if g['E1'][0] is None else (g['E1'][1] == 0 and g['E1'][0] <= E1_MAX)
    j['U1'] = None if g['U1'] is None else g['U1'] <= U1_FRAC
    return j

def fmt(g, j):
    def f(v, k, spec):
        if v is None: return '—'
        return f'{spec.format(v)}{"" if j[k] is None else ("（通る）" if j[k] else "（落ちる）")}'
    return (f'W1 {f(g["W1"][0], "W1", "{:.4f}")}[nan {g["W1"][1]}] | W1m {f(g["W1m"][0], "W1m", "{:.4f}")}[nan {g["W1m"][1]}] | '
            f'W2b {f(g["W2b"], "W2b", "{}")}/{len(main_)} | E1 {f(g["E1"][0], "E1", "{:.4f}")}[nan {g["E1"][1]}] | U1 {f(g["U1"], "U1", "{:.4f}")}')

if a.mode == 'paper':
    forms = {  # 形: rel・片側(金)・片側(銀)・入った州・1 州の最大
        'A′（旗 2 の形・min 参照価格建て）': ('relMinRefIndex', 'minRefOneSidedShare', 'minRefAgOneSidedShare', 'enterShareMinRef', 'minRefMaxShare'),
        '対照 等重み（旗 0）': ('relEqualIndex', 'equalOneSidedShare', 'equalAgOneSidedShare', 'enterShareEqual', None),
        '対照 w1（旗 1＝値 2→1 の突然変異・dAg を無視の突然変異）': ('relW1Index', 'w1OneSidedShare', 'w1AgOneSidedShare', 'enterShareW1', 'w1MaxShare'),
        '対照 max（min→max の突然変異）': ('relMaxRefIndex', 'maxRefOneSidedShare', 'maxRefAgOneSidedShare', 'enterShareMaxRef', 'maxRefMaxShare'),
        '記録 A（min 局所の値段建て）': ('relMinValIndex', 'minValOneSidedShare', None, 'enterShareMinVal', 'minValMaxShare'),
        '対照 二乗': ('relSqIndex', None, None, None, 'sqMaxShare'),
        '対照 産出': ('relProdIndex', None, None, None, 'prodMaxShare'),
    }
    P(); P('形 | 門（旗 0 の盤面の反実仮想・計器の自走査）')
    res = {}
    for name, cols in forms.items():
        g = gates(name, *cols); j = judge(g); res[name] = (g, j)
        P(f'{name} | {fmt(g, j)}')
    P()
    gA, jA = res['A′（旗 2 の形・min 参照価格建て）']
    P(f'A′ の門: ' + ' '.join(f'{k}={"—" if v is None else ("通る" if v else "落ちる")}' for k, v in jA.items()))
    ge, je = res['対照 等重み（旗 0）']; gw, jw = res['対照 w1（旗 1＝値 2→1 の突然変異・dAg を無視の突然変異）']
    P(f'旗 0 で W1 が落ちる: {"はい" if je["W1"] is False else "いいえ"}（{ge["W1"][0]}）')
    P(f'旗 1 で E1 が落ちる: {"はい" if jw["E1"] is False else "いいえ"}（{gw["E1"][0]}）')
    if has_new: P(f'w1 で W1m が落ちる: {"はい" if jw["W1m"] is False else "いいえ"}（{gw["W1m"][0]}）')
    v = [num(r, 'relMinRefIndex') / num(r, 'relEqualIndex') for r in main_ if num(r, 'relEqualIndex') > 0]
    P(f'狙い T2（記録）本走の rel の A′÷等重み: p1={q(v,.01):.4f} p5={q(v,.05):.4f} p50={q(v,.5):.4f} p95={q(v,.95):.4f} p99={q(v,.99):.4f} 最小={min(v):.4f} 最大={max(v):.4f}')
    for name in ('A′（旗 2 の形・min 参照価格建て）', '対照 等重み（旗 0）', '対照 w1（旗 1＝値 2→1 の突然変異・dAg を無視の突然変異）'):
        relc = forms[name][0]; w = [num(r, relc) for r in main_]
        P(f'  本走の rel {name}: p5={q(w,.05):.3f} p50={q(w,.5):.3f} p95={q(w,.95):.3f} 最小={min(x for x in w if not math.isnan(x)):.3f} 最大={max(x for x in w if not math.isnan(x)):.3f}')
    relcol = 'relMinRefIndex'
else:
    g = gates('エンジン', 'usedRelIndex', 'usedOneSidedShare', 'usedAgOneSidedShare', 'usedEnterShare', 'usedMaxShare'); j = judge(g)
    P(); P(f'エンジン（旗 2・used*） | {fmt(g, j)}')
    ok = [r for r in rows if r['ratioOkFlag'] == '1']
    s1 = {k: sum(1 for r in ok if r[u] != r[c]) for k, (u, c) in
          {'rel': ('usedRelIndex', 'relMinRefIndex'), 'maxShare': ('usedMaxShare', 'minRefMaxShare'), 'oneSided': ('usedOneSidedShare', 'minRefOneSidedShare'),
           'agOneSided': ('usedAgOneSidedShare', 'minRefAgOneSidedShare'), 'enter': ('usedEnterShare', 'enterShareMinRef')}.items()} if has_new else None
    if s1 is None: P('S1: 読めない（列なし）'); s1ok = None
    else:
        s1ok = all(v == 0 for v in s1.values())
        P(f'S1 ratioOk の注入 {len(ok)}・食い違い ' + '・'.join(f'{k} {v}' for k, v in s1.items()) + f' → {"通る" if s1ok else "落ちる"}')
    nok = len(rows) - len(ok)
    P(f'  ratioOkFlag=0 の注入 {nok}（記録）・clampHitCount の本走の和 {sum(int(r["clampHitCount"]) for r in main_)}（記録）')
    P('門: ' + ' '.join(f'{k}={"—" if v is None else ("通る" if v else "落ちる")}' for k, v in j.items()) + f' S1={"—" if s1ok is None else ("通る" if s1ok else "落ちる")}')
    v = [num(r, 'usedRelIndex') / num(r, 'relEqualIndex') for r in main_ if num(r, 'relEqualIndex') > 0]
    P(f'狙い T2（記録）同じ盤面の本走の rel の新÷旧（used÷等重み）: p1={q(v,.01):.4f} p5={q(v,.05):.4f} p50={q(v,.5):.4f} p95={q(v,.95):.4f} p99={q(v,.99):.4f} 最小={min(v):.4f} 最大={max(v):.4f}')
    w = [num(r, 'usedRelIndex') for r in main_]
    P(f'  本走の used rel: p5={q(w,.05):.3f} p50={q(w,.5):.3f} p95={q(w,.95):.3f} 最小={min(w):.3f} 最大={max(w):.3f}')
    relcol = 'usedRelIndex'

# 狙い T1：1786〜1790 年の英の州の票の割合（silverst_s の行の w1Share・minRefShare）
uk = [r for r in st if r['nation'] == UK and 1786 <= int(r['year']) <= 1790]
w1s = [num(r, 'w1Share') for r in uk]; rfs = [num(r, 'minRefShare') for r in uk]
w1max, _ = mx(w1s); rfmax, _ = mx(rfs)
per = {}
for r in uk:
    k = r['_seed']; a1, b1 = per.get(k, (0.0, 0.0))
    x, y = num(r, 'w1Share'), num(r, 'minRefShare')
    per[k] = (max(a1, x) if not math.isnan(x) else a1, max(b1, y) if not math.isnan(y) else b1)
P(); P(f'狙い T1 1786〜1790 の英の州の行 {len(uk)}・w1 の票の割合の最大 {w1max:.4f}（前提 ＞0.1: {"はい" if w1max > 0.1 else "いいえ"}）・A′ の票の割合の最大 {rfmax:.4f}（≦0.05: {"通る" if rfmax <= 0.05 else "落ちる"}）')
P('  種ごと (w1 の最大, A′ の最大): ' + ' '.join(f's{k}=({v[0]:.3f},{v[1]:.4f})' for k, v in sorted(per.items())))
P('  ⚠限界：silverst_s は上位 5（T1・TR・TV）と入った・出た州の行だけ＝英の州が どの上位 5 にも入らない注入では割合が行に無い（その州の割合は上位 5 の最小より小さい）')
bad = [r for r in main_ if outside(num(r, relcol))]
P(f'挟みの外の本走の注入（{relcol}）: {len(bad)}')
for r in bad[:20]:
    P(f'  種{r["_seed"]} seq{r["seq"]} 年{r["year"]} {relcol}={r[relcol]} 等重み={r["relEqualIndex"]} w1={r["relW1Index"]}')
P('判定: 完了' if rows and not missing else '判定: 未了（種が欠けた）')
if a.out:
    with open(a.out, 'w', encoding='utf-8', newline='\n') as fh: fh.write('\n'.join(out) + '\n')
