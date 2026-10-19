#!/usr/bin/env python
# (452) 事前登録 [8] の狙い a0〜a6 と記録を、計器つき 10 種（B）と基線（A＝m451_ins）の生の表から読む。
# 使い方：PYTHONUTF8=1 python an_ed452.py <B の ins フォルダ> <A の ins フォルダ> [種の数=10]
#   ed_s（B・A の両方に出る＝旗に依らず出す）・reform_s・class_s を読む。国×期の値は、期の中の四半期の中央値→種どうしの中央値。
#   判定：7 種以上＝当たり・4〜6＝部分・3 以下＝外れ（種ごとに条件を満たすかを数える）。
#   ★追記 2（2026-10-20・腕の前）：採否の線は a1・a5・a6 だけ（NaN・欠けた行・A/B の欠けは不合格に倒す＝fail-closed）。a2・a3・a4・a7・a8・a0 は記録（理由は事前登録の追記 2）。
#   a2 は A の ed_s が無いので A・B の pop_s の eduCostFull（教育の必要量×値段×120 か月＝値段に比例する列）で読む。
#   a3・a4 は種ごとの合否でなく 10 種の中央値で、ヌル対（効き 0）の中央値の幅と並べて出す（ヌル対の読み＝同じ読み手を <B=n2 A=n1> などで走らせる）。
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
    ED_COLS = ['fundingShare', 'needPerMonth', 'availablePerMonth', 'stateWantPerMonth', 'stateTakePerMonth', 'stateWantMoneyPerMonth', 'stateTakeMoneyPerMonth', 'shortRegionShare', 'priceOverRefTimesLevelIndex', 'demandSignalPerMonth', 'producedPerMonth', 'soldPerMonth']
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
    print(f'a0〔記録〕場面がある（供給が尽きた四半期の割合 ≧2%）：{k0}/{NS} {verdict(k0)}  種ごと={[round(x, 3) for x in r0]}')
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
    # a7 買えた割合の上がり（金額で束ねた値）：8 国×P2〜P4 の Σ取れた金額÷Σ買いたい金額 の B − A（同じ種）≧ +0.10。国×期の中央で束ねた値の B−A は記録に出す
    def pooled(ed, s):
        w = t = 0.0
        for (n, p), v in ed[s].items():
            if p not in ('P2', 'P3', 'P4'): continue
            w += sum(x for x in v['stateWantMoneyPerMonth'] if x == x); t += sum(x for x in v['stateTakeMoneyPerMonth'] if x == x)
        return t / w if w > 0 else float('nan')
    def take_ratio(ed, s):
        vals = []
        for (n, p), v in ed[s].items():
            if p not in ('P2', 'P3', 'P4'): continue
            w = sum(x for x in v['stateWantMoneyPerMonth'] if x == x); t = sum(x for x in v['stateTakeMoneyPerMonth'] if x == x)
            if w > 0: vals.append(t / w)
        return med(vals)
    k7 = 0; r7 = []; r7m = []
    if haveA:
        for s in range(1, NS + 1):
            d = pooled(edB, s) - pooled(edA, s); r7.append(d); r7m.append(take_ratio(edB, s) - take_ratio(edA, s))
            if d == d and d >= 0.10: k7 += 1
        print(f'a7〔記録〕買えた割合の上がり（金額で束ねた値の B−A ≧ +0.10）：{k7}/{NS} {verdict(k7)}  種ごと={[round(x, 3) if x == x else None for x in r7]}  〔記録：国×期の中央で束ねた値の B−A={[round(x, 3) if x == x else None for x in r7m]}〕')
    # a8 供給の応答：availablePerMonth÷needPerMonth の B÷A（国庫が買う率が正の国×期・P2〜P4 の中央）≧ 1.00
    k8 = 0; r8 = []
    if haveA:
        for s in range(1, NS + 1):
            vals = []
            for (n, p), v in edB[s].items():
                if p not in ('P2', 'P3', 'P4'): continue
                a = edA[s].get((n, p))
                if not a: continue
                def rate(x):
                    num = med(x['availablePerMonth']); den = med(x['needPerMonth'])
                    return num / den if den == den and den > 0 else float('nan')
                rb, ra = rate(v), rate(a)
                if rb == rb and ra == ra and ra > 0 and med(v['fundingShare']) > 1e-9: vals.append(rb / ra)
            m = med(vals); r8.append(m)
            if m == m and m >= 1.0: k8 += 1
        print(f'a8〔記録〕供給の応答（供給÷必要量の B÷A ≧ 1.00）：{k8}/{NS} {verdict(k8)}  種ごと={[round(x, 3) if x == x else None for x in r8]}')
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
        print(f'a2〔記録〕値段に出る（ed_s の B÷A が 1.00〜1.30）：{k2}/{NS} {verdict(k2)}  種ごと={[round(x, 3) if x == x else None for x in r2]}')
    else:
        print('a2 A の ed_s が無い＝ed_s では読めない（下の pop_s の eduCostFull で読む）')
    # a2'：A・B の両方に出る pop_s の eduCostFull（教育の必要量×値段×120 か月＝値段に比例）の B÷A。B の ed_s が供給が尽きた州を持つ国×期だけ（a0 の場面）
    k2p = 0; r2p = []
    def pop_edu(base, s):
        acc = {}
        for d in rows(f'{base}pop_s{s}.tsv'):
            if d['scope'] != 'N' or d['nation'] not in NAT8: continue
            try: q = int(d['quarter'])
            except Exception: continue
            for p in per_of(q):
                acc.setdefault((d['nation'], p), []).append(num(d['eduCostFull']))
        return acc
    for s in range(1, NS + 1):
        pb, pa = pop_edu(B, s), pop_edu(A, s); vals = []
        for (n, p), v in edB[s].items():
            if p not in ('P2', 'P3', 'P4'): continue
            if not any(x == x and x > 0 for x in v['shortRegionShare']): continue
            xb, xa = med(pb.get((n, p), [])), med(pa.get((n, p), []))
            if xb == xb and xa == xa and xa > 0: vals.append(xb / xa)
        m = med(vals); r2p.append(m)
        if m == m and 1.0 <= m <= 1.30: k2p += 1
    print(f'a2p〔記録〕値段に出る（pop_s の eduCostFull の B÷A が 1.00〜1.30）：{k2p}/{NS} {verdict(k2p)}  種ごと={[round(x, 3) if x == x else None for x in r2p]}  10 種の中央値={med(r2p):.3f}')
    # a5 値段 P2・P3・P4 ÷ 開幕期 ≦1.5（8 国・どの期も）
    # ★fail-closed（追記 2）：8 国×3 期のどれかで行が欠ける・NaN・開幕期が正でない種は不合格（worst＝inf）。Python の max(0.0, nan) は 0.0 を返す＝NaN を無視して合格に化けない書き方にする。
    k5 = 0; r5 = []; p10 = []; hi27 = []; lost = []
    for s in range(1, NS + 1):
        worst = 0.0; bad = False; cnt27 = 0
        for n in NAT8:
            base = edB[s].get((n, 'P0'))
            b0 = med(base['priceOverRefTimesLevelIndex']) if base else float('nan')
            if not (b0 == b0 and b0 > 0): bad = True; continue
            for p in ('P2', 'P3', 'P4'):
                v = edB[s].get((n, p))
                pv = med(v['priceOverRefTimesLevelIndex']) if v else float('nan')
                if not (pv == pv and pv > 0): bad = True; continue
                worst = max(worst, pv / b0)
                if pv >= 2.7: cnt27 += 1
        worst = float('inf') if bad else worst
        r5.append(worst); hi27.append(cnt27)
        if worst <= 1.5: k5 += 1
        if worst != float('inf') and worst > 10: p10.append(s)
        if worst == float('inf'): lost.append(s)
    print(f'a5 値段（開幕期の 1.5 倍以内・8 国のどの期も）：{k5}/{NS} {verdict(k5)}  種ごとの最大比={[round(x, 3) for x in r5]}')
    print(f'止める線② 発散：開幕期の 10 倍超の種={p10 or "なし"}・値が欠けた／NaN の種（不合格に倒した）={lost or "なし"}・帯の上限の近く〈国×期の中央が 2.7 以上〉の数（種ごと）={hi27}（1 件でもあれば止めて A 側と見比べる）')
    # ---------------- reform_s（識字・教育費） ----------------
    RF = ['educationSpendingShare', 'literacyShare']
    NAT_LIT = ['イギリス', 'プロイセン', '日本', 'アメリカ', '清']
    rfB = {s: load_series(B, s, 'reform', 'nation', 'quarter', RF, NAT_LIT) for s in range(1, NS + 1)}
    rfA = {s: load_series(A, s, 'reform', 'nation', 'quarter', RF, NAT_LIT) for s in range(1, NS + 1)}
    # a4 識字の伸び（記録・追記 2）：種ごとの合否は効き 0 でも外れになる（ヌル対 0〜2/10）＝10 種の中央値の B−A を国ごとに出す。ヌル対の中央値の幅と並べて読む
    k4 = 0; r4 = []; dif = {n: ([], []) for n in ['イギリス', 'プロイセン', '日本', 'アメリカ']}
    for s in range(1, NS + 1):
        ok = True; det = {}
        for n in ['イギリス', 'プロイセン', '日本', 'アメリカ']:
            b2, b4 = rfB[s].get((n, 'P2')), rfB[s].get((n, 'P4')); a2_, a4_ = rfA[s].get((n, 'P2')), rfA[s].get((n, 'P4'))
            if not (b2 and b4 and a2_ and a4_): ok = False; continue
            lb2, lb4, la2, la4 = med(b2['literacyShare']), med(b4['literacyShare']), med(a2_['literacyShare']), med(a4_['literacyShare'])
            det[n] = (round(lb4 - la4, 3), round((lb4 - lb2) - (la4 - la2), 3))
            dif[n][0].append(lb4 - la4); dif[n][1].append((lb4 - lb2) - (la4 - la2))
            if lb4 < la4 - 0.02 or (lb4 - lb2) < (la4 - la2) - 0.02: ok = False
        r4.append(det)
        if ok: k4 += 1
    print(f'a4〔記録〕識字の種ごとの合否（効き 0 でも外れになる線）：{k4}/{NS}')
    print('a4〔記録〕10 種の中央値の B−A（P4 の識字・P2→P4 の伸び）：' + '・'.join(f'{n} {med(v[0]):+.3f}/{med(v[1]):+.3f}' for n, v in dif.items()))
    # a6 自己増幅の歯止め：P4 の識字で 清 ≦ 英（B）かつ 清の B ≦ A+0.05
    k6 = 0; r6 = []
    for s in range(1, NS + 1):
        c, u = rfB[s].get(('清', 'P4')), rfB[s].get(('イギリス', 'P4')); ca = rfA[s].get(('清', 'P4'))
        if not (c and u and ca): r6.append(None); continue   # 欠けた種は不合格（fail-closed）
        lc, lu, lca = med(c['literacyShare']), med(u['literacyShare']), med(ca['literacyShare'])
        r6.append((round(lc, 3), round(lu, 3), round(lca, 3)))
        if lc == lc and lu == lu and lca == lca and lc <= lu and lc <= lca + 0.05: k6 += 1
    print(f'a6 識字の歯止め（清 ≦ 英 かつ 清の B ≦ A+0.05）：{k6}/{NS} {verdict(k6)}  種ごと(清B,英B,清A)={r6}')
    # ---------------- class_s（教師・技師） ----------------
    # a3 教師の数と所得：B の educationSpendingShare の期の中央 ≧0.5 の国×期で、教師の人口割合と相対所得の B÷A ≧ 1.00
    k3 = 0; r3 = []; all_n = []; all_i = []
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
        all_n.append(mn); all_i.append(mi)
    print(f'a3〔記録〕教師の種ごとの合否（効き 0 でも外れになる線）：{k3}/{NS}  種ごと(人口割合比,相対所得比,国×期の数)={r3}')
    print(f'a3〔記録〕10 種の中央値：教師の人口割合の B÷A={med(all_n):.3f}・相対所得の B÷A={med(all_i):.3f}')
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
