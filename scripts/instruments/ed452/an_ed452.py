#!/usr/bin/env python
# (452) 事前登録 [8] の狙い a0〜a6 と記録を、計器つき 10 種（B）と基線（A＝m451_ins）の生の表から読む。
# 使い方：PYTHONUTF8=1 python an_ed452.py <B の ins フォルダ> <A の ins フォルダ> [種の数=10]
#   ed_s（B・A の両方に出る＝旗に依らず出す）・reform_s・class_s を読む。国×期の値は、期の中の四半期の中央値→種どうしの中央値。
#   判定：7 種以上＝当たり・4〜6＝部分・3 以下＝外れ（種ごとに条件を満たすかを数える）。
import csv, sys, statistics as st

B, A = sys.argv[1].rstrip('/') + '/', sys.argv[2].rstrip('/') + '/'
NS = int(sys.argv[3]) if len(sys.argv) > 3 else 10
NAT8 = ['清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本']
P = {'P0': (13, 47), 'P1': (0, 67), 'P2': (68, 267), 'P3': (268, 467), 'P4': (468, 667)}   # P0＝開幕期（助走後・1783 年の値段を入れない）


def rows(path):
    try:
        with open(path, encoding='utf-8') as f:
            yield from csv.DictReader(f, delimiter='\t')
    except FileNotFoundError:
        return


def num(x):
    try:
        v = float(x)
        return v
    except Exception:
        return float('nan')


def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float('nan')


def per_of(q):
    out = []
    for k, (a, b) in P.items():
        if a <= q <= b:
            out.append(k)
    return out


def load_series(base, seed, table, nat_col, q_col, cols, nations):
    """(nation, period) -> {col: [値...]}"""
    acc = {}
    for d in rows(f'{base}{table}_s{seed}.tsv'):
        n = d[nat_col]
        if n not in nations:
            continue
        try:
            q = int(d[q_col])
        except Exception:
            continue
        for p in per_of(q):
            slot = acc.setdefault((n, p), {c: [] for c in cols})
            for c in cols:
                slot[c].append(num(d[c]))
    return acc


def verdict(k):
    return '当たり' if k >= 7 else ('部分' if k >= 4 else '外れ')


def class_series(base, seed):
    """class_s：(nation, q) -> {teacherK, allK, teacherEarn, allEarn, engineerK}"""
    out = {}
    for d in rows(f'{base}class_s{seed}.tsv'):
        t = d['time']
        if not t.startswith('q'):
            continue
        q = int(t[1:])
        n = d['nation']
        o = out.setdefault((n, q), {'teacherK': 0.0, 'allK': 0.0, 'teacherEarn': 0.0, 'allEarn': 0.0, 'engK': 0.0})
        sk = num(d['sizeK']); ea = num(d['earnedPerMonth'])
        if sk == sk:
            o['allK'] += sk
        if ea == ea:
            o['allEarn'] += ea
        if d['className'] == '教師':
            if sk == sk: o['teacherK'] += sk
            if ea == ea: o['teacherEarn'] += ea
        if d['className'] == '技師' and sk == sk:
            o['engK'] += sk
    return out


def main():
    res = {}
    # ---------------- ed_s ----------------
    ED_COLS = ['fundingShare', 'stateWantPerMonth', 'stateTakePerMonth', 'stateWantMoneyPerMonth', 'stateTakeMoneyPerMonth', 'shortRegionShare', 'priceOverRefTimesLevelIndex', 'demandSignalPerMonth', 'producedPerMonth', 'soldPerMonth']
    edB = {s: load_series(B, s, 'ed', 'nation', 'quarter', ED_COLS, NAT8) for s in range(1, NS + 1)}
    edA = {s: load_series(A, s, 'ed', 'nation', 'quarter', ED_COLS, NAT8) for s in range(1, NS + 1)}
    haveA = any(edA[s] for s in edA)
    # a0 場面：fundingShare>0 の国×四半期のうち shortRegionShare>0 の割合（B の P2〜P4・8 国合計）
    k0 = 0; r0 = []
    for s in range(1, NS + 1):
        tot = hit = 0
        for (n, p), v in edB[s].items():
            if p not in ('P2', 'P3', 'P4'): continue
            for f, sh in zip(v['fundingShare'], v['shortRegionShare']):
                if f == f and f > 1e-9:
                    tot += 1
                    if sh == sh and sh > 0: hit += 1
        share = hit / tot if tot else float('nan'); r0.append(share)
        if share == share and share >= 0.02: k0 += 1
    print(f'a0 場面がある（供給が尽きた四半期の割合 ≧2%）：{k0}/{NS} {verdict(k0)}  種ごと={[round(x, 3) for x in r0]}')
    # a1 買えた割合 Σtake×price ÷ Σwant×price（国×期の中央・P2〜P4）≧0.8
    k1 = 0; r1 = []
    for s in range(1, NS + 1):
        vals = []
        for (n, p), v in edB[s].items():
            if p not in ('P2', 'P3', 'P4'): continue
            w = sum(x for x in v['stateWantMoneyPerMonth'] if x == x); t = sum(x for x in v['stateTakeMoneyPerMonth'] if x == x)
            if w > 0: vals.append(t / w)
        m = med(vals); r1.append(m)
        if m == m and m >= 0.8: k1 += 1
    print(f'a1 買えた割合（取れた÷買いたい ≧0.8）：{k1}/{NS} {verdict(k1)}  種ごと={[round(x, 3) for x in r1]}')
    # a2 値段に出る：B÷A（priceOverRefTimesLevelIndex）を a0 の場面があった国×期で P2〜P4 の中央が 1.00〜1.30
    k2 = 0; r2 = []
    if haveA:
        for s in range(1, NS + 1):
            vals = []
            for (n, p), v in edB[s].items():
                if p not in ('P2', 'P3', 'P4'): continue
                if not any(x == x and x > 0 for x in v['shortRegionShare']): continue
                a = edA[s].get((n, p))
                if not a: continue
                pb, pa = med(v['priceOverRefTimesLevelIndex']), med(a['priceOverRefTimesLevelIndex'])
                if pb == pb and pa == pa and pa > 0: vals.append(pb / pa)
            m = med(vals); r2.append(m)
            if m == m and 1.0 <= m <= 1.30: k2 += 1
        print(f'a2 値段に出る（B÷A が 1.00〜1.30）：{k2}/{NS} {verdict(k2)}  種ごと={[round(x, 3) if x == x else None for x in r2]}')
    else:
        print('a2 A の ed_s が無い＝読めない')
    # a5 値段 P2・P3・P4 ÷ 開幕期 ≦1.5（8 国・どの期も）
    k5 = 0; r5 = []
    for s in range(1, NS + 1):
        worst = 0.0
        for n in NAT8:
            base = edB[s].get((n, 'P0'))
            if not base: continue
            b0 = med(base['priceOverRefTimesLevelIndex'])
            for p in ('P2', 'P3', 'P4'):
                v = edB[s].get((n, p))
                if v and b0 == b0 and b0 > 0:
                    worst = max(worst, med(v['priceOverRefTimesLevelIndex']) / b0)
        r5.append(worst)
        if worst <= 1.5: k5 += 1
    print(f'a5 値段（開幕期の 1.5 倍以内・8 国のどの期も）：{k5}/{NS} {verdict(k5)}  種ごとの最大比={[round(x, 3) for x in r5]}')
    # ---------------- reform_s（識字・教育費） ----------------
    RF = ['educationSpendingShare', 'literacyShare']
    NAT_LIT = ['イギリス', 'プロイセン', '日本', 'アメリカ', '清']
    rfB = {s: load_series(B, s, 'reform', 'nation', 'quarter', RF, NAT_LIT) for s in range(1, NS + 1)}
    rfA = {s: load_series(A, s, 'reform', 'nation', 'quarter', RF, NAT_LIT) for s in range(1, NS + 1)}
    # a4 識字の伸び：4 国の P4 の識字の B ≧ A−0.02 かつ P2→P4 の伸び ≧ A の伸び−0.02
    k4 = 0; r4 = []
    for s in range(1, NS + 1):
        ok = True; det = {}
        for n in ['イギリス', 'プロイセン', '日本', 'アメリカ']:
            b2, b4 = rfB[s].get((n, 'P2')), rfB[s].get((n, 'P4')); a2_, a4_ = rfA[s].get((n, 'P2')), rfA[s].get((n, 'P4'))
            if not (b2 and b4 and a2_ and a4_): continue
            lb2, lb4, la2, la4 = med(b2['literacyShare']), med(b4['literacyShare']), med(a2_['literacyShare']), med(a4_['literacyShare'])
            det[n] = (round(lb4 - la4, 3), round((lb4 - lb2) - (la4 - la2), 3))
            if lb4 < la4 - 0.02 or (lb4 - lb2) < (la4 - la2) - 0.02: ok = False
        r4.append(det)
        if ok: k4 += 1
    print(f'a4 識字の伸び（英・普・日・米・B が A−0.02 以上）：{k4}/{NS} {verdict(k4)}  種ごと(P4差,伸び差)={r4}')
    # a6 自己増幅の歯止め：P4 の識字で 清 ≦ 英（B）かつ 清の B ≦ A+0.05
    k6 = 0; r6 = []
    for s in range(1, NS + 1):
        c, u = rfB[s].get(('清', 'P4')), rfB[s].get(('イギリス', 'P4')); ca = rfA[s].get(('清', 'P4'))
        if not (c and u and ca): continue
        lc, lu, lca = med(c['literacyShare']), med(u['literacyShare']), med(ca['literacyShare'])
        r6.append((round(lc, 3), round(lu, 3), round(lca, 3)))
        if lc <= lu and lc <= lca + 0.05: k6 += 1
    print(f'a6 識字の歯止め（清 ≦ 英 かつ 清の B ≦ A+0.05）：{k6}/{NS} {verdict(k6)}  種ごと(清B,英B,清A)={r6}')
    # ---------------- class_s（教師・技師） ----------------
    # a3 教師の数と所得：B の educationSpendingShare の期の中央 ≧0.5 の国×期で、教師の人口割合と相対所得の B÷A ≧ 1.00
    k3 = 0; r3 = []
    NAT_T = NAT8
    rfB8 = {s: load_series(B, s, 'reform', 'nation', 'quarter', RF, NAT_T) for s in range(1, NS + 1)}
    for s in range(1, NS + 1):
        cb, ca = class_series(B, s), class_series(A, s)
        ratios_n, ratios_i = [], []
        for (n, p), v in rfB8[s].items():
            if p not in ('P2', 'P3', 'P4') or med(v['educationSpendingShare']) < 0.5: continue
            a, b = P[p]
            qn_b = [cb[(n, q)] for q in range(a, b + 1) if (n, q) in cb and cb[(n, q)]['allK'] > 0]
            qn_a = [ca[(n, q)] for q in range(a, b + 1) if (n, q) in ca and ca[(n, q)]['allK'] > 0]
            if not qn_b or not qn_a: continue
            sh_b = med([x['teacherK'] / x['allK'] for x in qn_b]); sh_a = med([x['teacherK'] / x['allK'] for x in qn_a])
            inc_b = med([(x['teacherEarn'] / x['teacherK']) / (x['allEarn'] / x['allK']) for x in qn_b if x['teacherK'] > 0 and x['allEarn'] > 0])
            inc_a = med([(x['teacherEarn'] / x['teacherK']) / (x['allEarn'] / x['allK']) for x in qn_a if x['teacherK'] > 0 and x['allEarn'] > 0])
            if sh_a > 0: ratios_n.append(sh_b / sh_a)
            if inc_a > 0: ratios_i.append(inc_b / inc_a)
        mn, mi = med(ratios_n), med(ratios_i); r3.append((round(mn, 3) if mn == mn else None, round(mi, 3) if mi == mi else None, len(ratios_n)))
        if mn == mn and mi == mi and mn >= 1.0 and mi >= 1.0: k3 += 1
    print(f'a3 教師の数と所得（教育費の高い国×期で B÷A ≧1.00）：{k3}/{NS} {verdict(k3)}  種ごと(人口割合比,相対所得比,国×期の数)={r3}')
    # 記録：世界の技師の総数 W の 1865 年以後の B÷A（class_s の 技師 sizeK の全国の合計・四半期の中央）
    print('--- 記録（採否に使わない）---')
    for s in range(1, NS + 1):
        cb, ca = class_series(B, s), class_series(A, s)
        def world(c, q): return sum(v['engK'] for (n, qq), v in c.items() if qq == q)
        q0 = (1865 - 1783) * 4
        rb = [world(cb, q) for q in range(q0, 668, 4)]; ra = [world(ca, q) for q in range(q0, 668, 4)]
        pairs = [x / y for x, y in zip(rb, ra) if y > 0]
        print(f'  seed {s}: 世界の技師の総数 W の B÷A（1865 年以後・年ごとの中央）={med(pairs):.3f}')


main()
