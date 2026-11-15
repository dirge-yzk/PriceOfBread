# (1152) c113lc 紙の台本：今の盤面（旗 0＝自国の発見の門）の生から、案 C の門の旗 0 の値と働く盤面の大きさを数える。読むだけ・書くのは標準出力だけ。
#   python -X utf8 -I paper_lc.py <ins の生> <種の始め> <種の終わり>
# 読む表：railspend_s（discEnd・months・monthsPre・pre の線路と機関車）・disccontact_s（機関車の行・運び手〈機関車〉）。列は名で引く（★(749)）。
# 〈世界が知る〉は、その四半期の終わりに同じ種で discEnd=1 の国が 1 つでもあること（四半期の束＝月の単位の近似・月の列は恒等の計器で足す）。
import sys, os
from collections import defaultdict

ins, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
CONS_LOCO, CONS_RAIL, MAINT_LOCO = 0.12, 0.25, 0.5   # Coefficients.p04_trade.cs:72〜:73・p08:50（ソースから写した）

def rd(p):
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in f:
            c = ln.rstrip('\n').split('\t')
            if len(c) < len(h): continue
            yield ix, c

T = defaultdict(float)
firsts = []
print(f'== {ins} 種 {s0}〜{s1}')
for s in range(s0, s1 + 1):
    p = os.path.join(ins, f'railspend_s{s}.tsv')
    if not os.path.exists(p): print(f'種 {s}：railspend_s が無い'); continue
    rows = list(rd(p))
    wk = defaultdict(bool)
    for ix, c in rows:
        if c[ix['discEnd']] == '1': wk[int(c[ix['q']])] = True
    qf = min(wk) if wk else None
    firsts.append((s, qf, rows[0][1][rows[0][0]['year']] if rows else ''))
    yfirst = None
    for ix, c in rows:
        q = int(c[ix['q']]); d = c[ix['discEnd']]
        if qf is not None and q == qf and yfirst is None: yfirst = c[ix['year']]
        if d == '1': k = '自国が知る'
        elif wk.get(q - 1, False): k = '自国は知らず世界は知る'   # 前の四半期の終わりに世界が知る＝この四半期の全ての月で世界の和は真
        else: k = '世界の誰も知らない'
        T[k + '：行'] += 1
        T[k + '：月'] += float(c[ix['months']])
        for col in ('preBuildRailQty', 'preMaintRailQty', 'preBuildRailAmtGold', 'preMaintRailAmtGold', 'preBuildLocoAmtGold', 'preMaintLocoAmtGold'):
            T[k + '：' + col] += float(c[ix[col]])
    cc = os.path.join(ins, f'disccontact_s{s}.tsv')
    if os.path.exists(cc):
        for ix, c in rd(cc):
            if c[ix['row']] == '機関車' and c[ix['carrier']] == '機関車': T['G-C1a：知る国から買った機関車の額'] += float(c[ix['valuePerQuarter']])
    T['種'] += 1
print('種ごとの世界で最初に機関車を知る四半期（railspend_s の discEnd・G-C0 の境）')
for s, qf, _ in firsts: print(f'  種 {s}：q{qf}（{1783 + qf // 4 if qf is not None else "-"} 年）')
tot = sum(v for k, v in T.items() if k.endswith('：月'))
for k in ('世界の誰も知らない', '自国は知らず世界は知る', '自国が知る'):
    print(f'{k}：国×四半期 {int(T[k + "：行"])}・国×月 {int(T[k + "：月"])}（{T[k + "：月"] / tot if tot else float("nan"):.1%}）')
    print(f'  pre の線路の量 建設 {T[k + "：preBuildRailQty"]:.4g}・保守 {T[k + "：preMaintRailQty"]:.4g}／額 建設 {T[k + "：preBuildRailAmtGold"]:.4g}・保守 {T[k + "：preMaintRailAmtGold"]:.4g}／pre の機関車の額 建設 {T[k + "：preBuildLocoAmtGold"]:.4g}・保守 {T[k + "：preMaintLocoAmtGold"]:.4g}')
k = '自国は知らず世界は知る'
print(f'予言〈推〉：旗 1 で {k} の機関車の需要の量 ≈ 建設 {CONS_LOCO / CONS_RAIL:.2f}×線路の建設の量 {T[k + "：preBuildRailQty"]:.4g} ＋ 保守 {MAINT_LOCO}×線路の保守の量 {T[k + "：preMaintRailQty"]:.4g} ＝ {CONS_LOCO / CONS_RAIL * T[k + "：preBuildRailQty"] + MAINT_LOCO * T[k + "：preMaintRailQty"]:.4g}（配給の前・予算の単価の上がりで建設と保守の量は下がる側）')
print(f'G-C1a の旗 0 の値（10 種の和）：{T["G-C1a：知る国から買った機関車の額"]:.4g}')
