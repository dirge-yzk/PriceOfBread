"""(316) 線 G 読み手＝読むだけ。標準の計器 site_s／metal_s（SimCore/Instruments.cs）を読む。事前登録 measurements/g316_20261014_prereg.txt の [1][3][6]。
  python an_g316.py years <ins> [<ins>…]      … 産地の開いた年（O 行）＝産地ごとの 10 種の年・中央〔開いた種の数〕・史実との差・L1／L1′／L2 の量。開かない種は 1950。ins を 2 つ渡すと A B の順で並べて判定する
  python an_g316.py cf <ins> [E]              … 反実仮想（A の軌道から）＝候補の式ごとに、産地が開く年を T 行〔まだ開かない産地の軌道〕と O 行から出す（一次の近似＝最初に開く産地だけが当たる）
  python an_g316.py metal <ins> [<ins>…]      … 世界の金銀の年表（10 年ごとの 10 種の中央）・金本位の国の数・主要 6 国の金本位の最初の年
限界の自己申告：①cf は T 行（四半期の末に読む）で判定する＝本体の判定（次の四半期の頭に平滑化した値段を進めてから読む）と四半期1つ分ずれうる（検算＝式 old の反実仮想が A の実際の年と一致するかを同じ出力に出す）。
②T 行の技術項は 70×ratio×厚み（ratio=技術水準÷難易度を1で切る）。必要性だけを式で置き換える。③最初の産地が世界を動かして他の産地の軌道を変えた後は当てにならない。"""
import sys, os, glob, re, math, statistics as st, collections

HIST = collections.OrderedDict([('カリフォルニアの金', 1848), ('ヴィクトリアの金', 1851), ('ラントの金', 1886), ('クロンダイクの金', 1896),
                                ('ルイジアナの硫黄', 1894), ('九州の硫黄', 1885)])
GOLD = ['カリフォルニアの金', 'ヴィクトリアの金', 'ラントの金', 'クロンダイクの金']
LAST = 1950


def seeds_of(D, kind='site'):
    return sorted(int(re.search(kind + r'_s(\d+)\.tsv', p).group(1)) for p in glob.glob(os.path.join(D, kind + '_s*.tsv')))


def rows_of(D, sd, kind='site'):
    with open(os.path.join(D, f'{kind}_s{sd}.tsv'), encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t')
        for ln in fh:
            f = ln.rstrip('\n').split('\t')
            yield {k: v for k, v in zip(h, f)}


def fl(x):
    try: return float(x)
    except ValueError: return float('nan')


def opens(D):
    """site -> seed -> (year, nation)"""
    out = collections.defaultdict(dict)
    for sd in seeds_of(D):
        for r in rows_of(D, sd):
            if r['kind'] == 'O': out[r['site']].setdefault(sd, (int(r['year']), r['nation']))
    return out


def med_year(ys):
    return st.median(ys)


def years(Ds):
    res = []
    for D in Ds:
        seeds = seeds_of(D); op = opens(D)
        tab = collections.OrderedDict()
        for site, h in HIST.items():
            ys = [op[site][sd][0] if sd in op[site] else LAST for sd in seeds]
            tab[site] = (ys, sum(1 for sd in seeds if sd in op[site]))
        res.append((D, seeds, op, tab))
        print(f'######## {D}（種 {len(seeds)}・産地の開いた年＝種の中央〔開いた種の数〕・開かない種は {LAST}）')
        dg = []
        for site, (ys, n) in tab.items():
            m = med_year(ys); h = HIST[site]
            who = collections.Counter(op[site][sd][1] for sd in seeds if sd in op[site]).most_common(2)
            print(f'  {site}: {m:.0f}〔{n}/{len(seeds)}〕 史実 {h}（{m - h:+.0f}）  種ごと {sorted(ys)}  {who}')
            if site in GOLD: dg.append(abs(m - h))
        print(f'  金 4 行の |中央−史実| の平均 {sum(dg) / len(dg):.1f} 年')
        win = sum(1 for s in GOLD for y in tab[s][0] if abs(y - HIST[s]) <= 20)
        early_rows = sum(1 for s in HIST if med_year(tab[s][0]) <= HIST[s] - 20)
        early_pairs = sum(1 for s in HIST for y in tab[s][0] if y <= HIST[s] - 20)
        print(f'  L1′ 金 4 行×種で史実の ±20 年に入る数 {win}／{4 * len(seeds)}')
        print(f'  L2(a) 鉱床 6 行で 10 種の中央が史実より 20 年以上早い行の数 {early_rows}・L2(b) (産地×種) で 20 年以上早い数 {early_pairs}／{6 * len(seeds)}')
    if len(res) == 2:
        (_, _, _, ta), (_, _, _, tb) = res
        def D(t, s): return abs(med_year(t[s][0]) - HIST[s])
        mA = sum(D(ta, s) for s in GOLD) / 4; mB = sum(D(tb, s) for s in GOLD) / 4
        worse = [s for s in GOLD if D(tb, s) > D(ta, s)]
        l1 = mB < mA and len(worse) <= 1
        wa = sum(1 for s in GOLD for y in ta[s][0] if abs(y - HIST[s]) <= 20); wb = sum(1 for s in GOLD for y in tb[s][0] if abs(y - HIST[s]) <= 20)
        ea = sum(1 for s in HIST if med_year(ta[s][0]) <= HIST[s] - 20); eb = sum(1 for s in HIST if med_year(tb[s][0]) <= HIST[s] - 20)
        pa = sum(1 for s in HIST for y in ta[s][0] if y <= HIST[s] - 20); pb = sum(1 for s in HIST for y in tb[s][0] if y <= HIST[s] - 20)
        print('######## 判定（A→B）')
        print(f'  L1 金 4 行の |差| の平均 A {mA:.1f}→B {mB:.1f}・悪化する行 {worse}・{"合格" if l1 else "外れ"}')
        print(f'  L1′ ±20 年に入る数 A {wa}→B {wb}・{"合格" if wb >= wa else "外れ"}')
        print(f'  L2(a) 中央が 20 年以上早い行 A {ea}→B {eb}・L2(b) 種ごと A {pa}→B {pb}・{"合格" if eb <= ea and pb <= pa else "外れ"}')


def load_traj(D):
    """site -> seed -> [(quarter, year, nation, ratio, techTerm, v, c, kind)] を時間順に（T 行と O 行）"""
    out = collections.defaultdict(lambda: collections.defaultdict(list))
    for sd in seeds_of(D):
        for r in rows_of(D, sd):
            if r['kind'] not in ('T', 'O'): continue
            tech, diff = fl(r['techLevel']), fl(r['difficulty'])
            ratio = 1.0 if not diff > 0 else min(1.0, max(0.0, tech / diff))
            out[r['site']][sd].append((int(r['quarter']), int(r['year']), r['nation'], ratio, fl(r['techTerm']), fl(r['price']), fl(r['cost']), r['kind'], r['siteKind']))
    return out


def sat(s, h): return 70.0 * s / (s + h) if s > 0 and h > 0 else 0.0


def make_forms(E):
    lnE = math.log(E)
    def old(v, c):
        x = v / c if v > 0 and c > 0 else 0.0
        return sat(math.log(x), 0.04) if x > 1 else 0.0
    def s_after(v, c):
        x = E * v / c if v > 0 and c > 0 else 0.0
        return math.log(x) if x > 1 else 0.0
    return collections.OrderedDict([
        ('old(r314)', old),
        ('S0(sat 0.04 on ln(E v/c))', lambda v, c: sat(s_after(v, c), 0.04)),
        ('hyp(half=lnE)', lambda v, c: sat(s_after(v, c), lnE)),
        ('logramp(cap=2lnE)', lambda v, c: 70.0 * min(1.0, s_after(v, c) / (2 * lnE))),
        ('linramp(x:1/E→0,1→70)', lambda v, c: 70.0 * min(1.0, max(0.0, (E * v / c - 1) / (E - 1))) if v > 0 and c > 0 else 0.0),
    ])


def cf(D, E):
    traj = load_traj(D); op = opens(D); seeds = seeds_of(D)
    forms = make_forms(E)
    print(f'######## 反実仮想（A の軌道・{D}・種 {len(seeds)}・E={E:g}）＝式ごとの産地の開く年（最初に 技術項＋必要性 ≧ 100 になる T／O 行の年・開かなければ {LAST}）')
    for name, fn in forms.items():
        print(f'--- 式 {name}')
        for site in HIST:
            ys = []
            for sd in seeds:
                y = LAST
                for (q, yr, nat, ratio, tt, v, c, kind, sk) in traj[site][sd]:
                    if tt + fn(v, c) >= 100.0: y = yr; break
                ys.append(y)
            m = med_year(ys); h = HIST[site]
            real = sorted(op[site][sd][0] if sd in op[site] else LAST for sd in seeds)
            extra = f'  （A の実際 {real}）' if name.startswith('old') else ''
            print(f'  {site}: {m:.0f}（{m - h:+.0f}）  種ごと {sorted(ys)}{extra}')
    # 軌道の要約（金・硫黄）＝10 年ごとの 年末の ratio／人／v÷c／技術項／国
    print('######## 軌道（種ごと・10 年ごと・年末の四半期）＝ratio / 人 / v÷c / 技術項 / 国')
    for site in HIST:
        print(f'  {site}')
        for sd in seeds:
            out = []
            for (q, yr, nat, ratio, tt, v, c, kind, sk) in traj[site][sd]:
                if q % 4 == 3 and yr % 10 == 9:
                    out.append(f'{yr}:{ratio:.2f}/{tt:.0f}/{(v / c if c > 0 else float("nan")):.2f}/{nat[:2]}')
            print(f'    種{sd}: ' + '  '.join(out))


def metal(Ds):
    for D in Ds:
        seeds = seeds_of(D, 'metal')
        print(f'######## {D}（種 {len(seeds)}・世界の金銀・10 年ごと〔その年の 4 四半期の平均〕の 10 種の中央）')
        by = collections.defaultdict(lambda: collections.defaultdict(list))   # decade-year -> metric -> [per seed]
        first = collections.defaultdict(dict)   # nation -> seed -> year (金本位に最初になった年)
        for sd in seeds:
            acc = collections.defaultdict(lambda: collections.defaultdict(list))
            for r in rows_of(D, sd, 'metal'):
                y = int(r['year'])
                for k in ('goldProduced', 'silverProduced', 'lnAgAuMean', 'nGoldLaw'): acc[y][k].append(fl(r[k]))
                for nat in r['goldNations'].split(';'):
                    if nat and sd not in first[nat]: first[nat][sd] = y
            for y in sorted(acc):
                if y % 10 == 0:
                    for k, xs in acc[y].items():
                        xs = [x for x in xs if x == x]
                        if xs: by[y][k].append(sum(xs) / len(xs))
        print('  年    金の産出  銀の産出   ln(銀÷金÷参照)  金本位の国の数')
        for y in sorted(by):
            print(f'  {y}  {st.median(by[y]["goldProduced"]):9.3g} {st.median(by[y]["silverProduced"]):9.3g}   {st.median(by[y]["lnAgAuMean"]):8.3f}   {st.median(by[y]["nGoldLaw"]):5.1f}')
        for nat in ['イギリス', 'アメリカ', 'ドイツ', 'プロイセン', 'フランス', '日本', 'ロシア']:
            if nat in first:
                ys = sorted(first[nat].values()); print(f'  金本位に最初になった年 {nat}: 中央 {st.median(ys):.0f}〔{len(ys)}/{len(seeds)}〕 {ys}')


if __name__ == '__main__':
    m = sys.argv[1]
    if m == 'years': years(sys.argv[2:])
    elif m == 'cf': cf(sys.argv[2], float(sys.argv[3]) if len(sys.argv) > 3 else 5.0)
    elif m == 'metal': metal(sys.argv[2:])
