# (1286) 組み立て直しの点検と英の本位の外の続き（読むだけ）
# 使い方: python -X utf8 -I lk_check.py <生のフォルダ> <lk_fiscal_events.tsv> <出力>
import sys, os, csv, math
from collections import defaultdict
RAW, EV, OUT = sys.argv[1], sys.argv[2], sys.argv[3]


def read(p):
    with open(p, encoding='utf-8', newline='') as fh:
        r = csv.reader(fh, delimiter='\t'); h = next(r)
        return {k: i for i, k in enumerate(h)}, list(r)


ev = defaultdict(list)
with open(EV, encoding='utf-8') as fh:
    next(fh)
    for line in fh:
        s, n, y, k, x = line.rstrip('\n').split('\t')
        if k == 'reform':
            ev[(int(s), n)].append(int(y))
out = []
for s in (range(int(sys.argv[4]), int(sys.argv[5]) + 1) if len(sys.argv) > 5 else range(1, 11)):
    nix, nrows = read(os.path.join(RAW, f'nation_s{s}.tsv'))
    gdp = {(r[nix['nation']], int(r[nix['quarter']])): float(r[nix['nominalGdpPerYear']]) / 12 for r in nrows}
    ix, rows = read(os.path.join(RAW, f'fiscal_s{s}.tsv'))
    byn = defaultdict(list)
    for r in rows:
        byn[r[ix['nation']]].append(r)
    for nat, rs in byn.items():
        rs.sort(key=lambda r: int(r[ix['monthIndex']]))
        gold = [float(r[ix['onGold']]) > 0.5 for r in rs]
        years = [int(r[ix['year']]) for r in rs]
        # 改革の年の後 36 か月で金へ戻ったか
        for y in ev.get((s, nat), []):
            i0 = years.index(y)
            back = any(gold[i0:i0 + 48])
            out.append(f'reformCheck\t{s}\t{nat}\t{y}\tgoldWithin48m={int(back)}')
        # 最後の本位の外の続き
        if not gold[-1]:
            j = len(gold) - 1
            while j > 0 and not gold[j - 1]:
                j -= 1
            # 続きの中の印刷の月の割合と、印刷÷月の名目GDP の中央値
            dv = []
            for r in rs[j:]:
                d = float(r[ix['devaluedPerMonth']]); g = gdp.get((nat, int(r[ix['monthIndex']]) // 3), math.nan)
                if g > 0:
                    dv.append(d / g)
            pm = sum(1 for x in dv if x > 0) / max(1, len(dv))
            dvp = sorted(x for x in dv if x > 0)
            med = dvp[len(dvp) // 2] if dvp else 0.0
            out.append(f'offAtEnd\t{s}\t{nat}\tfrom={years[j]}\tmonths={len(rs) - j}\tprintShare={pm:.2f}\tmedPrintToGdpM={med:.4f}')
with open(OUT, 'w', encoding='utf-8', newline='\n') as o:
    o.write('\n'.join(out) + '\n')
print('ok', len(out))
