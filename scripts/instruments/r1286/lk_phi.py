# (1286) 紙の影：通貨改革を〈印刷の無い割合 φ に比例〉へ替えたら起き得る改革の月（読むだけ）
# φ＝1−印刷÷(印刷＋歳入)（fiscal_s の devaluedPerMonth・revenueBasePerMonth）。累積切下げ・切り下げ 0 の月・クールダウンは
# lk_fiscal.py と同じ組み立て直し〈近似〉。
# 使い方: python -X utf8 -I lk_phi.py <生> <出力.tsv> <種の下> <種の上>
import sys, os, csv, math
from collections import defaultdict
RAW, OUT, LO, HI = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4])
THR, CALM, COOL, MINF = 0.02, 24, 120, 0.85


def read(p):
    with open(p, encoding='utf-8', newline='') as fh:
        r = csv.reader(fh, delimiter='\t'); h = next(r)
        return {k: i for i, k in enumerate(h)}, list(r)


def fl(x):
    try:
        return float(x)
    except ValueError:
        return math.nan


cnt = defaultdict(int)
brit = defaultdict(int)
for s in range(LO, HI + 1):
    nix, nrows = read(os.path.join(RAW, f'nation_s{s}.tsv'))
    gdp = {(r[nix['nation']], int(r[nix['quarter']])): fl(r[nix['nominalGdpPerYear']]) / 12 for r in nrows}
    ix, rows = read(os.path.join(RAW, f'fiscal_s{s}.tsv'))
    byn = defaultdict(list)
    for r in rows:
        byn[r[ix['nation']]].append(r)
    for nat, rs in byn.items():
        rs.sort(key=lambda r: int(r[ix['monthIndex']]))
        cum = 0.0; nodev = 0; cd = 0; pending = False
        for r in rs:
            m = int(r[ix['monthIndex']])
            dev = fl(r[ix['devaluedPerMonth']]); rev = fl(r[ix['revenueBasePerMonth']])
            pr = fl(r[ix['payRateShare']]); gold = fl(r[ix['onGold']]) > 0.5
            g = gdp.get((nat, m // 3), math.nan)
            if dev > 0:
                cum += dev; nodev = 0
            else:
                nodev += 1
            if cd > 0:
                cd -= 1
            broken = g > 1e-9 and cum / g > THR
            if pending:
                pending = False
                if (not gold) and broken and nodev >= CALM and pr >= MINF and cd <= 0:
                    cum = 0.0; cd = COOL; broken = False
            if (not gold) and broken and pr >= MINF:
                d = dev if dev > 0 else 0.0
                phi = 1.0 - d / (d + rev) if (d + rev) > 0 else math.nan
                b = 'nan' if phi != phi else ('1' if phi >= 1.0 else ('ge0.5' if phi >= 0.5 else ('ge0.1' if phi >= 0.1 else 'lt0.1')))
                key = ('open' if (nodev >= CALM and cd <= 0) else ('cool' if nodev >= CALM else 'calm'))
                cnt[(key, b)] += 1
                if nat == 'イギリス':
                    brit[(key, b)] += 1
            if (m % 3) == 2 and (not gold) and cd <= 0 and broken and nodev >= CALM:
                pending = True
with open(OUT, 'w', encoding='utf-8', newline='\n') as o:
    o.write('who\tlockState\tphiBucket\tmonths\n')
    for (k, b), v in sorted(cnt.items()):
        o.write(f'all\t{k}\t{b}\t{v}\n')
    for (k, b), v in sorted(brit.items()):
        o.write(f'UK\t{k}\t{b}\t{v}\n')
print('ok')
