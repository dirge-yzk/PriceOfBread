# 流れG ② (336)：標準の計器の disc_s<seed>.tsv を、基線 A・腕 T1・ヌル腕 N1〜N4 で並べて読む（読むだけ・事前登録 measurements/ln_20261015_prereg.txt の線）。
# 使い方：PYTHONUTF8=1 python an_ln.py <名前=ins のフォルダ>...　例：A=…/out_LI_10/ins T1=… T1s=… N1=… N2=… N3=… N4=…　（A だけなら基線の読み。T で始まる名前＝腕・N で始まる名前＝ヌル腕）
# 列は main の disc_s（kind quarter year nation row techTerm needTerm transmitTerm replaced cost techLevel）。解禁の出来事 E は国×行で 1 行（開幕の既知は出来事を持たない）。
# 出す物：S1（前提が未解禁のまま解禁した行の数）・窓の表（蒸気機関の英以外の解禁数・必要性主導と伝わり主導）・機械化の地図（国×行の最初の年の種の中央値）・
#         C1（1783〜92 の解禁数の種対の差）・C2（工業国の蒸気機関の最初の年の種対の差と雑音の床）・C3（主要国が 1950 年までに消えない）・C4（機関車が蒸気機関より先の国の数）。
# ⚠辺の表は Catalog.cs の After と同じ手書きの写し（番人 DiscoveryTierTests が表との一致を見る）。国名は表示名で、改名・建国で同じ国の割合が割れる（nationUid は disc_s に無い）。
import sys, glob, os, re, statistics, collections, json

EDGES = [("汽船", "蒸気機関"), ("機関車", "蒸気機関"), ("内燃機関", "蒸気機関"), ("石炭火力", "蒸気機関"),
         ("装甲艦", "汽船"), ("弩級戦艦", "装甲艦"), ("戦車", "内燃機関"), ("軍用機", "内燃機関"), ("自動車", "内燃機関"),
         ("自転車", "加硫ゴム"), ("合成ゴム", "加硫ゴム"), ("送電網", "石炭火力"), ("水力発電", "石炭火力"), ("蒸気タービン", "石炭火力"),
         ("石油火力", "石炭火力"), ("窒素固定", "石炭火力"), ("電動機", "送電網"), ("電化製品", "送電網"), ("ラジオ", "電化製品"), ("魚の缶詰", "缶詰")]
PARENT = dict(EDGES)
# 腕 T1s は確証の高い辺だけ残す（scripts/instruments/ln/edit_t1s.py の DROP と同じ）＝S1 の数え方はその腕の残した辺で行う
T1S_DROP = {"石炭火力", "合成ゴム", "送電網", "水力発電", "蒸気タービン", "石油火力", "窒素固定", "電動機", "電化製品", "魚の缶詰"}
# ★(338) 腕 B＝T1s から機関車・汽船（蒸気機関の運び手の出口）の辺 1・2 も外す（scripts/instruments/ln/edit_b.py）／条件つきの腕 B2＝B から装甲艦の辺 5 も外す（edit_b2.py）。腕名→外した子の行
ARM_DROP = {"T1s": T1S_DROP, "B": T1S_DROP | {"汽船", "機関車"}, "B2": T1S_DROP | {"汽船", "機関車", "装甲艦"}}
# ★(338) C5＝親の行が消えない（C3 と同じ型の線を、前提の行になる親の行へ広げた）。国は C3 と同じ主要 8 国
C5_ROWS = ["蒸気機関", "汽船", "装甲艦", "弩級戦艦", "内燃機関", "電化製品", "加硫ゴム"]
INDUSTRIAL = ["フランス", "プロイセン", "オーストリア", "ロシア", "アメリカ", "ネーデルラント"]   # 英は開幕の既知
C3_NATIONS = ["フランス", "プロイセン", "オーストリア", "ロシア", "アメリカ", "ネーデルラント", "スペイン", "オスマン"]
MAP_NATIONS = ["イギリス", "フランス", "プロイセン", "オーストリア", "ロシア", "アメリカ", "ネーデルラント", "スペイン", "オスマン", "日本", "清"]
MAP_ROWS = ["蒸気機関", "機関車", "汽船", "石炭火力", "内燃機関", "送電網", "電動機", "装甲艦", "弩級戦艦", "自動車"]
DOWN_ROWS = ["水力発電", "蒸気タービン", "石油火力", "窒素固定", "電化製品", "ラジオ", "自転車", "戦車", "軍用機"]
CENSOR = 1950
INITIAL_KNOWN = {("イギリス", "蒸気機関")}   # world.json の knownDiscoveries（英＝蒸気機関・特許）。特許は辺に無い
ORIGINALS = None


def check_edges():
    """Catalog.cs の `After:` と EDGES の一致を見る（読み手だけ番人が無かった点・反証役 低8）。不一致なら止める。"""
    here = os.path.dirname(os.path.abspath(__file__))
    p = os.path.normpath(os.path.join(here, '..', '..', '..', 'PriceOfBread.SimCore', 'Catalog.cs'))
    try:
        txt = open(p, encoding='utf-8-sig').read()
    except Exception:
        print('（Catalog.cs が読めない＝辺の一致は確かめていない）'); return
    got = {}
    for m in re.finditer(r'new\("([^"]+)",[^\n]*?After: "([^"]+)"', txt):
        got[m.group(1)] = m.group(2)
    # ★(344) 合流で腕 B を既定にした＝Catalog.cs の After は B の 8 本（EDGES の 20 本から ARM_DROP["B"] を外した物）。
    #   EDGES は (336) の器の 20 本のまま（過去の腕 A・T1・T1s・B の読みの数え方）＝木が 20 本でも 8 本でも通す。
    b_edges = {k: v for k, v in PARENT.items() if k not in ARM_DROP["B"]}
    if got != PARENT and got != b_edges:
        print('EDGES（20 本）とも腕 B（8 本）とも Catalog.cs の After が違う', set(got.items()) ^ set(b_edges.items()))
        sys.exit(1)


def originals():
    global ORIGINALS
    if ORIGINALS is None:
        here = os.path.dirname(os.path.abspath(__file__))
        p = os.path.normpath(os.path.join(here, '..', '..', '..', 'PriceOfBread.SimCore', 'world.json'))
        try:
            d = json.load(open(p, encoding='utf-8-sig'))
            ORIGINALS = {n['name'] for n in d['nations']}
        except Exception:
            ORIGINALS = set()
    return ORIGINALS


def load(d):
    out = {}
    for f in sorted(glob.glob(os.path.join(d, 'disc_s*.tsv'))):
        sd = int(re.search(r'disc_s(\d+)\.tsv', f).group(1))
        rows = []
        with open(f, encoding='utf-8') as fh:
            hdr = fh.readline().rstrip('\n').split('\t')
            for l in fh:
                p = l.rstrip('\n').split('\t')
                if len(p) == len(hdr):
                    rows.append(dict(zip(hdr, p)))
        out[sd] = rows
    return out


def fl(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def med(xs):
    xs = [x for x in xs if x is not None and x == x]
    return statistics.median(xs) if xs else None


class Arm:
    def __init__(self, name, d):
        self.name, self.d = name, d
        self.disc = load(d)
        self.seeds = sorted(self.disc)
        self.E = {}
        for sd in self.seeds:
            for r in self.disc[sd]:
                if r['kind'] == 'E':
                    self.E.setdefault((sd, r['nation'], r['row']), r)

    def year(self, sd, n, row):
        if (n, row) in INITIAL_KNOWN:
            return 1783
        r = self.E.get((sd, n, row))
        return int(r['year']) if r else None

    def quarter(self, sd, n, row):
        if (n, row) in INITIAL_KNOWN:
            return -1
        r = self.E.get((sd, n, row))
        return int(r['quarter']) if r else None

    def year_c(self, sd, n, row):
        y = self.year(sd, n, row)
        return CENSOR if y is None else y   # 解禁しなかった種は 1950（打ち切り）

    def steam_set(self, sd, yr):
        return {n for (s, n, row), r in self.E.items() if s == sd and row == '蒸気機関' and int(r['year']) <= yr and n != 'イギリス'}


def skip_events(a):
    """前提が未解禁のまま解禁した行の出来事。戻り値＝(確定 [順序が逆], 確定 [前提が最後まで無い・元の国], 曖昧 [改名・建国で前提の出来事を追えない])。"""
    orig = originals()
    strict, never, amb = [], [], []
    for (sd, n, row), r in a.E.items():
        par = PARENT.get(row)
        if par is None or row in ARM_DROP.get(a.name, ()):
            continue
        qc = int(r['quarter'])
        qp = a.quarter(sd, n, par)
        if qp is not None:
            if qp >= qc:   # 前提と同じ四半期か後（同じ四半期でも判定は前の集合で読む＝同時はあり得ない）
                strict.append((sd, n, row, par, qc, qp))
        elif n in orig:
            never.append((sd, n, row, par, qc))
        else:
            amb.append((sd, n, row, par, qc))
    return strict, never, amb


def window_table(a):
    print(f'## {a.name}：蒸気機関の英以外の解禁数（{len(a.seeds)}種）')
    print('種 | 〜1784 | 〜1788 | 〜1792 | 〜1832 | 〜1950 | うち〜1792で必要性主導(need>0) | 伝わり主導(transmit>0) | 清の最初の年')
    for sd in a.seeds:
        s92 = a.steam_set(sd, 1792)
        nd = sum(1 for n in s92 if fl(a.E[(sd, n, '蒸気機関')]['needTerm']) > 0)
        tr = sum(1 for n in s92 if fl(a.E[(sd, n, '蒸気機関')]['transmitTerm']) > 0)
        print(f"{sd} | {len(a.steam_set(sd, 1784))} | {len(a.steam_set(sd, 1788))} | {len(s92)} | {len(a.steam_set(sd, 1832))} | {len(a.steam_set(sd, 1950))} | {nd} | {tr} | {a.year(sd, '清', '蒸気機関')}")


def first_year_table(a, nations, rows):
    print(f'## {a.name}：最初の年の種の中央値（打ち切り 1950・解禁した種の数）')
    print('国 | ' + ' | '.join(rows))
    for n in nations:
        cells = []
        for row in rows:
            ys = [a.year(s, n, row) for s in a.seeds]
            op = sum(1 for y in ys if y is not None)
            m = med([a.year_c(s, n, row) for s in a.seeds])
            cells.append(f'{m:g}({op})' if m is not None else '-')
        print(f'{n} | ' + ' | '.join(cells))


def led_table(a, nations):
    print(f'## {a.name}：蒸気機関の解禁の出来事で必要性が正だった種の数／伝わりが正だった種の数（解禁した種の内）')
    print('国 | 解禁した種 | need>0 | transmit>0 | 最初の年の中央値')
    for n in nations:
        ev = [a.E[(s, n, '蒸気機関')] for s in a.seeds if (s, n, '蒸気機関') in a.E]
        if not ev:
            print(f'{n} | 0 | - | - | -')
            continue
        print(f"{n} | {len(ev)} | {sum(1 for r in ev if fl(r['needTerm']) > 0)} | {sum(1 for r in ev if fl(r['transmitTerm']) > 0)} | {med([int(r['year']) for r in ev]):g}")


def paired(a, b, nation, row):
    seeds = [s for s in a.seeds if s in b.seeds]
    d = [b.year_c(s, nation, row) - a.year_c(s, nation, row) for s in seeds]
    oa = sum(1 for s in seeds if a.year(s, nation, row) is not None)
    ob = sum(1 for s in seeds if b.year(s, nation, row) is not None)
    return med(d), oa, ob


def loco_first(a):
    """機関車が蒸気機関より先（または同じ四半期）に解禁した国×種の数（(172) の症状）。蒸気機関が最後まで無いのに機関車がある場合も数える。"""
    cnt = 0; per = []
    for sd in a.seeds:
        c = 0
        for (s, n, row), r in a.E.items():
            if s != sd or row != '機関車':
                continue
            qs = a.quarter(sd, n, '蒸気機関')
            if qs is None and n in originals() or (qs is not None and qs >= int(r['quarter'])):
                c += 1
        per.append(c); cnt += c
    return cnt, per


def c5_cells(A, X, nations, rows):
    """X の A に対する C5 の外れたセル（国×行）。外れ＝解禁した種が A−2 を超えて減る、または最初の年の種の中央値（打ち切り 1950）が A の ±15 年の外。"""
    bad = []
    for n in nations:
        for row in rows:
            oa = sum(1 for s in A.seeds if A.year(s, n, row) is not None)
            ox = sum(1 for s in X.seeds if X.year(s, n, row) is not None)
            ya = med([A.year_c(s, n, row) for s in A.seeds]); yx = med([X.year_c(s, n, row) for s in X.seeds])
            if ox < oa - 2 or abs(yx - ya) > 15:
                bad.append((n, row, oa, ox, ya, yx))
    return bad


def c3_count(A, X):
    """C3（蒸気機関・主要 8 国）の外れた国の数（事前登録 [5] の定義＝ot<8 または ot<oa−2 または最初の年が ±15 年の外）。"""
    bad = 0
    for n in C3_NATIONS:
        oa = sum(1 for s in A.seeds if A.year(s, n, '蒸気機関') is not None)
        ot = sum(1 for s in X.seeds if X.year(s, n, '蒸気機関') is not None)
        ya = med([A.year_c(s, n, '蒸気機関') for s in A.seeds]); yt = med([X.year_c(s, n, '蒸気機関') for s in X.seeds])
        bad += (ot < 8) or (ot < oa - 2) or abs(yt - ya) > 15
    return bad


def main():
    if len(sys.argv) < 2:
        print(__doc__); return
    check_edges()
    arms = []
    for s in sys.argv[1:]:
        nm, _, d = s.partition('=')
        arms.append(Arm(nm, d))
    A = arms[0]
    TA = [a for a in arms[1:] if not a.name.startswith('N')]; nulls = [a for a in arms if a.name.startswith('N')]
    for a in [A] + TA:
        strict, never, amb = skip_events(a)
        print(f'## S1 {a.name}：前提が未解禁のまま解禁した行（{len(a.seeds)}種）＝順序が逆または同時 {len(strict)}・前提が最後まで無い（元の国）{len(never)}・曖昧（改名・建国）{len(amb)}')
        cnt = collections.Counter((row, par) for _, _, row, par, *_ in strict + never)
        for (row, par), c in cnt.most_common(12):
            print(f'  {row} ← {par}：{c}')
        per = collections.Counter(sd for sd, *_ in strict + never)
        print('  種ごと：' + ' '.join(f'{sd}:{per.get(sd, 0)}' for sd in a.seeds))
    for a in arms:
        window_table(a)
    for a in [A] + TA:
        led_table(a, INDUSTRIAL + ['スペイン', 'オスマン', '日本', '清'])
        first_year_table(a, MAP_NATIONS, MAP_ROWS)
    for a in [A] + TA:
        c, per = loco_first(a)
        print(f'## C4 {a.name}：機関車が蒸気機関より先か同時の国×種 {c}（種ごと {per}）')
    for T in TA:
        TN = T.name
        print(f'## C1 英以外の 1783〜92 の蒸気機関の解禁数の種対の差（{TN}−A）とヌル腕の差')
        dd = [len(T.steam_set(s, 1792)) - len(A.steam_set(s, 1792)) for s in A.seeds if s in T.seeds]
        print(f'  {TN}−A：{dd}・中央値 {med(dd)}')
        nmed = []
        for nl in nulls:
            nd = [len(nl.steam_set(s, 1792)) - len(A.steam_set(s, 1792)) for s in A.seeds if s in nl.seeds]
            nmed.append(med(nd))
            print(f'  {nl.name}−A：{nd}・中央値 {med(nd)}')
        wid = max([abs(x) for x in nmed if x is not None] or [0])
        md = med(dd)
        c1 = md is not None and md < 0 and md < -wid
        print(f'  C1 判定：{TN}−A の中央値 {md}・ヌル腕の幅 {wid:g} → {"合格（幅の外で減る）" if c1 else "外れ"}')
        print(f'## C2 工業国の蒸気機関の最初の年の種対の差（{TN}−A・中央値・年）と解禁した種 A→{TN}・雑音の床＝max|中央値(N−A)|')
        print(f'国 | {TN}−A | 解禁した種 A→{TN} | ' + ' | '.join(f'{n.name}−A' for n in nulls) + ' | 雑音の床 | 判定')
        for n in INDUSTRIAL:
            dT, oa, ot = paired(A, T, n, '蒸気機関')
            ds = [paired(A, nl, n, '蒸気機関')[0] for nl in nulls]
            floor = max([abs(x) for x in ds if x is not None] or [0])
            tag = '床の外・早まる' if dT is not None and dT < -floor else ('床の外・遅れる' if dT is not None and dT > floor else '床の内')
            print(f'{n} | {dT} | {oa}→{ot} | ' + ' | '.join(str(x) for x in ds) + f' | {floor:g} | {tag}')
        print(f'## C3 主要 8 国の蒸気機関が 1950 年までに解禁する種（A→{TN}）と最初の年の中央値（A→{TN}）')
        bad = 0
        for n in C3_NATIONS:
            oa = sum(1 for s in A.seeds if A.year(s, n, '蒸気機関') is not None)
            ot = sum(1 for s in T.seeds if T.year(s, n, '蒸気機関') is not None)
            ya = med([A.year_c(s, n, '蒸気機関') for s in A.seeds]); yt = med([T.year_c(s, n, '蒸気機関') for s in T.seeds])
            ng = (ot < 8) or (ot < oa - 2) or abs(yt - ya) > 15
            bad += ng
            print(f'  {n}：{oa}→{ot}・{ya:g}→{yt:g}{"  ←外れ" if ng else ""}')
        print(f'  C3 外れた国 {bad}（合格＝0）・ヌル腕の C3 外れた国（雑音の床）：' + ' '.join(f'{nl.name}={c3_count(A, nl)}' for nl in nulls))
        c5 = c5_cells(A, T, C3_NATIONS, C5_ROWS)
        c5n = {nl.name: c5_cells(A, nl, C3_NATIONS, C5_ROWS) for nl in nulls}
        floor5 = max([len(v) for v in c5n.values()] or [0])
        print(f'## C5 親の行が消えない（{TN}・主要 8 国×{len(C5_ROWS)} 行＝{8 * len(C5_ROWS)} セル）：外れたセル {len(c5)}・ヌル腕の外れたセル ' + ' '.join(f'{k}={len(v)}' for k, v in c5n.items()) + f'・雑音の床 {floor5} → {"合格（床の内）" if len(c5) <= floor5 else "外れ（床の外）"}')
        for n, row, oa, ox, ya, yx in c5:
            print(f'  {n} {row}：{oa}→{ox}種・最初の年 {ya:g}→{yx:g}')
        for rows, title in ((MAP_ROWS, '機械化の地図'), (DOWN_ROWS, '下流の地図（石炭火力の子・電気・化学）')):
            print(f'## {title}：国×行の最初の年の種対の差（{TN}−A・中央値・打ち切り 1950）と雑音の床・解禁した種 A→{TN}')
            print('国 | ' + ' | '.join(rows))
            for n in MAP_NATIONS:
                cells = []
                for row in rows:
                    dT, oa, ot = paired(A, T, n, row)
                    ds = [paired(A, nl, n, row)[0] for nl in nulls]
                    floor = max([abs(x) for x in ds if x is not None] or [0])
                    cells.append(f'{dT:+g}(床{floor:g};{oa}→{ot})' if dT is not None else '-')
                print(f'{n} | ' + ' | '.join(cells))
        print(f'## 遅れる向きで 5 年以上かつ床の外の国×行（{TN}・MAP_ROWS+DOWN_ROWS）')
        cnt = 0
        for n in MAP_NATIONS:
            for row in MAP_ROWS + DOWN_ROWS:
                dT, oa, ot = paired(A, T, n, row)
                ds = [paired(A, nl, n, row)[0] for nl in nulls]
                floor = max([abs(x) for x in ds if x is not None] or [0])
                if dT is not None and dT >= 5 and dT > floor:
                    cnt += 1
        print(f'  {cnt} 件')


if __name__ == '__main__':
    main()
