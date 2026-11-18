#!/usr/bin/env python
# (1199) 注：(1193) で欄（国民意識）が消えた・旧の生でだけ動く（中身は変えていない）
"""(710) rd48 ③の続き：1 国の植民地の州の分離独立の項（secession_s・CheckSecession の内訳）を腕ごとに並べる（読むだけ・既存の列だけ）。
使い方：python -X utf8 -I sec_terms.py <腕名>=<計器の出力 ins フォルダ>[:<種の起点>] ... [--nation イギリス] [--regions 中核ベンガル,内陸ベンガル,マドラス] [--periods 1784-1789,1790-1794]
出力：P 行＝腕×期間×項の種の中央（種ごとに期間の四半期の平均をとってから中央）。項＝州の列（闘争度・大衆の飽和・圧力・届く保持力・共有分母で割った保持力・余り＝保持力−圧力・支配度）は
  挙げた州の平均、国の列（正統性・中央の保持力・共有分母・不穏の件数・国の総人口）はその国の値。
  G 行＝種ごとの挙げた州の goFlag（支配度が 0＝分離独立の戦争へ入る）が最初に立った年。
限界の申告：secession_s は CheckSecession を呼んだ時点の値（InstrumentsSecession.cs の頭の ①〜⑥）。分離独立の戦争の勝ち負けは別（wars_s・colony_events.py）。期間の中に州を失った種は、失うまでの四半期だけで平均する。
"""
import csv, os, statistics, sys
from collections import defaultdict
csv.field_size_limit(10**9)
a = sys.argv[1:]; nat = 'イギリス'; regs = ['中核ベンガル', '内陸ベンガル', 'マドラス']; periods = [(1784, 1789), (1790, 1794)]; arms = []
i = 0
while i < len(a):
    if a[i] == '--nation': nat = a[i + 1]; i += 2; continue
    if a[i] == '--regions': regs = a[i + 1].split(','); i += 2; continue
    if a[i] == '--periods': periods = [tuple(int(y) for y in p.split('-')) for p in a[i + 1].split(',')]; i += 2; continue
    name, rest = a[i].split('=', 1)
    d, s0 = (rest.rsplit(':', 1) if ':' in rest[2:] else (rest, '1'))
    arms.append((name, d, int(s0))); i += 1
RT = ['militancyMeanIndex', 'crowdFactorShare', 'popK', 'pressureIndex', 'effHoldBeforeShareIndex', 'holdShareIndex', 'marginIndex', 'dominanceShare', 'consciousnessMeanIndex', 'cultureDistanceIndex']
NT = ['legitimacyIndex', 'holdBaseIndex', 'sharedDivIndex', 'restiveCount', 'totalPopK', 'coerceIndex']
print('kind\tarm\tperiod\tterm\tmedianOverSeeds\tmin\tmax\tseeds')
for name, d, s0 in arms:
    per = defaultdict(list); gos = []
    for s in range(s0, s0 + 10):
        acc = defaultdict(lambda: [0.0, 0]); go = None
        nq = {}
        with open(os.path.join(d, f'secession_s{s}.tsv'), encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: k for k, c in enumerate(h)}
            for x in r:
                if len(x) != len(h) or x[ix['nation']] != nat: continue
                y = int(x[ix['year']]); q = int(x[ix['quarter']])
                pi = next((k for k, (lo, hi) in enumerate(periods) if lo <= y <= hi), None)
                if x[ix['region']] in regs:
                    if go is None and x[ix['goFlag']] == '1': go = y
                    if pi is None: continue
                    for t in RT:
                        try: v = float(x[ix[t]])
                        except ValueError: continue
                        if v == v: b = acc[(pi, t)]; b[0] += v; b[1] += 1
                    if (pi, q) not in nq:
                        nq[(pi, q)] = 1
                        for t in NT:
                            try: v = float(x[ix[t]])
                            except ValueError: continue
                            if v == v: b = acc[(pi, t)]; b[0] += v; b[1] += 1
        gos.append(go)
        for (pi, t), (sm, n) in acc.items():
            if n: per[(pi, t)].append(sm / n)
    for pi, (lo, hi) in enumerate(periods):
        for t in RT + NT:
            v = per.get((pi, t), [])
            if v: print(f'P\t{name}\t{lo}-{hi}\t{t}\t{statistics.median(v):.4g}\t{min(v):.4g}\t{max(v):.4g}\t{len(v)}')
    print(f'G\t{name}\t' + ' '.join(f's{s0 + k}:{g or "—"}' for k, g in enumerate(gos)) + f'\t立った種 {sum(1 for g in gos if g)}/10・1820 年までに {sum(1 for g in gos if g and g <= 1820)}')
