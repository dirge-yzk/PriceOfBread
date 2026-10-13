"""(300) 線 i1 段 1：合格の線 J1・J2 と記録の読み手（読むだけ）。
  python an_i1_j.py <出力のファイル> 腕名=<ins> [腕名=<ins> ...]
  国＝英・仏・蘭・葡。窓（q は 0 始まり）：Q5-40＝q4..39・Q41-160＝q40..159。種ごとに窓で足してから中央値（最小〜最大）。
  J1 民間の工場建設÷所得＝cons_s の paidPerMonth（州の和）の窓の月の和 ÷ nation_s の earnedTotalPerMonth の窓の和（どちらも月あたりの四半期の平均の和）。本土／植民地格に分ける。
  J2 資本家の貯蓄÷所得＝cons_s の capitalistSavingsStock（州の和・四半期の末）の窓の平均 ÷ 窓の年所得（earnedTotalPerMonth の窓の平均×12）。種ごとに出し、A より下がった種の数も出す。
  記録＝建った束（builtUnitsPerMonth の和）・計画した束・植民地格の工業化度（生産的工業化度の州の平均）・本土の工業化度。
  限界の自己申告：①所得は名目（自国通貨）②J2 は貯蓄が所得の数十年分と巨大で中央値の差は小さい③窓の月数は四半期×3で同じ。"""
import sys, os, statistics, collections
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..'))
from instr import num as fl, seeds  # noqa: E402

OUT = sys.argv[1]
ARMS = collections.OrderedDict(a.split('=', 1) for a in sys.argv[2:])
NATIONS = ['イギリス', 'フランス', 'ネーデルラント', 'ポルトガル']
WIN = {'Q5-40': (4, 39), 'Q41-160': (40, 159)}


def table(path):
    with open(path, encoding='utf-8') as fh:
        head = fh.readline().rstrip('\n').split('\t')
        for ln in fh:
            yield dict(zip(head, ln.rstrip('\n').split('\t')))


def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')


def cell(xs, pct=True, nd=2):
    ys = [x for x in xs if x == x]
    if not ys:
        return '—'
    k = 100.0 if pct else 1.0
    s = '%' if pct else ''
    return f'{med(ys)*k:.{nd}f}{s} ({min(ys)*k:.{nd}f}〜{max(ys)*k:.{nd}f})'


def load(D):
    res = {}  # (nation, win) -> dict of per-seed lists
    for sd in seeds(D, 'cons'):
        earn = collections.defaultdict(dict)
        for r in table(os.path.join(D, f'nation_s{sd}.tsv')):
            if r['nation'] in NATIONS:
                earn[r['nation']][int(r['quarter'])] = fl(r['earnedTotalPerMonth'])
        acc = collections.defaultdict(lambda: collections.defaultdict(float))
        sav = collections.defaultdict(lambda: collections.defaultdict(float))
        ind = collections.defaultdict(lambda: collections.defaultdict(list))
        for r in table(os.path.join(D, f'cons_s{sd}.tsv')):
            n = r['nation']
            if n not in NATIONS or r['activeFlag'] != '1':
                continue
            q = int(r['quarter']); k = 'c' if r['tier'] == 'Colonial' else 'h'
            for win, (a, b) in WIN.items():
                if a <= q <= b:
                    for col, key in (('paidPerMonth', 'paid'), ('builtUnitsPerMonth', 'built'), ('plannedUnitsPerMonth', 'plan'), ('desiredMoneyPerMonth', 'des')):
                        v = fl(r[col])
                        if v == v: acc[(n, win)][k + key] += v
                    ind[(n, win)][k].append(fl(r['productiveIndustrializationIndex']))
            sv = fl(r['capitalistSavingsStock'])
            if sv == sv:
                sav[n][q] += sv
        for n in NATIONS:
            for win, (a, b) in WIN.items():
                qs = [q for q in range(a, b + 1) if earn[n].get(q) == earn[n].get(q) and q in earn[n]]
                if not qs:
                    continue
                inc = sum(earn[n][q] for q in qs)
                d = res.setdefault((n, win), collections.defaultdict(list))
                for k in ('h', 'c'):
                    d[k + 'paid'].append(acc[(n, win)][k + 'paid'] / inc)
                    d[k + 'des'].append(acc[(n, win)][k + 'des'] / inc)
                    d[k + 'built'].append(acc[(n, win)][k + 'built'] / len(qs))
                    d[k + 'plan'].append(acc[(n, win)][k + 'plan'] / len(qs))
                    d[k + 'ind'].append(statistics.mean([x for x in ind[(n, win)][k] if x == x]) if ind[(n, win)][k] else float('nan'))
                d['paid'].append((acc[(n, win)]['hpaid'] + acc[(n, win)]['cpaid']) / inc)
                ys = [sav[n][q] for q in qs if q in sav[n]]
                yinc = statistics.mean([earn[n][q] for q in qs]) * 12
                d['sav'].append(statistics.mean(ys) / yinc if ys and yinc > 0 else float('nan'))
    return res


R = {arm: load(D) for arm, D in ARMS.items()}
md = ['# T-I1 合格の線 J1・J2 と記録（' + ' / '.join(f'{a}={d}' for a, d in ARMS.items()) + '）', '',
      '■ J1＝民間の工場建設（資本家の建設の払い）÷所得。J2＝資本家の貯蓄の在庫÷年所得（括弧は最小〜最大・種ごとに窓で集計してから中央値）。', '']
first = next(iter(ARMS))
for n in NATIONS:
    for win in WIN:
        md += [f'## {n} {win}', '', '| 腕 | J1 民間の工場建設÷所得（本土+植民地） | うち本土 | うち植民地格 | 希望÷所得(本土) | 希望÷所得(植民地格) | 建った束/月(本土) | 建った束/月(植民地格) | 本土の生産的工業化度 | 植民地格の生産的工業化度 | J2 資本家の貯蓄÷年所得 | A より J2 が下がった種 | A の3倍以上の種 |', '|---|---|---|---|---|---|---|---|---|---|---|---|---|']
        base = R[first].get((n, win))
        for arm in ARMS:
            d = R[arm].get((n, win))
            if not d:
                md.append(f'| {arm} | — |'); continue
            lower = sum(1 for x, y in zip(d['sav'], base['sav']) if x < y) if base else 0
            triple = sum(1 for x, y in zip(d['paid'], base['paid']) if y > 0 and x >= 3 * y) if base else 0
            md.append(f'| {arm} | {cell(d["paid"],nd=3)} | {cell(d["hpaid"],nd=3)} | {cell(d["cpaid"],nd=3)} | {cell(d["hdes"],nd=3)} | {cell(d["cdes"],nd=3)} | {cell(d["hbuilt"],False,2)} | {cell(d["cbuilt"],False,2)} | {cell(d["hind"],False,2)} | {cell(d["cind"],False,2)} | {cell(d["sav"],False,1)} | {lower}/{len(d["sav"])} | {triple}/{len(d["paid"])} |')
        md.append('')
txt = '\n'.join(md) + '\n'
open(OUT, 'w', encoding='utf-8').write(txt)
print(txt)
