# (657) 線 igi3：他の 4 本（信用・疫病・扶養力・交易）の〈動いたか〉の記録（採否に使わない）。読むだけ。
# 使い方：python obs4.py <名前=ins フォルダ> ...（例：base=…/m655_ins n1=… n2=… U5=…） 種 1..10 の中央値［最小,最大］を並べる。
#  疫病＝epi_s の deadK の年合計（全ての国・種ごと）／人口の倍率＝nation_s の populationK の 1913 年末÷1783 年（9 国の中央値の中央値と各国）／
#  信用＝money_s の outstandingLoansStock÷(12×gdpRealPerMonth) の国ごとの最大（9 国の中央値）。
import sys, csv, statistics, collections
NATS = ['清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本', 'アメリカ']
def med(v): v = [x for x in v if x == x]; return statistics.median(v) if v else float('nan')
def rg(v): v = [x for x in v if x == x]; return (min(v), max(v)) if v else (float('nan'),) * 2
def epi(d, s):
    t = 0.0
    try:
        with open(f"{d}/epi_s{s}.tsv", encoding='utf-8') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); i = h.index('deadK')
            for x in r: t += float(x[i])
    except FileNotFoundError: return float('nan')
    return t
def popmult(d, s):
    first = {}; last = {}
    with open(f"{d}/nation_s{s}.tsv", encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); iy, inn, ip = h.index('year'), h.index('nation'), h.index('populationK')
        for x in r:
            if x[inn] not in NATS: continue
            y = int(x[iy]); p = float(x[ip])
            if y == 1783 and x[inn] not in first: first[x[inn]] = p
            if y <= 1913: last[x[inn]] = p
    return {n: last[n] / first[n] for n in first if n in last and first[n] > 0}
def credit(d, s):
    mx = collections.defaultdict(float)
    with open(f"{d}/money_s{s}.tsv", encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); inn, il, ig = h.index('nation'), h.index('outstandingLoansStock'), h.index('gdpRealPerMonth')
        for x in r:
            if x[inn] not in NATS: continue
            try: g = float(x[ig]); l = float(x[il])
            except ValueError: continue
            if g > 0: mx[x[inn]] = max(mx[x[inn]], l / (12 * g))
    return mx
args = [a.split('=', 1) for a in sys.argv[1:]]
for name, d in args:
    E = [epi(d, s) for s in range(1, 11)]
    P = [popmult(d, s) for s in range(1, 11)]; C = [credit(d, s) for s in range(1, 11)]
    pm = [med(list(p.values())) for p in P]; cm = [med(list(c.values())) for c in C]
    print(f"{name:6s} 疫病deadK年合計 中央値 {med(E):.0f} [{rg(E)[0]:.0f},{rg(E)[1]:.0f}] | 人口倍率1913/1783(9国中央値の種中央値) {med(pm):.3f} [{rg(pm)[0]:.3f},{rg(pm)[1]:.3f}] | 信用 貸出÷GDP最大(9国中央値) {med(cm):.3f} [{rg(cm)[0]:.3f},{rg(cm)[1]:.3f}]")
    by = {n: med([p.get(n, float('nan')) for p in P]) for n in NATS}
    print("       国別の人口倍率: " + ' '.join(f"{n}{by[n]:.2f}" for n in NATS))
