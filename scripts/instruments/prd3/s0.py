# -*- coding: utf-8 -*-
"""(715) prd3 段0：影の N（旗 0 の世界の上の数）の基線・ヌル腕・種 11〜20 の統計と、事前登録の門の値の導出（読むだけ）。
使い方：python s0.py   （入力は common.ARMS の keep_711）
出力：①§3-2 の規則（読み方 1）②種ごとの N≧1 の割合 ③政体×組合法の表 ④今の主軸（nBodyComReal）がヌル腕で 0 であること ⑤蜂起の目標の内訳と政体 6 の行 ⑥門 G1a・G1c の値の導出
"""
import collections
from common import *

res = {}
for arm, (root, s0) in ARMS.items():
    per = []
    cell = collections.defaultdict(lambda: [0, 0])
    uk_n1 = tot_n1 = 0
    uk_seed_half = 0
    real_n1 = rows_com = 0
    for s in seeds(s0):
        ix, rows = read(path(root, 'prds', s))
        n = k = 0
        uk_s = tot_s = 0
        for x in rows:
            if float(x[ix['eraComm']]) != 1.0:
                continue
            n += 1
            rows_com += 1
            sh = int(x[ix['nBodyComShadow']]) >= 1
            k += sh
            real_n1 += int(x[ix['nBodyComReal']]) >= 1
            c = cell[(int(x[ix['polityCode']]), int(x[ix['unionLaw']]))]
            c[0] += 1
            c[1] += sh
            if sh:
                tot_s += 1
                uk_s += x[ix['nation']] == UK
        per.append((s, n, k))
        uk_n1 += uk_s
        tot_n1 += tot_s
        if tot_s and 2 * uk_s >= tot_s:
            uk_seed_half += 1
    res[arm] = dict(per=per, cell=cell, uk=uk_n1, tot=tot_n1, ukhalf=uk_seed_half, real=real_n1, rows=rows_com)

print('# (715) prd3 段0 の数（読み手 scripts/instruments/prd3/s0.py・入力 D:/PoB_work/keep_711・影は旗 0 の世界の上の数）')
print()
print('## ① 設計書 §3-2 の規則（読み方 1＝N≧1 の行のうち英の割合。★(711) 作者に聞く③の推奨）')
for arm in ARMS:
    r = res[arm]
    print(f'  {arm}: N≧1 の行 {r["tot"]}・うち英 {r["uk"]}（{100.0 * r["uk"] / r["tot"]:.1f}%）・英が半分以上の種 {r["ukhalf"]}/10')
print('  判定：英が半分以上の種は 4 本とも 0/10、割合は 8.3〜12.9% ＝〈英が半分以上〉を満たさない ＝ §3-2 の規則で腕 B を回さない理由は無い（腕 B へ進む）。')
print()
print('## ② 種ごとの N≧1 の割合 %（共産主義を見つけた国×四半期のうち nBodyComShadow≧1。左から種の順）')
for arm in ARMS:
    r = res[arm]
    fr = [100.0 * k / n for (_, n, k) in r['per']]
    pooled = 100.0 * sum(k for _, _, k in r['per']) / sum(n for _, n, _ in r['per'])
    print(f'  {arm}: ' + ' '.join(f'{v:.1f}' for v in fr) + f' | 全体 {pooled:.2f}% 種の最小 {min(fr):.1f}% 最大 {max(fr):.1f}%')
print()
print('## ③ 政体×組合法（共産主義を見つけた行の数 / 影の N≧1 の割合 %）  base | n1 | n2 | a11')
keys = sorted(set(k for a in res for k in res[a]['cell']))
for k in keys:
    cs = []
    for a in ('base', 'n1', 'n2', 'a11'):
        n, kk = res[a]['cell'].get(k, (0, 0))
        cs.append(f'{n:6d} {100.0 * kk / n if n else float("nan"):5.1f}')
    print(f'  {POL.get(k[0], k[0])}・{LAW.get(k[1], k[1])} : ' + ' | '.join(cs))
print()
print('## ④ 今の主軸（nBodyComReal≧1）の行数：base / n1 / n2 / a11 ＝ ' + ' / '.join(f'{res[a]["real"]}（共産主義を見つけた行 {res[a]["rows"]}）' for a in ARMS))
print()
print('## ⑤ 蜂起の目標（prdf_s の revoltN の和を revoltTarget 別）と政体 6 の行（国×四半期）・政体 6 に届いた（種×国）')
for arm, (root, s0) in ARMS.items():
    tgt = collections.Counter()
    seeds_pd = 0
    pol6 = 0
    pairs = set()
    for s in seeds(s0):
        ix, rows = read(path(root, 'prdf', s))
        seed_has = False
        for x in rows:
            if int(x[ix['polityCode']]) == PD:
                pol6 += 1
                pairs.add((s, x[ix['nation']]))
            n = int(x[ix['revoltN']])
            if n > 0:
                tgt[int(x[ix['revoltTarget']])] += n
                if int(x[ix['revoltTarget']]) == PD:
                    seed_has = True
        seeds_pd += seed_has
    print(f'  {arm}: 蜂起の目標別 {dict(sorted(tgt.items()))}・目標が政体 6 の蜂起のある種 {seeds_pd}/10・政体 6 の行 {pol6}・政体 6 に届いた（種×国）{len(pairs)}')
print()
print('## ⑥ 門の値の導出（機械的に・手で触らない）')
nullpool = [100.0 * sum(k for _, _, k in res[a]['per']) / sum(n for _, n, _ in res[a]['per']) for a in ('base', 'n1', 'n2')]
nullseedmin = min(100.0 * k / n for a in ('base', 'n1', 'n2') for (_, n, k) in res[a]['per'])
a11pool = 100.0 * sum(k for _, _, k in res['a11']['per']) / sum(n for _, n, _ in res['a11']['per'])
a11seedmin = min(100.0 * k / n for (_, n, k) in res['a11']['per'])
print(f'  影の N≧1 の割合（全体）：ヌル腕 3 本 {nullpool[0]:.2f} / {nullpool[1]:.2f} / {nullpool[2]:.2f}（幅 {max(nullpool) - min(nullpool):.2f}pp）・種 11〜20 {a11pool:.2f}')
print(f'  G1a（種 1〜10）：腕 B の nBodyComReal≧1 の割合 ≧ 影の最小の半分 ＝ 0.5 × {min(nullpool):.2f} ＝ {0.5 * min(nullpool):.2f}%')
print(f'  G1c（種 1〜10）：種ごとの割合 ≧ 影の種ごとの最小（ヌル腕 3 本×10 種 30 軌道）の半分 ＝ 0.5 × {nullseedmin:.1f} ＝ {0.5 * nullseedmin:.1f}% の種が 8/10 以上')
print(f'  G1a（種 11〜20）：≧ 0.5 × {a11pool:.2f} ＝ {0.5 * a11pool:.2f}%／G1c（種 11〜20）：種ごと ≧ 0.5 × {a11seedmin:.1f} ＝ {0.5 * a11seedmin:.1f}% の種が 8/10 以上')
