# (705) 段0 の記録：括弧だけを外した canary（C5・TechProductivityBracketOff=1）と旗 0 の canary（C0）の同じ種の比
# 世界の工場の能力（factory_s の capacityRefValuePerMonth・計器 9 国の和）・産出（outputRefValuePerMonth）・投資の信号（modsig_s の returnShare・9 国の中央値）を十年ごとに。
# 使い方：python c5cmp.py <C0 の ins> <C5 の ins> <種>
import sys, statistics
from collections import defaultdict

NINE = {"清", "ロシア", "オスマン", "フランス", "イギリス", "プロイセン", "オーストリア", "日本", "アメリカ"}


def fac(path):
    cap = defaultdict(float); out = defaultdict(float)
    with open(path, 'rb') as f:
        h = f.readline().decode().rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            a = line.decode().rstrip('\r\n').split('\t')
            if a[ix['nation']] not in NINE or int(a[ix['quarter']]) % 4 != 3:
                continue
            y = int(a[ix['year']])
            try:
                cap[y] += float(a[ix['capacityRefValuePerMonth']]); out[y] += float(a[ix['outputRefValuePerMonth']])
            except ValueError:
                pass
    return cap, out


def mod(path):
    rs = defaultdict(list)
    with open(path, 'rb') as f:
        h = f.readline().decode().rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            a = line.decode().rstrip('\r\n').split('\t')
            if a[ix['nation']] not in NINE or int(a[ix['quarter']]) % 4 != 3:
                continue
            try:
                rs[int(a[ix['year']])].append(float(a[ix['returnShare']]))
            except ValueError:
                pass
    return {y: statistics.median(v) for y, v in rs.items()}


a, b, s = sys.argv[1], sys.argv[2], sys.argv[3]
ca, oa = fac(f"{a}/factory_s{s}.tsv"); cb, ob = fac(f"{b}/factory_s{s}.tsv")
ma = mod(f"{a}/modsig_s{s}.tsv"); mb = mod(f"{b}/modsig_s{s}.tsv")
print("年\t能力 C5÷C0\t産出 C5÷C0\t投資の信号 C0\tC5")
for y in sorted(ca):
    if y % 10 != 0 and y != 1913:
        continue
    r1 = cb.get(y, float('nan')) / ca[y] if ca[y] > 0 else float('nan')
    r2 = ob.get(y, float('nan')) / oa[y] if oa.get(y, 0) > 0 else float('nan')
    print(f"{y}\t{r1:.3f}\t{r2:.3f}\t{ma.get(y, float('nan')):.3f}\t{mb.get(y, float('nan')):.3f}")
