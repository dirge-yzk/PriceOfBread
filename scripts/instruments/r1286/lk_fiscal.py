# (1286) 紙の影：借入の禁止の月と通貨改革の錠（fiscal_s＋nation_s・読むだけ）
# 使い方: python -X utf8 -I lk_fiscal.py <生のフォルダ> <出力フォルダ>
# 生に無い量（CumulativeDevaluation・NoDevaluationMonths・CurrencyReformCooldown・正貨のカバー率・MetallicStandardLaw）は
# 生の devaluedPerMonth と nominalGdpPerYear から組み立て直す〈近似〉。組み立て直せない物は〈読めない〉と書く。
import sys, os, math, csv
from collections import defaultdict

RAW, OUT = sys.argv[1], sys.argv[2]
SEEDS = range(int(sys.argv[3]), int(sys.argv[4]) + 1) if len(sys.argv) > 4 else range(1, 11)
THR = 0.02          # CapitalFlightDistrustThreshold（p08:41）＝累積切下げ÷月の名目GDP
CALM = 24           # CurrencyReformCalmMonths（p03:126）
COOL = 120          # CurrencyReformCooldownMonths（p03:127）
MINF = 0.85         # GoldStandardMinFunding（p03:111）
HALF_YEARS = [15, 25, 35]   # 世代の入れ替わりの半減期〈推〉の感度


def f(x):
    try:
        v = float(x)
    except ValueError:
        return math.nan
    return v


def read(path):
    with open(path, encoding='utf-8', newline='') as fh:
        r = csv.reader(fh, delimiter='\t')
        h = next(r)
        ix = {k: i for i, k in enumerate(h)}
        return ix, list(r)


tot = defaultdict(float)
pern = defaultdict(lambda: defaultdict(float))
events = []
for s in SEEDS:
    nix, nrows = read(os.path.join(RAW, f'nation_s{s}.tsv'))
    gdp = {}
    for row in nrows:
        gdp[(row[nix['nation']], int(row[nix['quarter']]))] = f(row[nix['nominalGdpPerYear']]) / 12.0
    ix, rows = read(os.path.join(RAW, f'fiscal_s{s}.tsv'))
    byn = defaultdict(list)
    for row in rows:
        byn[row[ix['nation']]].append(row)
    for nat, rs in byn.items():
        rs.sort(key=lambda r: int(r[ix['monthIndex']]))
        prev_dc = 0.0
        cum = 0.0; nodev = 0; cd = 0; pending = False
        mems = {h: 0.0 for h in HALF_YEARS}
        last_def = None; first_borrow_after = None
        for row in rs:
            m = int(row[ix['monthIndex']]); yr = int(row[ix['year']])
            cdm = f(row[ix['borrowCooldownMonths']])
            gate = f(row[ix['borrowGateOpenFlag']]); lim = f(row[ix['interestAtLimitFlag']])
            sf = f(row[ix['borrowShortfallPerMonth']]); tk = f(row[ix['borrowTicketPerMonth']])
            bor = f(row[ix['borrowedPerMonth']]); dev = f(row[ix['devaluedPerMonth']])
            pr = f(row[ix['payRateShare']]); gold = f(row[ix['onGold']]) > 0.5
            dc = f(row[ix['defaultCount']])
            g = gdp.get((nat, m // 3), math.nan)
            # --- 借入の禁止の月 ---
            if dc > prev_dc:
                tot['defaults'] += 1; pern[nat]['defaults'] += 1
                events.append((s, nat, yr, 'default', ''))
                if cdm > 0 and last_def is not None:
                    pass
                last_def = m; first_borrow_after = None
            prev_dc = dc if dc == dc else prev_dc
            if cdm > 0:
                tot['cooldownMonths'] += 1; pern[nat]['cooldownMonths'] += 1
            if gate == 0 and lim == 0 and sf > 0:
                # 門が閉じ、利払いの門は開いていた＝禁止の月（か開幕の凍結）だけで閉じた月
                tot['lockBindMonths'] += 1; pern[nat]['lockBindMonths'] += 1
                w = min(sf, tk) if tk == tk else sf
                tot['lockWouldBorrow'] += w; pern[nat]['lockWouldBorrow'] += w
                if pr < 1:
                    tot['lockBindUnpaid'] += 1; pern[nat]['lockBindUnpaid'] += 1
            if last_def is not None and first_borrow_after is None and bor > 0:
                first_borrow_after = m - last_def
                tot['monthsToFirstBorrowSum'] += first_borrow_after; tot['monthsToFirstBorrowN'] += 1
            # --- 通貨改革（組み立て直し〈近似〉）---
            if dev > 0:
                cum += dev; nodev = 0
            else:
                nodev += 1
            for h in HALF_YEARS:
                lam = 1.0 - 0.5 ** (1.0 / (12.0 * h))
                mems[h] = mems[h] * (1.0 - lam) + (dev if dev > 0 else 0.0)
            if cd > 0:
                cd -= 1
            broken = g > 1e-9 and cum / g > THR
            if pending:
                pending = False
                if (not gold) and broken and nodev >= CALM and pr >= MINF and cd <= 0:
                    tot['reforms'] += 1; pern[nat]['reforms'] += 1
                    events.append((s, nat, yr, 'reform', f'{cum / g:.3f}'))
                    cum = 0.0; cd = COOL
                    broken = False
            if not gold:
                tot['offGold'] += 1; pern[nat]['offGold'] += 1
                if broken:
                    tot['offGoldBroken'] += 1; pern[nat]['offGoldBroken'] += 1
                    if nodev < CALM and cd <= 0:
                        tot['blockedByCalm'] += 1; pern[nat]['blockedByCalm'] += 1
                        if nodev >= 1:
                            tot['blockedByCalmSomeCalm'] += 1
                    if nodev >= CALM and cd > 0:
                        tot['blockedByCoolOnly'] += 1; pern[nat]['blockedByCoolOnly'] += 1
                    if dev > 0:
                        tot['offGoldBrokenPrinting'] += 1; pern[nat]['offGoldBrokenPrinting'] += 1
                for h in HALF_YEARS:
                    if g > 1e-9 and mems[h] / g > THR:
                        tot[f'offGoldBrokenDecay{h}'] += 1; pern[nat][f'offGoldBrokenDecay{h}'] += 1
            if (m % 3) == 2 and (not gold) and cd <= 0 and broken and nodev >= CALM:
                pending = True

os.makedirs(OUT, exist_ok=True)
with open(os.path.join(OUT, 'lk_fiscal_total.tsv'), 'w', encoding='utf-8', newline='\n') as o:
    for k in sorted(tot):
        o.write(f'{k}\t{tot[k]:.6g}\n')
keys = sorted({k for d in pern.values() for k in d})
with open(os.path.join(OUT, 'lk_fiscal_nation.tsv'), 'w', encoding='utf-8', newline='\n') as o:
    o.write('nation\t' + '\t'.join(keys) + '\n')
    for nat in sorted(pern):
        o.write(nat + '\t' + '\t'.join(f'{pern[nat].get(k, 0):.6g}' for k in keys) + '\n')
with open(os.path.join(OUT, 'lk_fiscal_events.tsv'), 'w', encoding='utf-8', newline='\n') as o:
    o.write('seed\tnation\tyear\tkind\tcumDevToGdpMonth\n')
    for e in events:
        o.write('\t'.join(str(x) for x in e) + '\n')
print('ok', len(events))
