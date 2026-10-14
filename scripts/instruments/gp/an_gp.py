# 流れG ① (328)：標準の計器の disc_s<seed>.tsv を、基線 A・腕 B・ヌル腕 N1・N2 で並べて読む（読むだけ・事前登録 measurements/gp_20261014_prereg.txt の線）。
# 使い方：PYTHONUTF8=1 python an_gp.py <A の ins> <B の ins> [<N1 の ins> <N2 の ins>]
# 列は main の disc_s（kind quarter year nation row techTerm needTerm transmitTerm replaced cost techLevel）。⚠ 枝 dg の列 ownOpen は main に無い（読まない）。
# 出す物：窓の表（1783-84・〜1788・〜1792）・L1（工業国 6 の蒸気機関の最初の年の種対の差と雑音の床）・L2（薄い国の 1783〜92 の解禁数）・L3（主要国が 1950 年までに消えない）・
#         記録（伝わり主導の別・動力の値段 v と費用の軌道・蒸気機関以外の行の最初の年の種対の差・日本と清）。
import sys, glob, os, re, statistics, collections

INDUSTRIAL = ["フランス", "プロイセン", "オーストリア", "ロシア", "アメリカ", "ネーデルラント"]
RECORD = ["スペイン", "オスマン", "日本", "清"]
L3_NATIONS = INDUSTRIAL + ["スペイン", "オスマン"]
CENSOR = 1950


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
    def __init__(self, d):
        self.d = d
        self.disc = load(d)
        self.seeds = sorted(self.disc)
        self.E = {}
        self.T = collections.defaultdict(list)
        for sd in self.seeds:
            for r in self.disc[sd]:
                if r['kind'] == 'E':
                    self.E.setdefault((sd, r['nation'], r['row']), r)
                else:
                    self.T[(sd, r['nation'], r['row'])].append(r)

    def year(self, sd, n, row):
        r = self.E.get((sd, n, row))
        return int(r['year']) if r else None

    def year_c(self, sd, n, row):
        y = self.year(sd, n, row)
        return CENSOR if y is None else y   # 解禁しなかった種は 1950 として数える（打ち切り）

    def steam_set(self, sd, yr):
        return {n for (s, n, row), r in self.E.items() if s == sd and row == '蒸気機関' and int(r['year']) <= yr and n != 'イギリス'}


def window_table(name, a):
    print(f'## {name}（{len(a.seeds)}種）')
    print('種 | 蒸気機関 〜1784（英以外） | 〜1788 | 〜1792 | うち 〜1792 で伝わり主導（transmitTerm>0） | 1783 の全行 | 清の最初の年')
    for sd in a.seeds:
        s92 = a.steam_set(sd, 1792)
        tr = sum(1 for n in s92 if fl(a.E[(sd, n, '蒸気機関')]['transmitTerm']) > 0)
        a83 = sum(1 for (s, n, row), r in a.E.items() if s == sd and int(r['year']) == 1783)
        print(f"{sd} | {len(a.steam_set(sd, 1784))} | {len(a.steam_set(sd, 1788))} | {len(s92)} | {tr} | {a83} | {a.year(sd, '清', '蒸気機関')}")


def paired(a, b, nation, row):
    """国×行の 〈同じ種の b の最初の年 − a の最初の年〉（打ち切り 1950）の種の中央値と、解禁した種の数 (a, b)。"""
    seeds = [s for s in a.seeds if s in b.seeds]
    d = [b.year_c(s, nation, row) - a.year_c(s, nation, row) for s in seeds]
    return med(d), sum(1 for s in seeds if a.year(s, nation, row) is not None), sum(1 for s in seeds if b.year(s, nation, row) is not None)


def lines(A, B, Ns):
    print('\n## 工業国 6 の蒸気機関：最初の年の種対の差（腕−A・中央値・年・打ち切り 1950）と 〜1950 に解禁した種の数')
    print('国 | A の中央値 | B−A | ' + ' | '.join(f'N{i+1}−A' for i in range(len(Ns))) + ' | 解禁した種 A→B | 雑音の床=max|N−A|')
    n_ok = 0
    for n in INDUSTRIAL + RECORD:
        dB, ca, cb = paired(A, B, n, '蒸気機関')
        dN = [paired(A, N, n, '蒸気機関')[0] for N in Ns]
        floor = max([abs(x) for x in dN if x is not None], default=0.0)
        ma = med([A.year(s, n, '蒸気機関') for s in A.seeds])
        mark = ''
        if n in INDUSTRIAL and dB is not None and dB <= -5 and abs(dB) > floor:
            n_ok += 1; mark = ' ←早まり（線の内）'
        print(f"{n} | {ma} | {dB} | " + ' | '.join(str(x) for x in dN) + f" | {ca}→{cb} | {floor}{mark}")
    print(f'L1（工業国 6 のうち、B−A ≦ −5 年かつ雑音の床の外の国）{n_ok}/6  合格の線＝4 以上')

    print('\n## L2 薄い国の 1783〜92 の解禁数（英以外・種対）')
    worse = 0; diffs = []
    for s in A.seeds:
        if s not in B.seeds: continue
        a, b = len(A.steam_set(s, 1792)), len(B.steam_set(s, 1792)); diffs.append(b - a)
        if b > a: worse += 1
        print(f'  種{s}: A {a} → B {b}（{b-a:+d}）' + ''.join(f' N{i+1} {len(N.steam_set(s,1792))}' for i, N in enumerate(Ns) if s in N.seeds))
    nd = [[len(N.steam_set(s, 1792)) - len(A.steam_set(s, 1792)) for s in A.seeds if s in N.seeds] for N in Ns]
    print(f'L2（B が A より多い種）{worse}/{len(diffs)}・差の中央値 {med(diffs)}・ヌル腕の差の中央値 {[med(x) for x in nd]}  合格の線＝B が多い種 ≦ 3/10 かつ差の中央値 ≦ 0')

    print('\n## L3 主要国の蒸気機関が 1950 年までに消えない（A→B の解禁した種の数・最初の年の中央値）')
    bad = 0
    for n in L3_NATIONS:
        ca = sum(1 for s in A.seeds if A.year(s, n, '蒸気機関') is not None)
        cb = sum(1 for s in B.seeds if B.year(s, n, '蒸気機関') is not None)
        ma = med([A.year(s, n, '蒸気機関') for s in A.seeds]); mb = med([B.year(s, n, '蒸気機関') for s in B.seeds])
        flag = ''
        if cb < ca - 2: flag += ' 〜1950 の種が −2 を超えて減った'
        if ma is not None and mb is not None and abs(mb - ma) > 15: flag += ' 中央値が ±15 年の外'
        if flag: bad += 1
        print(f'  {n}: {ca}→{cb}・{ma}→{mb}{flag}')
    print(f'L3（外れた国）{bad}  合格の線＝0')


def records(A, B):
    print('\n## 記録：動力の値段 v（T 行の replaced）と蒸気機関の費用（cost）の軌道（未解禁の四半期の国×種の中央値・B÷A）')
    for q in (1, 2, 4, 8, 12, 20, 40):
        va = []; vb = []; ca = []; cb = []
        for lab, arm, vv, cc in (('A', A, va, ca), ('B', B, vb, cb)):
            for (s, n, row), rs in arm.T.items():
                if row != '蒸気機関': continue
                for r in rs:
                    if int(r['quarter']) == q:
                        vv.append(fl(r['replaced'])); cc.append(fl(r['cost']))
        print(f'  Q{q}: v A {med(va)} B {med(vb)}（B/A {None if not med(va) else round(med(vb)/med(va),3)}）・cost A {med(ca)} B {med(cb)}（B/A {None if not med(ca) else round(med(cb)/med(ca),3)}）・replaced÷cost A {None if not med(ca) else round(med(va)/med(ca),3)} B {None if not med(cb) else round(med(vb)/med(cb),3)}（未解禁の国のみ・Q が進むほど解禁済みが抜けて偏る）')
    print('\n## 記録：蒸気機関以外の行の最初の年の種対の差（B−A・国をまたいだ行ごとの中央値・早まった/遅れた/同じ（国×種の数））')
    rows = sorted({row for (s, n, row) in A.E} | {row for (s, n, row) in B.E})
    for row in rows:
        if row == '蒸気機関': continue
        d = []
        for (s, n, rw), r in A.E.items():
            if rw != row or s not in B.seeds: continue
            d.append(B.year_c(s, n, row) - int(r['year']))
        for (s, n, rw), r in B.E.items():
            if rw != row or s not in A.seeds or (s, n, row) in A.E: continue
            d.append(int(r['year']) - CENSOR)
        if not d: continue
        print(f'  {row}: 中央値 {med(d)}・早 {sum(1 for x in d if x < 0)}・遅 {sum(1 for x in d if x > 0)}・同 {sum(1 for x in d if x == 0)}')


def load_contact(d):
    out = {}
    for f in sorted(glob.glob(os.path.join(d, 'disccontact_s*.tsv'))):
        sd = int(re.search(r'disccontact_s(\d+)\.tsv', f).group(1))
        rows = []
        with open(f, encoding='utf-8') as fh:
            hdr = fh.readline().rstrip(chr(10)).split(chr(9))
            for l in fh:
                p = l.rstrip(chr(10)).split(chr(9))
                if len(p) == len(hdr):
                    rows.append(dict(zip(hdr, p)))
        out[sd] = rows
    return out


def contacts(name, d):
    """接触の内訳（disccontact_s・事前登録 [8]）：主要国が蒸気機関の運び手の財を、どの国から買って接触を得たか（種をまたいだ額の和の上位 5 国と全体に占める割合）。"""
    data = load_contact(d)
    if not data:
        print(f'{chr(10)}## 接触の内訳 {name}：disccontact_s が無い'); return
    print(f'{chr(10)}## 接触の内訳 {name}（蒸気機関の行・買い手別・種をまたいだ額の和の上位 5 売り手と割合）')
    print('  ⚠売り手の鍵は表示名（nationUid でない）＝内戦の後継・建国で同じ国の割合が割れる（例：立憲王政イギリスとイギリス）。額は種をまたいだ和で、分母で正規化していない')
    for n in INDUSTRIAL + RECORD:
        tot = collections.Counter()
        for sd, rows in data.items():
            for r in rows:
                if r['buyer'] == n and r['row'] == '蒸気機関':
                    tot[r['seller']] += fl(r['valuePerQuarter'])
        s = sum(tot.values())
        top = ' / '.join(f'{k} {v / s:.0%}' for k, v in tot.most_common(5)) if s > 0 else '（記帳なし）'
        print(f'  {n}: 合計 {s:.4g} ＝ {top}')


if __name__ == '__main__':
    ds = sys.argv[1:]
    arms = [Arm(d) for d in ds]
    for d, a in zip(ds, arms): window_table(d, a)
    if len(arms) >= 2:
        lines(arms[0], arms[1], arms[2:])
        records(arms[0], arms[1])
    for d in ds: contacts(d, d)
