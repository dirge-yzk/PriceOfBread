# (893) c76lg 読むだけ：議会の侵食・一党支配・政治予算・敗戦ショック・戦時の結束の門の開き方を数える（sf_s・scalars_s・nation_s）。
# 使い方: python -X utf8 s15_extra.py <生のフォルダ>
import sys, os, collections
d = sys.argv[1]
def rows(t, s):
    p = os.path.join(d, f'{t}_s{s}.tsv')
    if not os.path.exists(p): return []
    L = open(p, 'rb').read().decode('utf-8').split('\n'); h = L[0].rstrip('\r').split('\t')
    return [dict(zip(h, l.rstrip('\r').split('\t'))) for l in L[1:] if l.strip()]
def f(x):
    try: return float(x)
    except: return float('nan')
cm = cm_anti = 0; anti_sum = 0.0; dom_rows = 0; parl_rows = 0; ds_jump = 0; ds_half = 0; dmwar_nz = 0; rally = 0; atwar = 0; drain_neg = 0
nf = 0; pb_lt1 = 0; fronts = collections.Counter(); pbmin = 1.0
for s in range(1, 11):
    sc = {(r['nation'], r['quarter']): r for r in rows('scalars', s)}
    prev = {}
    for k, r in sorted(sc.items(), key=lambda kv: (kv[0][0], int(kv[0][1]))):
        D = f(r['defeatShock'])
        if D >= 0.5: ds_half += 1
        if k[0] in prev and D > prev[k[0]] + 1e-9: ds_jump += 1
        prev[k[0]] = D
        if f(r['dmWar']) != 0: dmwar_nz += 1
        if r['atWar'] == '1':
            atwar += 1; we = f(r['warExhaustion'])
            if we < 0.4: rally += 1
            if 1 + 0.3 * max(0, 1 - we / 0.4) - 0.15 * we < 1: drain_neg += 1
    for r in rows('sf', s):
        x = sc.get((r['nation'], r['quarter']))
        if x is None: continue
        pc = x['polityCode']; c_, l_, so = f(r['seatCons']), f(r['seatLib']), f(r['seatSoc'])
        if pc == '3':
            cm += 1
            if l_ + so > 0: cm_anti += 1; anti_sum += l_ + so
        if pc in ('3', '4'):
            parl_rows += 1
            if max(c_, l_, so) > 0.6: dom_rows += 1
    for r in rows('nation', s):
        nf += 1; pb = f(r['pushBudgetFactor']); fronts[r['openFrontsCount']] += 1
        if pb < 1 - 1e-12: pb_lt1 += 1
        if pb < pbmin: pbmin = pb
print(f'議会の侵食：立憲の行 {cm}・反王党派（自由＋社会）の議席＞0 の行 {cm_anti}・その平均 {anti_sum / max(1, cm_anti):.3f}（侵食＝0.02×これ×天命の乗数）')
print(f'一党支配：立憲・共和の行 {parl_rows}・最大会派＞0.6 の行 {dom_rows}（もう 1 つの条件＝大衆の平均満足＜0.43 は列が無い）')
print(f'敗戦ショック：≥0.5 の行 {dom_rows*0 + ds_half}・前の行より増えた行（ショックの点火）{ds_jump}')
print(f'天命の戦争の項 dmWar≠0 の行 {dmwar_nz}')
print(f'戦時の結束：交戦の行 {atwar}・厭戦＜0.4（結束の項＞0）{rally}・乗数＜1（ドレインが上回る）{drain_neg}')
print(f'政治予算：nation の行 {nf}・押す力の係数＜1 の行 {pb_lt1}・最小 {pbmin:.4f}・開いた戦線の数の分布', dict(sorted(fronts.items(), key=lambda kv: int(kv[0]) if kv[0].lstrip('-').isdigit() else 99)[:12]))
