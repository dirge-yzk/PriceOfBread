"""(349) 消費の入口 段D＋E の腕の比べ（読むだけ・事前登録 measurements/ce349_20261015_prereg.txt の追記 6 の線）。
  python an_ce_arms.py 名前=<計器の出力ディレクトリ(ins)> ...    例：A=…/out_ains_10/ins N1=…/out_n1_10/ins N2=… B=…
  読む物（10 種の種ごと→中央値と最小〜最大）：
    英の兌換停止の最初の四半期（fiscal_s の onGold が 1→0 になった最初の月÷3・起きない種は 669）
    英の名目GDP（nation_s の nominalGdpPerYear）の Q8／Q40／Q100
    英の歳入÷年GDP（fiscal_s の revenueBasePerMonth×12 ÷ nation_s の nominalGdpPerYear・窓 Q5–Q40 の月平均）
    英の資本家の人口（ce_s の C 行 classId 3 の popK・窓 Q13–Q40 の四半期の平均）＝L5
    補給崩壊（nation_s の supplyFillShare<0.3 の国×四半期）の数：全体・人口100以上・千件級の種の数
  限界：①fiscal_s の月の行は挙げた国だけ（英を挙げていること）②四半期＝月の添字÷3（0 始まり）③窓の Q は 1 始まり＝q は Q−1。"""
import sys, os, csv, glob, statistics, collections

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
UK = 'イギリス'


def rd(path):
    with open(path, encoding='utf-8', errors='replace', newline='') as f:
        return list(csv.DictReader(f, delimiter='\t'))


def fl(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')


def summ(xs):
    xs = [x for x in xs if x == x]
    if not xs:
        return 'n/a'
    return f'{statistics.median(xs):.4g} ({min(xs):.4g}〜{max(xs):.4g})'


def seedsOf(D):
    return sorted(int(os.path.basename(p)[len('nation_s'):-4]) for p in glob.glob(os.path.join(D, 'nation_s*.tsv')))


def one(D, s):
    out = {}
    nat = rd(os.path.join(D, f'nation_s{s}.tsv'))
    gdp = {}
    coll_all = coll_100 = 0
    for r in nat:
        q = int(r['quarter'])
        if r['nation'] == UK:
            gdp[q] = fl(r['nominalGdpPerYear'])
        fill, pop = fl(r['supplyFillShare']), fl(r['populationK'])
        if fill == fill and fill < 0.3:
            coll_all += 1
            if pop >= 100:
                coll_100 += 1
    out['collAll'], out['coll100'] = coll_all, coll_100
    for Q in (8, 40, 100):
        out[f'gdpQ{Q}'] = gdp.get(Q - 1, float('nan'))
    # 財政
    fis = [r for r in rd(os.path.join(D, f'fiscal_s{s}.tsv')) if r['nation'] == UK]
    stop = 669
    prev = None
    rev = []
    for r in fis:
        m = int(r['monthIndex']); q = m // 3
        og = fl(r["onGold"])
        if prev == 1.0 and og == 0.0 and stop == 669:
            stop = q + 1
        prev = og
        if 4 <= q <= 39 and q in gdp and gdp[q] > 0:
            rev.append(fl(r['revenueBasePerMonth']) * 12 / gdp[q])
    out['convStopQ'] = stop
    out['revToGdp'] = sum(rev) / len(rev) if rev else float('nan')
    # 資本家の人口
    pops = collections.defaultdict(list)
    for r in rd(os.path.join(D, f'ce_s{s}.tsv')):
        if r['scope'] == 'C' and r['nation'] == UK and r['classId'] == '3':
            q = int(r['quarter'])
            if 12 <= q <= 39:
                pops[q].append(fl(r['popK']))
    allp = [sum(v) / len(v) for v in pops.values() if v]
    out['capPopK'] = sum(allp) / len(allp) if allp else float('nan')
    return out


def main():
    arms = [a.split('=', 1) for a in sys.argv[1:]]
    res = {}
    for name, D in arms:
        ss = seedsOf(D)
        res[name] = [one(D, s) for s in ss]
        print(f'## {name}  seeds={len(ss)}  {D}')
        rows = res[name]
        for key, lab in [('convStopQ', '英の兌換停止の最初のQ(起きない=669)'), ('revToGdp', '英の歳入÷年GDP(Q5-Q40)'), ('gdpQ8', '英の名目GDP Q8'), ('gdpQ40', '英の名目GDP Q40'), ('gdpQ100', '英の名目GDP Q100'),
                         ('capPopK', '英の資本家の人口K(Q13-Q40)'), ('collAll', '補給崩壊 国×四半期(全体)'), ('coll100', '補給崩壊 国×四半期(人口100以上)')]:
            xs = [r[key] for r in rows]
            print(f'  {lab}: {summ(xs)}   各種: ' + ' '.join(f'{x:.4g}' for x in xs))
        print(f'  千件級(人口100以上)の種の数: {sum(1 for r in rows if r["coll100"] >= 1000)}/{len(rows)}')
    nulls = [n for n in res if n.startswith('N')]
    if nulls and 'A' in res:
        print('## ヌル腕の中央（N1・N2 の中央値の平均）と幅（最大−最小）')
        for key in ('convStopQ', 'revToGdp', 'gdpQ8', 'gdpQ40', 'gdpQ100', 'capPopK', 'coll100'):
            ms = [med([r[key] for r in res[n]]) for n in nulls]
            print(f'  {key}: 中央 {sum(ms)/len(ms):.4g}  幅 {max(ms)-min(ms):.4g}  ({" / ".join(f"{m:.4g}" for m in ms)})')


main()
