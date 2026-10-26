# (572) 線 dsc の読み手：発見の式の作り直し（実現可能性×必要性）の機構の観測量（事前登録 measurements/dsc572_20261026_prereg.txt [4]）。
# 使い方：PYTHONUTF8=1 python an_dsc.py <B の ins> <基線の ins> [種の起点,種の数] [年の上限]
#   例）PYTHONUTF8=1 python an_dsc.py D:/PoB_work/dsc_572/out_B/ins D:/PoB_work/keep_560/m560_ins 1,10
# 読むのは計器 engineer_s*.tsv の kind=E（国が行を解禁した事象・列＝quarter・year・nation・row・decade・engineerPerMonth・techTerm・needTerm）と
# clock_s*.tsv の kind=G（主要国の財ごとの技師の雇用・census）。読むだけ。期待値を本体と同じ出所から作らない（前提の対は手書き・主要国の名前は手書き）。
# 限界（自己申告）：engineer_s の E 行は計器つきの走りだけ／国の名前は表示名（改称・建国で別の名前）／種 11〜20 の基線は無い＝G5 は絶対値の線も出す。
import csv, collections, statistics as st, sys

MAIN = ['イギリス', 'フランス', 'プロイセン', 'オーストリア', 'ロシア', 'アメリカ', '清', '日本']
# 追加の前提（子, 親）＝旗 1 の表（手書き・DiscoveryFeasibilityTests と同じ一覧を別の出所として持つ）
EDGES = [('汽船', '蒸気機関'), ('機関車', '蒸気機関'), ('転炉鋼', '蒸気機関'), ('石炭火力', '蒸気機関'), ('電動機', '石炭火力'), ('送電網', '石炭火力'),
         ('電化製品', '送電網'), ('蒸気タービン', '石炭火力'), ('魚の缶詰', '缶詰'), ('ソルベー法', 'ルブラン法'),
         ('内燃機関', '蒸気機関'), ('装甲艦', '汽船'), ('弩級戦艦', '装甲艦'), ('戦車', '内燃機関'), ('軍用機', '内燃機関'), ('自動車', '内燃機関'),
         ('自転車', '加硫ゴム'), ('ラジオ', '電化製品'), ('電解', '石炭火力')]
KEY_ROWS = ['蒸気機関', '汽船', '機関車', '転炉鋼', '石炭火力', '電動機', '送電網']


def mainname(n):
    for m in MAIN:
        if n == m or n.endswith(m):
            return m
    return 'プロイセン' if 'ドイツ' in n else None


_cache = {}


def events(d, s):
    k = (d, s)
    if k not in _cache:
        E = []
        with open(f'{d}/engineer_s{s}.tsv', encoding='utf-8') as f:
            for r in csv.DictReader(f, delimiter='\t'):
                if r['kind'] != 'E':
                    continue
                r['quarter'] = int(r['quarter']); r['year'] = int(r['year']); r['decade'] = int(r['decade'])
                E.append(r)
        _cache[k] = E
    return _cache[k]


def g4(d, seeds):
    """1783〜86 年に d≧2（年代 1800 以降）の行を解禁した 国×行 の数（種ごと）。"""
    return {s: sum(1 for r in events(d, s) if r['decade'] >= 1800 and r['year'] <= 1786) for s in seeds}


def china_first(d, seeds):
    a = b = 0
    for s in seeds:
        by = collections.defaultdict(list)
        for r in events(d, s):
            if r['decade'] > 1780:
                by[r['row']].append((r['year'], r['nation']))
        for row, L in by.items():
            y0 = min(y for y, _ in L)
            b += 1
            a += any(mainname(n) == '清' for y, n in L if y == y0)
    return a, b


def within_1y(d, seeds, row):
    """行を最初に解禁した主要国から 1 年以内に解禁した主要国の数（種ごと・主要国が 1 つも無い種は載せない）。"""
    out = {}
    for s in seeds:
        L = sorted((r['year'], mainname(r['nation'])) for r in events(d, s) if r['row'] == row and mainname(r['nation']))
        if L:
            out[s] = len({n for y, n in L if y <= L[0][0] + 1})
    return out


def opened_main(d, seeds, year):
    """年までに主要国のどれかが解禁した行の数（種ごと）＝空の合格の守り。"""
    return {s: len({r['row'] for r in events(d, s) if r['year'] <= year and mainname(r['nation'])}) for s in seeds}


def first_year(d, seeds, row):
    out = []
    for s in seeds:
        ys = [(r['year'], mainname(r['nation'])) for r in events(d, s) if r['row'] == row and mainname(r['nation'])]
        out.append(min(ys) if ys else None)
    return out


def prereq_violations(d, seeds):
    """前提の対：子が解禁された四半期に、同じ国が親を同じ四半期以後に解禁している件数（親が開幕の既知なら E 行が無い＝数えない）。0 でなければ前提が効いていない。"""
    bad = []
    for s in seeds:
        q = collections.defaultdict(dict)
        for r in events(d, s):
            q[r['nation']].setdefault(r['row'], r['quarter'])
        for n, rows in q.items():
            for ch, pa in EDGES:
                if ch in rows and pa in rows and rows[pa] >= rows[ch]:
                    bad.append((s, n, ch, pa, rows[ch], rows[pa]))
    return bad


def steam_reach(d, seeds, year):
    """蒸気機関が仏・米・普のうち 2 国以上へ year 年までに届いた種の数（G8）と、1783〜86 年に英以外の国で開いた件数（段0 と素通りの撤去の確かめ）。"""
    reached = 0
    early_nonuk = 0
    for s in seeds:
        ns = {mainname(r['nation']) for r in events(d, s) if r['row'] == '蒸気機関' and r['year'] <= year}
        if len(ns & {'フランス', 'アメリカ', 'プロイセン'}) >= 2:
            reached += 1
        early_nonuk += sum(1 for r in events(d, s) if r['row'] == '蒸気機関' and r['year'] <= 1786 and mainname(r['nation']) != 'イギリス')
    return reached, early_nonuk


def g4b(d, seeds):
    """1800 年までに d≧2 を解禁した 国×行（種ごと）。結果を見た後に足した観測量＝種 11〜20 で判定する。"""
    return {s: sum(1 for r in events(d, s) if r['decade'] >= 1800 and r['year'] <= 1800) for s in seeds}


def verdict(ok):
    return 'PASS' if ok else 'FAIL'


def main():
    b, a = sys.argv[1], sys.argv[2]
    s0, n = (int(x) for x in (sys.argv[3] if len(sys.argv) > 3 else '1,10').split(','))
    seeds = list(range(s0, s0 + n))
    ylim = int(sys.argv[4]) if len(sys.argv) > 4 else 1913
    has_a = a not in ('-', '')
    gb = g4(b, seeds)
    print(f'# an_dsc B={b} A={a} 種={seeds[0]}..{seeds[-1]} 年の上限={ylim}')
    print(f'G4 1783〜86 年に d≧2 を解禁した 国×行（狙い 0）B={gb} 合計 {sum(gb.values())}')
    print(f'   判定 {verdict(sum(gb.values()) == 0)}')
    cb = china_first(b, seeds)
    ra = cb[0] / max(cb[1], 1)
    if has_a:
        cA = china_first(a, seeds)
        base = cA[0] / max(cA[1], 1)
        print(f'G5 最初の発見者に清が入る行の割合 B={cb[0]}/{cb[1]}={ra:.3f}  A={cA[0]}/{cA[1]}={base:.3f}（狙い B ≦ A＋0.08）  判定 {verdict(ra <= base + 0.08)}')
    else:
        print(f'G5 最初の発見者に清が入る行の割合 B={cb[0]}/{cb[1]}={ra:.3f}（基線なし・絶対の線 ≦ 0.619＋0.08＝0.699〔種 1〜10 の基線 0.619 に同じ +0.08〕）  判定 {verdict(ra <= 0.699)}')
    ob = opened_main(b, seeds, ylim)
    opened_seeds = sum(1 for v in ob.values() if v > 0)
    mb = st.median(ob.values())
    if has_a:
        oa = opened_main(a, seeds, ylim)
        ma = st.median(oa.values())
        g6a_ok = mb >= ma * 2 / 3 and mb > 0
        print(f'G6a 主要国が {ylim} 年までに解禁した行の数（空の合格の守り・B ≧ A の 2/3）B 中央 {mb:.1f} {ob}  A 中央 {ma:.1f}  判定 {verdict(g6a_ok)}')
    else:
        g6a_ok = mb > 0
        print(f'G6a B 中央 {mb:.1f} {ob}（基線なし＝開いた行が無ければ不合格だけ読む）  判定 {verdict(g6a_ok)}')
    g6b_ok = True
    for row in ['機関車', '汽船', '転炉鋼']:
        w = within_1y(b, seeds, row)
        med = st.median(w.values()) if w else float('nan')
        mx = max(w.values()) if w else float('nan')
        print(f'G6b {row}：最初の主要国から 1 年以内の主要国の数 B={w} 中央 {med}・最大 {mx}（狙い 中央 ≦4・最大 ≦6）  開いた種 {len(w)}/{len(seeds)}')
        if w and not (med <= 4 and mx <= 6):
            g6b_ok = False
    all_rows_opened = all(len(within_1y(b, seeds, r)) * 2 >= len(seeds) for r in ['機関車', '汽船', '転炉鋼'])
    print(f'   G6b 判定 {verdict(g6b_ok)}／開いた種が 5/10 未満の行があれば G6a の不合格に含める規則：{"含まれない" if all_rows_opened else "含まれる＝G6a FAIL"}')
    bad = prereq_violations(b, seeds)
    print(f'G7 前提の対の違反（狙い 0）B {len(bad)} 件 {bad[:3]}（⚠Score が前提のまだの行を飛ばすので作りの上で恒真＝番人は xUnit〔旗1は追加の前提がまだの行を判定しない〕へ移した・ここは記録）')
    reach, early = steam_reach(b, seeds, 1830)
    print(f'G8 蒸気機関が仏・米・普のうち 2 国以上へ 1830 年までに届いた種 {reach}/{len(seeds)}（狙い ≧ 7/10 の比）  判定 {verdict(reach * 10 >= 7 * len(seeds))}')
    print(f'   1783〜86 年に英以外で蒸気機関が開いた件数（狙い 0）{early}  判定 {verdict(early == 0)}')
    gb4 = g4b(b, seeds)
    if has_a:
        ga4 = g4b(a, seeds)
        amax = max(ga4.values())
        print(f'G4b 1800 年までに d≧2 を解禁した 国×行（種 11〜20 で判定する観測量）B={gb4} A={ga4}（狙い B の各種 ≦ A の最大＋2.5＝{amax + 2.5}）  判定 {verdict(all(v <= amax + 2.5 for v in gb4.values()))}')
    else:
        print(f'G4b B={gb4}（基線なし）')
    print('R 主要な行の最初の解禁（年, 国）種ごと：')
    for row in KEY_ROWS:
        print(f'   {row}: B={first_year(b, seeds, row)}' + (f'  A={first_year(a, seeds, row)}' if has_a else ''))
    print('凍る行の名簿（主要国のどれかが ylim 年までに解禁した種の数／全種）：')
    for row in ['汽船', '機関車', '転炉鋼', '石炭火力', '電動機', '送電網', '内燃機関', '自動車', '戦車', '軍用機']:
        k = sum(1 for s in seeds if any(r['row'] == row and r['year'] <= ylim and mainname(r['nation']) for r in events(b, s)))
        print(f'   {row}: B {k}/{len(seeds)}')


if __name__ == '__main__':
    main()
