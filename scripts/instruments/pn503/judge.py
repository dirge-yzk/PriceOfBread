#!/usr/bin/env python
# (503) 線 pn の読み手（nd と工程の 3 行の門）。事前登録 measurements/pn_20261023_prereg.txt [5] の PN0〜PN3・PD1 を計器の生から出す。
# op の門（PM1・PM2a〜c・PM3）は scripts/instruments/op494/judge.py（5 行へ広げる前の版）で読む。この読み手は PN と PD だけ。
# 使い方：python judge.py <腕の ins フォルダ> [--base <m500_ins>] [--ref <nd なしの参照の腕の ins〔B1 を読むときの B2〕>]   ※追記 3 で引数を変えた（PN2・PN3 は基線に絞る・PN0/PN4 は --ref があるときだけ・PN1 は B2 では記録）
# 入力：engineer_s<種>.tsv（T 行＝まだ解禁していない行の四半期ごとの断面〔挙げた国〕・E 行＝解禁した四半期）・procrow_s<種>.tsv。出力は標準出力（日本語）。
# 閾値は事前登録のとおり（全て〈推〉）：PN0 ≦5%・PN1 過半の種（≧6/10）で最初の発見者の必要性の順位が 3 位以内の割合 ＞1/2（T 行は全ての国〔約 61〕＝偶然の水準は 3/61）・PN2/PN3 は m500_ins の数以下・PD1 +8≦+16≦+24 かつ +24−+0 ≧0.05。
# ⚠限界（読み手が自己申告する）：T 行は全ての国（engineer_s の T 行は NATIONS に絞らない・約 61 国）＝順位は全ての国の中の順位／PN0 は参照の腕（先頭の次のフォルダ）が要る／製法の行の名簿は Catalog.cs の `DiscoveryField.Process` の行を読む（写し）／
#   procrow の goodOutput は同じ財の全てのレシピの産出の和（三段膨張の財は労働力＝人力と動力の混合）／開いた国の割合は挙げた国の中。
import sys, os, re, csv, glob, statistics, collections
sys.stdout.reconfigure(encoding='utf-8')

HERE = os.path.dirname(os.path.abspath(__file__))
CAT = os.path.join(HERE, '..', '..', '..', 'PriceOfBread.SimCore', 'Catalog.cs')
NEW3 = ['熱風炉', '三段膨張', '自動ミュール']
PD1_GATE = ['熱風炉', '自動ミュール']   # 追記 3：三段膨張は記録（分母の労働力の財に人力・畜力・水車が入る）
TH = {'PN0': 0.05, 'PN1_share': 0.5, 'PN1_seeds': 6, 'PD1_gain': 0.05}

def process_rows():
    s = open(CAT, encoding='utf-8-sig').read()
    names = re.findall(r'new\("([^"]+)",\s*\w+,\s*"[^"]*",\s*DiscoveryField\.Process,', s)
    return set(names)

PROC = process_rows()

def rd(path):
    with open(path, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t')
        h = next(r)
        ix = {k: i for i, k in enumerate(h)}
        for row in r:
            yield ix, row

def fl(x):
    try: return float(x)
    except Exception: return float('nan')

def seeds_of(d):
    out = []
    for p in sorted(glob.glob(os.path.join(d, 'engineer_s*.tsv'))):
        m = re.search(r'engineer_s(\d+)\.tsv$', p)
        if m: out.append(int(m.group(1)))
    return sorted(out)

def load_eng(d, s):
    """T: (row, q) -> {nation: (a, tech, need, replaced, cost, known)}  E: row -> [(q, nation, tech, need)]  dec: row -> decade"""
    T = collections.defaultdict(dict); E = collections.defaultdict(list); dec = {}
    for ix, r in rd(os.path.join(d, f'engineer_s{s}.tsv')):
        k = r[ix['kind']]; q = int(r[ix['quarter']]); row = r[ix['row']]; nat = r[ix['nation']]
        dec[row] = int(r[ix['decade']])
        if k == 'T':
            T[(row, q)][nat] = (fl(r[ix['engineerPerMonth']]), fl(r[ix['techTerm']]), fl(r[ix['needTerm']]), fl(r[ix['replaced']]), fl(r[ix['cost']]), int(r[ix['knownFlag']]))
        elif k == 'E':
            E[row].append((q, nat, fl(r[ix['techTerm']]), fl(r[ix['needTerm']])))
    return T, E, dec

def pn0(d, seeds, ref):
    """分子が nd で立たなくなった割合＝参照（nd なしの腕〔B2 や m500_ins〕）で replaced＞0 だった同じ（行，四半期，国）のうち、この腕で replaced≦0 の割合。
    replaced≦0 は nd と無関係にも起きる（需要が無い財・市場が死んだ財＝旧の式でも立たない）ので、腕の中の絶対値では読まない（canary で全体の 22〜24% が nd なしでも立たなかった）。"""
    n = bad = 0; per = collections.defaultdict(lambda: [0, 0])
    if ref is None: return float('nan'), 0, per
    for s in seeds:
        T, E, dec = load_eng(d, s)
        if not os.path.exists(os.path.join(ref, f'engineer_s{s}.tsv')): continue
        Tr, Er, decr = load_eng(ref, s)
        for (row, q), L in T.items():
            if row not in PROC or (row, q) not in Tr: continue
            for nat, (a, tech, need, rep, cost, known) in L.items():
                v = Tr[(row, q)].get(nat)
                if known or v is None or v[5] or not (v[3] > 0): continue
                n += 1; per[row][0] += 1
                if not (rep > 0): bad += 1; per[row][1] += 1
    return (bad / n if n else float('nan')), n, per

def first_openers(d, seeds):
    """(seed,row) -> (q, nation, need rank in the T snapshot at the largest q'<=q, snapshot size)。decade>1780 の行だけ。"""
    res = []
    for s in seeds:
        T, E, dec = load_eng(d, s)
        qs_by_row = collections.defaultdict(list)
        for (row, q) in T: qs_by_row[row].append(q)
        for row, lst in E.items():
            if dec.get(row, 0) <= 1780: continue
            lst.sort(); q, nat, tech, need = lst[0]
            qs = [x for x in qs_by_row[row] if x <= q]
            if not qs: continue
            snap = {n: v for n, v in T[(row, max(qs))].items() if not v[5]}
            if nat not in snap: continue
            # 追記 3：同点の塊に入ると並びで順位が決まる穴を塞ぐ＝順位は〈必要性が発見者より厳密に大きい国の数＋1〉
            res.append((s, row, q, nat, 1 + sum(1 for n, v in snap.items() if v[2] > snap[nat][2]), len(snap)))
    return res

def pn1(d, seeds):
    fo = first_openers(d, seeds)
    per_seed = {}
    for s in seeds:
        L = [x for x in fo if x[0] == s]
        if L: per_seed[s] = sum(1 for x in L if x[4] <= 3) / len(L)
    ok = sum(1 for v in per_seed.values() if v > TH['PN1_share'])
    return ok, per_seed, fo

def opening_sets(d, seeds):
    opened = collections.defaultdict(set); early = collections.defaultdict(set); present = set()
    for s in seeds:
        T, E, dec = load_eng(d, s)
        for (row, q) in T:
            if dec.get(row, 0) > 1780: present.add(row)
        for row, lst in E.items():
            if dec.get(row, 0) <= 1780: continue
            present.add(row)
            opened[row].add(s)
            if any(q <= 3 for q, *_ in lst): early[row].add(s)
    return present, opened, early

def pn23(d, seeds):
    present, opened, early = opening_sets(d, seeds)
    never = sorted(r for r in present if not opened.get(r))
    early_rows = sorted(r for r, ss in early.items() if len(ss) * 2 >= len(seeds))
    return never, early_rows, present, opened

def pn23_vs_base(d, seeds, base, bseeds):
    """追記 3：PN2＝基線（m500_ins）にもある行について〈どの種でも開かない行〉の数が基線以下・基線に無い行（新しい 5 行）は各々 1 種以上で開く。PN3＝基線にもある行について〈Q0〜Q3 に半分以上の種で開く行〉の数が基線以下。"""
    never, early, present, opened = pn23(d, seeds)
    bnever, bearly, bpresent, bopened = pn23(base, bseeds)
    common = present & bpresent
    new = sorted(present - bpresent)
    n_main = sorted(r for r in never if r in common); n_base = sorted(r for r in bnever if r in common)
    e_main = sorted(r for r in early if r in common); e_base = sorted(r for r in bearly if r in common)
    new_closed = sorted(r for r in new if not opened.get(r))
    return dict(n_main=n_main, n_base=n_base, e_main=e_main, e_base=e_base, new=new, new_closed=new_closed, pn2=(len(n_main) <= len(n_base) and not new_closed), pn3=(len(e_main) <= len(e_base)))

def pd1(d, seeds):
    out = {}
    for row in NEW3:
        shares = {0: [], 8: [], 16: [], 24: []}; n = 0
        for s in seeds:
            p = os.path.join(d, f'procrow_s{s}.tsv')
            if not os.path.exists(p): continue
            ser = collections.defaultdict(dict)
            for ix, r in rd(p):
                if r[ix['row']] != row: continue
                ser[int(r[ix['nationUid']])][int(r[ix['quarter']])] = (int(r[ix['known']]), fl(r[ix['defaultOutputPerMonth']]), fl(r[ix['goodOutputPerMonth']]))
            for uid, qq in ser.items():
                opens = [q for q, v in sorted(qq.items()) if v[0]]
                if not opens: continue
                q0 = opens[0]; qmax = max(qq)
                if q0 + 24 > qmax: continue
                n += 1
                for k in shares:
                    v = qq.get(q0 + k)
                    if v and v[2] > 0: shares[k].append(v[1] / v[2])
        med = {k: (statistics.median(v) if v else float('nan')) for k, v in shares.items()}
        # 追記 3：事前登録の文言どおり＝外れ（+24 ＜ +8 または +8 ＜ +0）。+16 は見ない（以前の版は +16 も単調を要求していた）
        mono = (med[24] >= med[8] - 1e-12 and med[8] >= med[0] - 1e-12) if all(x == x for x in med.values()) else False
        gain = med[24] - med[0] if all(x == x for x in (med[24], med[0])) else float('nan')
        out[row] = (n, med, mono, gain)
    return out


OPROWS = ['コークス製鉄', '機械紡績']

def opcount(d, seeds):
    """op の行ごとに、追う国が開いた延べ（国，種）＝engineer_s の E 行の数（開幕の既知の国は E 行を出さない）。"""
    c = collections.Counter()
    for s in seeds:
        T, E, dec = load_eng(d, s)
        for row in OPROWS: c[row] += len(E.get(row, []))
    return c

def pn4(d, ref, seeds):
    """PN4（追記 2）＝nd が op の行を殺さない：B1 の追う国の開いた延べ ÷ 参照（B2）の延べ ≧ 1/2（行ごと）。"""
    if ref is None: return None
    a = opcount(d, seeds); b = opcount(ref, [s for s in seeds if os.path.exists(os.path.join(ref, f'engineer_s{s}.tsv'))])
    return {r: (a[r], b[r], (a[r] / b[r]) if b[r] else float('nan')) for r in OPROWS}

def report(d, label, ref=None, base=None):
    seeds = seeds_of(d)
    print(f'## {label}  ({d}・種 {seeds})')
    if not seeds: print('  種が無い'); return
    if ref is not None:
        a, n, per = pn0(d, seeds, ref)
        print(f'  PN0 nd で分子が立たなくなった割合（参照 {ref} で立っていた同じ（行，四半期，国））= {a:.4f}（{n} 件）→ {"OK" if a <= TH["PN0"] else "外れ"}（閾値 {TH["PN0"]}）')
        worst = sorted(((v[1] / v[0], k, v[0]) for k, v in per.items() if v[0] >= 20), reverse=True)[:5]
        print('     行ごとの悪い順：' + '・'.join(f'{k} {x:.2f}({c})' for x, k, c in worst))
    else:
        print('  PN0 PN4 nd の腕でないので読まない（B1 を回していない）')
    ok, ps, fo = pn1(d, seeds)
    print(f'  PN1（記録・B2 では nd が無いので門にしない）最初の発見者の必要性の順位が 3 位以内の割合が 1/2 を超えた種 = {ok}/{len(seeds)}（閾値 {TH["PN1_seeds"]}）；全体の割合 {sum(1 for x in fo if x[4] <= 3) / max(1, len(fo)):.3f}（{len(fo)} 件・断面の国の数の中央値 {statistics.median([x[5] for x in fo]) if fo else float("nan")}）')
    never, early, present, opened = pn23(d, seeds)
    print(f'  （素の数）どの種でも開かない行 = {len(never)} 本：{"・".join(never)}')
    print(f'  （素の数）Q0〜Q3 に半分以上の種で開く行 = {len(early)} 本：{"・".join(early)}')
    if base:
        bseeds = seeds_of(base)
        v = pn23_vs_base(d, seeds, base, bseeds)
        print(f'  PN2 基線 {base} にもある行で、どの種でも開かない行：主 {len(v["n_main"])} 本 ≦ 基線 {len(v["n_base"])} 本、かつ新しい行（{"・".join(v["new"])}）が全て 1 種以上で開く（開かない新しい行：{"・".join(v["new_closed"]) or "なし"}）→ {"OK" if v["pn2"] else "外れ"}')
        print(f'     主だけの開かない行：{"・".join(sorted(set(v["n_main"]) - set(v["n_base"]))) or "なし"}／基線だけ：{"・".join(sorted(set(v["n_base"]) - set(v["n_main"]))) or "なし"}')
        print(f'  PN3 基線にもある行で、Q0〜Q3 に半分以上の種で開く行：主 {len(v["e_main"])} 本 ≦ 基線 {len(v["e_base"])} 本 → {"OK" if v["pn3"] else "外れ"}（主だけ：{"・".join(sorted(set(v["e_main"]) - set(v["e_base"]))) or "なし"}）')
    p4v = pn4(d, ref, seeds)
    if p4v:
        for r, (x, y, q) in p4v.items():
            print(f'  PN4 {r}: 追う国の開いた延べ {x} 対 参照 {y}（比 {q:.2f}）→ {"OK" if q >= 0.5 else "外れ（nd が op の行を殺す）"}（閾値 1/2）')
    pdv = pd1(d, seeds)
    for row, (n, med, mono, gain) in pdv.items():
        gate = '門' if row in PD1_GATE else '記録'
        print(f'  PD1（{gate}）{row}: 開いた（国，種）{n} 件・割合の中央値 +0={med[0]:.3f} +8={med[8]:.3f} +16={med[16]:.3f} +24={med[24]:.3f}・向き（+8≧+0 かつ +24≧+8）={mono}・増え={gain:.3f} → {"OK" if mono and gain >= TH["PD1_gain"] else "外れ/読めない"}')

if __name__ == '__main__':
    import argparse
    ap = argparse.ArgumentParser()
    ap.add_argument('main'); ap.add_argument('--base', default=None, help='基線の ins（m500_ins）＝PN2・PN3 の比べる側')
    ap.add_argument('--ref', default=None, help='nd なしの参照の腕の ins（B2）＝PN0・PN4（B1 を読むときだけ）')
    a = ap.parse_args()
    report(a.main, '主', a.ref, a.base)
