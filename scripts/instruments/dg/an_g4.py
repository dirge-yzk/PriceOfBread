# 流れG (360) 後半：伝わりの道（DiscoveryCarrierDownstream）の腕 A・B・N1・N2 を計器つき 10 種（NATIONS=*）で並べて読む（読むだけ）。
# 事前登録＝measurements/g4_20261016_prereg.txt ＋追記 measurements/g4_20261016_prereg_amend.txt（追記が本文を上書きする）。
# 使い方：PYTHONUTF8=1 python an_g4.py <A の ins> <B の ins> <N1 の ins> <N2 の ins>
# 出す物：L1（雑音の床つき）・L1c・L2a（日本・デンマーク）・L2b（記録）・L2c（新）・L3・L3e（an_g3.py の関数）・L3x・R1・R2（分子と分母の内訳）・R5・R6・R8
import sys, os, glob, re, statistics, collections

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from an_dg2 import Arm, fl, load   # 読むだけの共通部（disc_s の E 行・T 行・汎用の表の読み込み）
import an_g3 as g3

CENSOR = 1950
JP7 = ['日本', 'デンマーク', 'シャム', 'モロッコ', 'ネパール', 'アワド', '西山朝']
L2A = {'日本': (1850, 1880), 'デンマーク': (1790, 1850)}          # 採否の線の分母（追記 6）
REC = {'シャム': (1860, 1900)}                                   # 記録だけ
JP4 = ['日本', 'デンマーク', 'シャム', 'モロッコ']               # L2c の 4 国
SIDE_ROWS = ['汽船', '機関車', '内燃機関', '自動車', '装甲艦', '機関銃', '戦車', '軍用機']
CHANGED_ROWS = ['蒸気機関'] + SIDE_ROWS
DOWN = ('機関車', '汽船')


def first_year(a, sd, nation, row='蒸気機関'):
    y = a.year(sd, nation, row)
    return y if y is not None else CENSOR


def med_year(a, nation, row='蒸気機関'):
    return statistics.median([first_year(a, sd, nation, row) for sd in a.seeds])


def n_unlocked(a, nation, row='蒸気機関'):
    return sum(1 for sd in a.seeds if a.year(sd, nation, row) is not None)


def unlock_q(a, sd, nation, row='蒸気機関'):
    r = a.E.get((sd, nation, row))
    return int(r['quarter']) if r else 10 ** 9


def load_contact(d):
    return load(d, 'disccontact')


def load_flow(d):
    return load(d, 'discflow')


def l1(arms, names):
    A, B, N1, N2 = arms
    print('## L1（雑音の床つき）：種ごとの Δ＝A−B（1783〜窓の終わりに蒸気機関を解禁した英以外の国の数）が床 max(3, 2×max(|A−N1|, |A−N2|)) を超える種が 8/10 以上')
    res = {}
    for w in (1784, 1788, 1792):
        ok = 0; rows = []
        for sd in A.seeds:
            cnt = [len(x.steam_nations(sd, w)) if sd in x.seeds else None for x in arms]
            if None in cnt: continue
            a, b, n1, n2 = cnt
            floor = max(3, 2 * max(abs(a - n1), abs(a - n2)))
            hit = (a - b) > floor
            ok += hit
            rows.append(f'  種{sd}: A {a}・B {b}・N1 {n1}・N2 {n2}・Δ {a - b}・床 {floor}・B/A {b / a if a else float("nan"):.3f}{"  ○" if hit else ""}')
        res[w] = ok
        print(f'窓 〜{w}: 超えた種 {ok}/{len(A.seeds)}')
        if w == 1792:
            print('\n'.join(rows))
    print(f'L1（〜1792）：{res[1792]}/{len(A.seeds)}＝{"合格" if res[1792] >= 8 else "★外れ"}（記録：〜1784 {res[1784]}・〜1788 {res[1788]}）')
    return res[1792] >= 8


def l1c(arms, names):
    print('\n## L1c：清の蒸気機関の最初の年が 1783〜84 の種（B・線＝3/10 以下）')
    for n, a in zip(names, arms):
        c = sum(1 for sd in a.seeds if (a.year(sd, '清', '蒸気機関') or 9999) <= 1784)
        print(f'  {n}: {c}/{len(a.seeds)}')
    c = sum(1 for sd in arms[1].seeds if (arms[1].year(sd, '清', '蒸気機関') or 9999) <= 1784)
    print(f'L1c：{c}/{len(arms[1].seeds)}＝{"合格" if c <= 3 else "★外れ"}')
    return c <= 3


def l2a(arms, names):
    print('\n## L2a：日本・デンマークの種の中央値の最初の年（解禁しない種は 1950・偶数個は中の 2 つの平均）が出典の幅に入る（両方）')
    ok = True
    for nm, (lo, hi) in {**L2A, **REC}.items():
        row = f'  {nm} 幅[{lo},{hi}]'
        for n, a in zip(names, arms):
            m = med_year(a, nm)
            row += f' | {n} m={m:g}（解禁 {n_unlocked(a, nm)}/{len(a.seeds)}）{"入る" if lo <= m <= hi else "外"}'
        print(row + ('（記録のみ）' if nm in REC else ''))
        if nm in L2A:
            m = med_year(arms[1], nm)
            ok &= lo <= m <= hi
    print(f'L2a：{"合格" if ok else "★外れ"}')
    return ok


def l2b(arms, names):
    print('\n## L2b（記録）：日本型 7 国の 1950 年まで解禁しない国×種（G3 単独の記録＝48・生の TSV なし）')
    for n, a in zip(names, arms):
        tot = sum(len(a.seeds) - n_unlocked(a, nm) for nm in JP7)
        per = '・'.join(f'{nm}{len(a.seeds) - n_unlocked(a, nm)}' for nm in JP7)
        print(f'  {n}: {tot}／{len(JP7) * len(a.seeds)}（{per}）')


def l2c(dirs, arms, names):
    print('\n## L2c（機構の確かめ・採否の線。効きの証拠に数えない）：row＝蒸気機関の接触の分子のうち、解禁前（解禁しない種は全期間）の合計で carrier＝機関車／汽船の割合（B・4 国中 3 国以上で 25% 以上）')
    out = {}
    for d, n, a in zip(dirs, names, arms):
        con = load_contact(d)
        has_col = any('carrier' in r for rows in con.values() for r in rows[:1])
        print(f'  {n}: carrier 列 {"あり" if has_col else "なし"}')
        passes = 0
        for nm in JP4:
            down = other = 0.0; nrow = 0
            for sd in a.seeds:
                uq = unlock_q(a, sd, nm)
                for r in con.get(sd, []):
                    if r['buyer'] != nm or r['row'] != '蒸気機関' or int(r['quarter']) > uq: continue
                    v = fl(r['valuePerQuarter']); nrow += 1
                    if r.get('carrier', '蒸気機関') in DOWN: down += v
                    else: other += v
            sh = down / (down + other) if down + other > 0 else float('nan')
            print(f'    {nm}: 機関車／汽船経由 {down:.4g}・蒸気機関の財経由 {other:.4g}・割合 {sh:.1%}（行 {nrow}）')
            if sh >= 0.25: passes += 1
        out[n] = passes
    print(f'L2c（B）：4 国中 {out[names[1]]} 国が 25% 以上＝{"合格" if out[names[1]] >= 3 else "★外れ"}／A の確かめ：機関車／汽船経由は row＝蒸気機関で 0 のはず（上の A の割合）')
    return out[names[1]] >= 3


def l3x(arms, names):
    A = arms[0]
    nulls = [a for n, a in zip(names[1:], arms[1:]) if n.upper().startswith('N')]
    print('\n## L3x（記録）：副作用の行 8 本×工業国 8 か国で、A より床 max(5, 2×max|m(N)−m(A)|) を超えて早まった国×行（B）')
    for n, a in zip(names[1:], arms[1:]):
        if n.upper().startswith('N'): continue
        hit = []
        for nm in g3.INDUSTRIAL:
            for row in SIDE_ROWS:
                mA = med_year(A, nm, row); m = med_year(a, nm, row)
                fl_ = max(5, 2 * max([abs(med_year(x, nm, row) - mA) for x in nulls] or [0]))
                if mA - m > fl_: hit.append(f'{nm}{row}({mA:g}→{m:g}・床{fl_:g})')
        print(f"  {n}：{len(hit)} 件（{'・'.join(hit) or '—'}）{'★5 以上＝報告の先頭に書く' if len(hit) >= 5 else ''}")


def r2(dirs, arms, names):
    print('\n## R2（伝わりの内訳）：日本型 7 国の蒸気機関の行の分子（運び手の財ごと）／分母（discflow_s の世界の越境の取引額）')
    wins = [(1, 40), (41, 120), (121, 240), (241, 400)]
    for d, n, a in zip(dirs, names, arms):
        con = load_contact(d); flow = load_flow(d)
        print(f'  {n}:')
        for lo, hi in wins:
            num = collections.Counter(); den = collections.Counter()
            late_loco = loco_all = 0.0
            for sd in a.seeds:
                for r in con.get(sd, []):
                    q = int(r['quarter'])
                    if not (lo <= q <= hi) or r['buyer'] not in JP7 or r['row'] != '蒸気機関': continue
                    g = r.get('carrier', '蒸気機関'); v = fl(r['valuePerQuarter'])
                    num[g] += v
                    if g == '機関車':
                        loco_all += v
                        if q < unlock_q(a, sd, r['buyer'], '機関車'): late_loco += v   # 買い手が機関車の行を未解禁の四半期の機関車の買い
                for r in flow.get(sd, []):
                    q = int(r['quarter'])
                    if lo <= q <= hi: den[r['good']] += fl(r['valueFlowQ'])
            gs = sorted(set(num) | set(den))
            if not gs: print(f'    Q{lo}-{hi}: —'); continue
            line = '・'.join(f'{g}[分子 {num[g]:.3g}／分母 {den[g]:.3g}]' for g in gs)
            extra = f'／機関車の買いのうち買い手が機関車の行を未解禁の時 {late_loco / loco_all:.1%}（{loco_all:.3g}）' if loco_all > 0 else ''
            print(f'    Q{lo}-{hi}: {line}{extra}')


def r5(arms, names):
    print('\n## R5：運び手の表が変わる 9 行の最初の年の中央値（解禁しない種は 1950）・工業国 8 か国と日本型 7 国')
    for nm in list(g3.INDUSTRIAL) + JP7:
        parts = []
        for row in CHANGED_ROWS:
            v = [med_year(a, nm, row) for a in arms]
            if v[0] != v[1]: parts.append(f'{row}:{v[0]:g}→{v[1]:g}(N1 {v[2]:g}/N2 {v[3]:g})')
        print(f"  {nm}: {'・'.join(parts) or '9 行とも A と同じ'}")


def r6(arms, names):
    print('\n## R6：1950 年まで解禁しない国×種（世界・英を除く）・1900 年より後の解禁')
    for n, a in zip(names, arms):
        nations = {nat for (s, nat) in a.first_q if nat != 'イギリス'}
        never = late = 0
        for sd in a.seeds:
            for nat in nations:
                if (sd, nat) not in a.first_q: continue
                y = a.year(sd, nat, '蒸気機関')
                if y is None: never += 1
                elif y > 1900: late += 1
        print(f'  {n}: 解禁しない国×種 {never}・1900 年より後 {late}（⚠この数は disc_s に現れた国×種だけの母集団＝(350) の記録 A 777／G3 単独 975 とは母集団が違う＝比べない。腕どうし A・N1・N2・B で比べる）')


def r8(arms, names):
    print('\n## R8：未解禁の蒸気機関の行の伝わりの点数 transmitTerm（T 行）：日本型 7 国の最大・解禁直前 4 行の平均')
    for nm in JP7:
        row = f'  {nm}'
        for n, a in zip(names, arms):
            mx = 0.0; last4 = []
            for sd in a.seeds:
                ts = a.T.get((sd, nm, '蒸気機関'), [])
                if not ts: continue
                vals = [fl(t['transmitTerm']) for t in ts]
                mx = max(mx, max(vals)); last4.append(sum(vals[-4:]) / len(vals[-4:]))
            row += f' | {n} 最大 {mx:.1f}・直前4行平均 {statistics.mean(last4) if last4 else float("nan"):.2f}（T のある種 {len(last4)}）'
        print(row)


if __name__ == '__main__':
    dirs = sys.argv[1:5]
    names = ['A', 'B', 'N1', 'N2']
    arms = [Arm(d) for d in dirs]
    r = {}
    r['L1'] = l1(arms, names)
    r['L1c'] = l1c(arms, names)
    r['L2a'] = l2a(arms, names)
    l2b(arms, names)
    r['L2c'] = l2c(dirs, arms, names)
    g3.l3(arms, names)
    g3.l3e(arms, names)
    l3x(arms, names)
    g3.r1(arms, names)
    r2(dirs, arms, names)
    r5(arms, names)
    r6(arms, names)
    r8(arms, names)
    g3.r4r5(arms, names)
    g3.r6(arms, names)
    print('\n## まとめ（L3・L3e は上の表の判定）：', {k: ('合格' if v else '★外れ') for k, v in r.items()})
