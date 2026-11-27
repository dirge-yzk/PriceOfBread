# (1409) 問い 1：侵略の偏り。旗 1 の生（warentry_s・invade_s・nation_s）と旗 0 の生（keep_1390_rej の wars_s・nation_s）を国ごとに数える。
# selfThreat の下限 LB＝0.45×実質GDP÷全国の平均−1（Diplomacy.cs:1019 の GDP の項だけ・植民地と軍の項は ≥0＝本当の selfThreat ≧ LB）。
# 旗 0 の試行の率の上限 p0max＝0.12×max(0.25, 1−LB)（LB≤0 なら 0.12）＝本当の率はこれ以下。
# 使い方: python q1_invade.py <生 D:/…> <種の起点> <種の数> <旗 1 か 0>
import csv, os, sys, statistics as st
D, SB, N, FLAG = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), int(sys.argv[4])
NAT = '清,アメリカ,イギリス,フランス,日本,ロシア,スペイン,オスマン,プロイセン'.split(',')
def rows(t, s):
    p = os.path.join(D, f'{t}_s{s}.tsv')
    if not os.path.exists(p): return None
    with open(p, encoding='utf-8') as f: return list(csv.DictReader(f, delimiter='\t'))
def med(v): return st.median(v) if v else float('nan')
ERAS = [(1783, 1830), (1830, 1870), (1870, 1914), (1914, 1951)]
agg = {n: {k: [] for k in ('calls', 'okQ', 'inv', 'sub', 'ref', 'subjWar', 'floorQ', 'p0sum', 'q', 'willQ')} for n in NAT}
era_inv = {n: [[0] * len(ERAS) for _ in range(N)] for n in NAT}
lbq = {n: [[] for _ in ERAS] for n in NAT}
for i, s in enumerate(range(SB, SB + N)):
    na = rows('nation', s) or []
    byq = {}
    for r in na:
        try: g = float(r['realGdpPerYear'])
        except: continue
        if g == g: byq.setdefault(r['quarter'], []).append((r['nation'], g, int(float(r['year']))))
    lb = {}
    for q, lst in byq.items():
        avg = max(1e-12, sum(g for _, g, _ in lst) / len(lst))
        for n, g, y in lst: lb[(n, q)] = (0.45 * g / avg - 1.0, y)
    cnt = {n: dict(calls=0, okQ=0, willQ=0) for n in NAT}
    for r in rows('warentry', s) or []:
        n = r['nation']
        if n not in cnt: continue
        c = int(float(r['calls']))
        cnt[n]['calls'] += c
        if c and int(float(r['r_ok'])) > 0: cnt[n]['okQ'] += 1
        if c and int(float(r['r_will'])) > 0: cnt[n]['willQ'] += 1
    inv = rows('invade', s) or []
    wars = rows('wars', s) or []
    for n in NAT:
        a = agg[n]
        a['calls'].append(cnt[n]['calls']); a['okQ'].append(cnt[n]['okQ']); a['willQ'].append(cnt[n]['willQ'])
        mine = [r for r in inv if r['invader'] == n]
        a['inv'].append(len(mine)); a['sub'].append(sum(1 for r in mine if r['submitted'] == '1')); a['ref'].append(sum(1 for r in mine if r['submitted'] == '0'))
        a['subjWar'].append(sum(1 for r in wars if r['entryCode'] == 'subjugate' and r['sideA'] == n))
        fq = 0; p0 = 0.0; qq = 0
        for (nn, q), (v, y) in lb.items():
            if nn != n: continue
            qq += 1
            if v >= 0.75: fq += 1
            p0 += 0.12 * (max(0.25, 1 - v) if v > 0 else 1.0)
            for e, (y0, y1) in enumerate(ERAS):
                if y0 <= y < y1: lbq[n][e].append(v)
        a['floorQ'].append(fq); a['p0sum'].append(p0); a['q'].append(qq)
        for r in mine:
            y = int(float(r['year']))
            for e, (y0, y1) in enumerate(ERAS):
                if y0 <= y < y1: era_inv[n][i][e] += 1
        if not inv:
            for r in wars:
                if r['entryCode'] == 'subjugate' and r['sideA'] == n:
                    y = int(float(r['startYear']))
                    for e, (y0, y1) in enumerate(ERAS):
                        if y0 <= y < y1: era_inv[n][i][e] += 1
print(f'# 生 {D}・種 {SB}〜{SB + N - 1}・旗 {FLAG}・値は種の中央（invade の表が無い生は wars の subjugate〔拒否された侵略＝戦争〕だけ）')
print('nation\tcalls\tokQ\twillQ\tinvade\tsubmitted\trefused\tsubjWar\tquarters\tfloorQ_LB>=0.75\tp0max_sum')
for n in NAT:
    a = agg[n]
    print(n + '\t' + '\t'.join(f'{med(a[k]):g}' for k in ('calls', 'okQ', 'willQ', 'inv', 'sub', 'ref', 'subjWar', 'q', 'floorQ')) + f"\t{med(a['p0sum']):.1f}")
print('# 侵略（invade の行・無ければ subjugate の戦争）の時代ごと（種の中央）：' + ' '.join(f'{a}-{b - 1}' for a, b in ERAS))
for n in NAT:
    print(n + '\t' + '\t'.join(f'{med([era_inv[n][i][e] for i in range(N)]):g}' for e in range(len(ERAS))))
print('# selfThreat の下限 LB の時代ごとの中央（全ての種の国×四半期）')
for n in NAT:
    print(n + '\t' + '\t'.join(f'{med(lbq[n][e]):.2f}' for e in range(len(ERAS))))
