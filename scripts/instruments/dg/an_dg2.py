# 流れG (320) 後半：disc_s（ownOpen つき）と site_s を、基線 A・腕 B・ヌル腕 N で並べて読む（読むだけ・事前登録 [3][3b][8][10] の線）。
# 使い方：PYTHONUTF8=1 python an_dg2.py <A の ins> <B の ins> [<N の ins>]
# 出す物：L1/L1b/L2（伝わり・自力の別つき）・L3（種対の差・打ち切り 1950・日本と清は数から外す）・L4（[8] の読み替え）・L6（蒸気機関とそれ以外・ownOpen の別）・
#         蒸気機関を除く行の種対の差（ownOpen 別・行別）・甜菜糖の国ごとの最初の四半期・薄い国の replaced÷cost の軌道・建国した国の最初の 8 四半期・site_s の 1783〜95 年の O 行。
import sys, glob, os, re, statistics, collections

MAJORS = ["フランス", "プロイセン", "オーストリア", "ロシア", "アメリカ", "スペイン", "オスマン", "ネーデルラント", "日本", "清"]
CENSOR = 1950


def load(d, stem):
    out = {}
    for f in sorted(glob.glob(os.path.join(d, stem + '_s*.tsv'))):
        sd = int(re.search(stem + r'_s(\d+)\.tsv', f).group(1))
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
    xs = [x for x in xs if x is not None]
    return statistics.median(xs) if xs else None


class Arm:
    def __init__(self, d):
        self.d = d
        self.disc = load(d, 'disc')
        self.site = load(d, 'site')
        self.seeds = sorted(self.disc)
        # (seed, nation, row) -> E 行（最初の1つ）
        self.E = {}
        self.T = collections.defaultdict(list)
        self.first_q = {}   # (seed, nation) -> 最初に現れた四半期
        for sd in self.seeds:
            for r in self.disc[sd]:
                q = int(r['quarter'])
                k = (sd, r['nation'])
                if k not in self.first_q or q < self.first_q[k]:
                    self.first_q[k] = q
                if r['kind'] == 'E':
                    kk = (sd, r['nation'], r['row'])
                    if kk not in self.E:
                        self.E[kk] = r
                else:
                    self.T[(sd, r['nation'], r['row'])].append(r)

    def year(self, sd, nation, row):
        r = self.E.get((sd, nation, row))
        return int(r['year']) if r else None

    def steam_nations(self, sd, yr):
        return {n for (s, n, row), r in self.E.items() if s == sd and row == '蒸気機関' and int(r['year']) <= yr and n != 'イギリス'}


def table_head(name, a):
    print(f'## {name}（{len(a.seeds)}種）')
    print('種 | 蒸気機関 1783-84（英以外） | 〜1788 | 〜1792 | うち 1783-92 で伝わり主導（transmitTerm>0） | 1783 の全行 | 清の蒸気機関の最初の年')
    for sd in a.seeds:
        n84 = len(a.steam_nations(sd, 1784)); n88 = len(a.steam_nations(sd, 1788)); n92 = a.steam_nations(sd, 1792)
        tr = sum(1 for n in n92 if fl(a.E[(sd, n, '蒸気機関')]['transmitTerm']) > 0)
        a83 = sum(1 for (s, n, row), r in a.E.items() if s == sd and int(r['year']) == 1783)
        print(f"{sd} | {n84} | {n88} | {len(n92)} | {tr} | {a83} | {a.year(sd, '清', '蒸気機関')}")


def l_lines(base, arm, label):
    seeds = [s for s in base.seeds if s in arm.seeds]
    n84 = lambda a, s: len(a.steam_nations(s, 1784))
    n92 = lambda a, s: len(a.steam_nations(s, 1792))
    l1 = sum(1 for s in seeds if n84(arm, s) * 3 <= n84(base, s))
    l1b = sum(1 for s in seeds if n92(arm, s) * 2 <= n92(base, s))
    l2 = sum(1 for s in seeds if (arm.year(s, '清', '蒸気機関') or 9999) <= 1784)
    print(f'\n### {label}：L1（〜1784 が基線の 1/3 以下）{l1}/{len(seeds)}・L1b（〜1792 が基線の 1/2 以下）{l1b}/{len(seeds)}・L2（清が 1783-84）{l2}/{len(seeds)}')
    # L3：種対の差
    early = 0
    print('L3（種対の差の中央値・打ち切り 1950・清と日本は数から外す）')
    flagged = []
    for nm in MAJORS:
        for row in ('蒸気機関', '機関車'):
            ds = []
            for s in seeds:
                yb = base.year(s, nm, row) or CENSOR; ya = arm.year(s, nm, row) or CENSOR
                ds.append(ya - yb)
            d = statistics.median(ds)
            cnt = sum(1 for x in ds if x < 0)
            mark = ''
            if d <= -5 and nm not in ('清', '日本'):
                early += 1; flagged.append((nm, row)); mark = ' ←早まり'
            print(f'  {nm} {row}: 種対の差の中央値 {d:+.1f} 年・早まった種 {cnt}・遅れた種 {sum(1 for x in ds if x > 0)}{mark}')
    print(f'L3（5 年以上早まった国×行・清と日本を除く）{early}')
    for nm, row in flagged:
        tr = [fl(arm.E[(s, nm, row)]['transmitTerm']) > 0 for s in seeds if (s, nm, row) in arm.E]
        print(f'  L3 早まり {nm} {row}: 腕の E 行のうち伝わり主導 {sum(tr)}/{len(tr)}')
    # L4
    bad4 = 0
    for nm in MAJORS:
        if nm in ('清', '日本'):
            continue
        cb = sum(1 for s in seeds if base.year(s, nm, '蒸気機関')); ca = sum(1 for s in seeds if arm.year(s, nm, '蒸気機関'))
        mb = med([base.year(s, nm, '蒸気機関') for s in seeds]); ma = med([arm.year(s, nm, '蒸気機関') for s in seeds])
        flag = ''
        if ca < cb - 2: flag += ' 〜1950 の種が基線から −2 を超えて減った'
        if mb is not None and ma is not None and abs(ma - mb) > 15: flag += ' 中央値が基線の ±15 年の外'
        if ca == 0: flag += ' 〜1950 で蒸気なし（NP の印）'
        if flag: bad4 += 1
        print(f'  L4 {nm}: 〜1950 に解禁した種 {cb}→{ca}・最初の年の中央値 {mb}→{ma}{flag}')
    print(f'L4（外れた国）{bad4}')
    # L6
    print('L6（1783〜92 年の出来事の数・種ごと）基線 → 腕：蒸気機関 | 蒸気機関以外 ownOpen=1 / 0 / -1')
    ok = 0
    for s in seeds:
        def cnt(a):
            c = collections.Counter()
            for (ss, n, row), r in a.E.items():
                if ss != s or int(r['year']) > 1792: continue
                c['steam' if row == '蒸気機関' else 'o' + r.get('ownOpen', 'na')] += 1
            return c
        cb, ca = cnt(base), cnt(arm)
        tb = sum(cb.values()); ta = sum(ca.values())
        if ta <= tb: ok += 1
        print(f"  種{s}: 全体 {tb}→{ta}（基線 steam {cb['steam']}・o1 {cb['o1']}・o0 {cb['o0']}・o-1 {cb['o-1']} → 腕 steam {ca['steam']}・o1 {ca['o1']}・o0 {ca['o0']}・o-1 {ca['o-1']}）")
    print(f'L6（全体が基線を超えない種）{ok}/{len(seeds)}')
    # 蒸気機関を除く行の種対の差
    grp = collections.defaultdict(lambda: [0, 0, 0, 0, 0, []])   # ownOpen -> 早まり・遅れ・同じ・基線だけ・腕だけ・差のリスト
    byrow = collections.defaultdict(lambda: [0, 0, 0, 0, []])
    keys = {k for k in base.E if k[0] in seeds and k[2] != '蒸気機関'} | {k for k in arm.E if k[0] in seeds and k[2] != '蒸気機関'}
    for k in keys:
        rb, ra = base.E.get(k), arm.E.get(k)
        own = (rb or ra).get('ownOpen', 'na')
        g = grp[own]; br = byrow[k[2]]
        if rb and ra:
            d = int(ra['year']) - int(rb['year'])
            g[5].append(d); br[4].append(d)
            if d < 0: g[0] += 1; br[0] += 1
            elif d > 0: g[1] += 1; br[1] += 1
            else: g[2] += 1; br[2] += 1
        elif rb: g[3] += 1; br[2 + 1] += 1
        else: g[4] += 1; br[3] += 0
    print('蒸気機関を除く行（種×国×行の組）の最初の年の対：ownOpen 別')
    for own in sorted(grp):
        g = grp[own]
        print(f'  ownOpen={own}: 早まり {g[0]}・遅れ {g[1]}・同じ {g[2]}・基線だけ解禁 {g[3]}・腕だけ解禁 {g[4]}・差の中央値（両方あり）{statistics.median(g[5]) if g[5] else None}')
    top = sorted(byrow.items(), key=lambda kv: -(kv[1][0] + kv[1][1]))[:12]
    print('  行別（動いた組の多い順 12）：行 | 早まり | 遅れ | 基線だけ | 差の中央値')
    for nm, v in top:
        print(f'    {nm} | {v[0]} | {v[1]} | {v[3]} | {statistics.median(v[4]) if v[4] else None}')
    # 甜菜糖
    print('甜菜糖の最初の四半期（種の中央値・基線→腕・基線で 1792 年までに解禁した国のうち動きの大きい順 10）')
    rows = []
    nats = {k[1] for k in base.E if k[2] == '甜菜糖'}
    for n in nats:
        qb = [int(base.E[(s, n, '甜菜糖')]['quarter']) for s in seeds if (s, n, '甜菜糖') in base.E]
        qa = [int(arm.E[(s, n, '甜菜糖')]['quarter']) for s in seeds if (s, n, '甜菜糖') in arm.E]
        if qb and qa:
            rows.append((abs(statistics.median(qa) - statistics.median(qb)), n, statistics.median(qb), statistics.median(qa), len(qb), len(qa)))
    for _, n, b, a, nb, na in sorted(rows, reverse=True)[:10]:
        print(f'  {n}: Q{b}→Q{a}（解禁した種 {nb}→{na}）')
    # 薄い国の軌道
    thin = collections.Counter()
    for s in base.seeds:
        for n in base.steam_nations(s, 1784): thin[n] += 1
    thinset = {n for n, c in thin.items() if c >= 5}
    print(f'薄い国（基線で 1783-84 に蒸気機関を解禁した種が 5 以上の国）{len(thinset)} 国・T 行の replaced÷cost（まだ解禁していない国×種の中央値）')
    for q in (1, 2, 3, 4, 6, 8, 12, 16, 20, 24, 32, 40):
        xs = []
        for (s, n, row), rs in arm.T.items():
            if row != '蒸気機関' or n not in thinset or s not in seeds: continue
            for r in rs:
                if int(r['quarter']) == q and fl(r['cost']) > 0: xs.append(fl(r['replaced']) / fl(r['cost']))
        print(f'  Q{q}: n={len(xs)}・中央値 {statistics.median(xs):.3f}' if xs else f'  Q{q}: n=0')
    for nm in ('清',):
        for q in (1, 2, 4, 8, 12, 20, 40):
            xs = []
            for s in seeds:
                for r in arm.T.get((s, nm, '蒸気機関'), []):
                    if int(r['quarter']) == q and fl(r['cost']) > 0: xs.append(fl(r['replaced']) / fl(r['cost']))
            print(f'  {nm} Q{q}: n={len(xs)}・' + (f'中央値 {statistics.median(xs):.3f}' if xs else '-'))
    # 建国した国
    new = collections.defaultdict(list)
    for (s, n), q in arm.first_q.items():
        if q > 1 and s in seeds: new[(s, n)] = q
    e8 = sum(1 for (s, n, row), r in arm.E.items() if (s, n) in new and int(r['quarter']) - new[(s, n)] < 8)
    newb = {(s, n): q for (s, n), q in base.first_q.items() if q > 1 and s in seeds}
    e8b = sum(1 for (s, n, row), r in base.E.items() if (s, n) in newb and int(r['quarter']) - newb[(s, n)] < 8)
    print(f'建国した国（disc_s の最初の行が Q1 でない国×種）基線 {len(newb)} → 腕 {len(new)}・最初の 8 四半期の E 行 基線 {e8b} → 腕 {e8}')
    # site_s
    def siteo(a):
        return [sum(1 for r in a.site.get(s, []) if r['kind'] == 'O' and int(r['year']) <= 1795) for s in seeds]
    sb, sa = siteo(base), siteo(arm)
    print(f'site_s の 1783〜95 年の O 行（種ごと）基線 {sb} → 腕 {sa}（中央値 {statistics.median(sb)}→{statistics.median(sa)}）')


if __name__ == '__main__':
    ds = sys.argv[1:]
    arms = [Arm(d) for d in ds]
    for d, a in zip(ds, arms): table_head(d, a)
    for d, a in zip(ds[1:], arms[1:]):
        l_lines(arms[0], a, d)
