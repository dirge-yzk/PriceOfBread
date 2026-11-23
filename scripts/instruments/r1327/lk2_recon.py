# (1327) 段 L2 の紙：組み立て直しをエンジンの月の順に合わせ、逆向きの不一致を分類し、案 B の規則で改革を起こす影を数える（読むだけ）
# 使い方: python -X utf8 -I lk2_recon.py <生のフォルダ> <出力フォルダ> <名> <種の下> <種の上>
# 月の順（Simulation.cs:1922〜:1976 → :2026 Fiscal.Step → :2061 MonthClose の行）：
#   (1) 切り下げ：cum += dev・nodev（Simulation.cs:1963・:1972）
#   (2) Fiscal.Step：cd--（Fiscal.cs:104）→ broken を改革の前の cum で（:105）→ 予約の消化（:106〜:137）→ 復帰の門は改革の前の broken（:232〜:234）
#   (3) 行を書く（onGold は門の後・revenueBasePerMonth は :99 の LastRevenuePerMonth＝この月の budgetBase）
#   (4) 四半期の頭（Simulation.cs:1509 QuarterStart の DecideQuarter）＝次の行の (2) で消化。読むのは前の月の後の値。
# φ＝R⁺÷(R⁺＋D)・D＝この月の切り下げ（Fiscal.Step の引数 devalued と同じ値＝Instruments.Rate の st.Devalued）・R⁺＝max(0, budgetBase)。D＝0 なら φ＝1。
# 名目 GDP は nation_s の四半期の nominalGdpPerYear÷12（エンジンは月の NominalOf(GdpPerMonth)）〈近似〉。
import sys, os, csv, math
from collections import defaultdict

RAW, OUT, NAME, LO, HI = sys.argv[1], sys.argv[2], sys.argv[3], int(sys.argv[4]), int(sys.argv[5])
PHASE = int(sys.argv[6]) if len(sys.argv) > 6 else 2   # 四半期の頭の判断を置く行（monthIndex %3）
THR, CALM, COOL, MINF = 0.02, 24, 120, 0.85     # p08:41・p03:126・p03:127・GoldStandardMinFunding
BURN, LEG = 0.5, 0.35                           # p03:123・p03:125


def fl(x):
    try:
        return float(x)
    except ValueError:
        return math.nan


def read(p):
    with open(p, encoding='utf-8', newline='') as fh:
        r = csv.reader(fh, delimiter='\t'); h = next(r)
        return {k: i for i, k in enumerate(h)}, list(r)


def phi_of(dev, rev):
    if not (dev >= 0) or not (rev == rev):
        return 0.0                               # NaN は改革を起こさない側（否定形）
    if dev == 0:
        return 1.0
    rp = rev if rev > 0 else 0.0
    return rp / (rp + dev)


tot = defaultdict(float)
mism = []          # 逆向きの不一致の行
evB = []           # 案 B の影の改革
evA = []           # 今の規則の組み立て直しの改革
perNS = defaultdict(lambda: [0, 0])   # (seed, nation) -> [A の件数, B の件数]
for s in range(LO, HI + 1):
    nix, nrows = read(os.path.join(RAW, f'nation_s{s}.tsv'))
    gq = {}
    for r in nrows:
        gq[(r[nix['nation']], int(r[nix['quarter']]))] = fl(r[nix['nominalGdpPerYear']]) / 12.0
    ix, rows = read(os.path.join(RAW, f'fiscal_s{s}.tsv'))
    byn = defaultdict(list)
    for r in rows:
        byn[r[ix['nation']]].append(r)
    for nat, rs in byn.items():
        rs.sort(key=lambda r: int(r[ix['monthIndex']]))
        perNS[(s, nat)]
        cumA = 0.0; nodev = 0; cdA = 0; pendA = False; goldPrev = None
        cumB = 0.0; pendB = False; lastRefB = None
        lastEligA = None   # 今の規則で改革の門の印刷の項（nodev≥24）が開いていた最後の月
        for r in rs:
            m = int(r[ix['monthIndex']]); yr = int(r[ix['year']])
            dev = fl(r[ix['devaluedPerMonth']]); rev = fl(r[ix['revenueBasePerMonth']])
            pr = fl(r[ix['payRateShare']]); gold = fl(r[ix['onGold']]) > 0.5
            g = gq.get((nat, m // 3), math.nan)
            if dev != dev:
                tot['devNaN'] += 1
            d = dev if dev > 0 else 0.0
            # (1)
            cumA += d; cumB += d
            nodev = 0 if d > 0 else nodev + 1
            # (2) 今の規則
            if cdA > 0:
                cdA -= 1
            gp = goldPrev if goldPrev is not None else gold
            brokenA = g > 1e-9 and cumA / g > THR
            if pendA:
                pendA = False
                if (not gp) and brokenA and nodev >= CALM and pr >= MINF and cdA <= 0:
                    evA.append((NAME, s, nat, yr, m, cumA / g)); perNS[(s, nat)][0] += 1
                    cumA = 0.0; cdA = COOL
            if (not gp) and nodev >= CALM:
                lastEligA = m
            brokenA2 = g > 1e-9 and cumA / g > THR
            if (not gp) and brokenA2 and pr >= MINF:
                ph0 = phi_of(dev, rev)
                if not (nodev >= CALM and cdA <= 0):
                    tot['lockClosedPhi1' if ph0 >= 1.0 else ('lockClosedPhiGe05' if ph0 >= 0.5 else 'lockClosedPhiLt05')] += 1
                    if cdA > 0 and nodev >= CALM:
                        tot['lockClosedCoolOnly'] += 1
            # 逆向きの不一致：実際に復帰した月に、組み立て直しの改革の前の記憶が壊れている
            if goldPrev is False and gold:
                tot['rejoin'] += 1
                if brokenA:
                    tot['rejoinBrokenRecon'] += 1
                    gs = [gq.get((nat, q), math.nan) for q in (m // 3 - 1, m // 3, m // 3 + 1)]
                    gs = [x for x in gs if x == x and x > 0]
                    ratio = cumA / g
                    ratioHi = cumA / max(gs) if gs else math.nan
                    cls = 'near' if ratioHi <= THR else ('elig' if (lastEligA is not None and m - lastEligA <= 3) else 'noelig')
                    tot['rejoinBroken_' + cls] += 1
                    mism.append((NAME, s, nat, yr, m, f'{ratio:.4f}', f'{ratioHi:.4f}', nodev, f'{pr:.3f}', cls))
            # (2) 案 B の影：門は !gold ∧ broken ∧ pr≥0.85・φ を掛ける
            brokenB = g > 1e-9 and cumB / g > THR
            if pendB:
                pendB = False
                if (not gp) and brokenB and pr >= MINF:
                    ph = phi_of(dev, rev)
                    forg = ph * cumB
                    leg = LEG * min(1.0, forg / g)
                    gap = (m - lastRefB) if lastRefB is not None else -1
                    evB.append((NAME, s, nat, yr, m, f'{ph:.4f}', f'{cumB / g:.4f}', f'{forg / g:.4f}', f'{BURN * forg / g:.4f}', f'{leg:.4f}', gap))
                    perNS[(s, nat)][1] += 1; lastRefB = m
                    cumB -= forg
            # 読み手の飽和（broken の月・資本逃避の不信 Clamp01(X−0.02) の和）＝改革の後の記憶で
            if g > 1e-9:
                xA = cumA / g; xB = cumB / g
                tot['brokenMonthsA'] += xA > THR; tot['brokenMonthsB'] += xB > THR
                tot['distrustSumA'] += min(1.0, max(0.0, xA - THR)); tot['distrustSumB'] += min(1.0, max(0.0, xB - THR))
            goldPrev = gold
            # (4) 四半期の頭の判断（次の行で消化）＝この行の後の値で読む
            if (m % 3) == PHASE:
                bA = g > 1e-9 and cumA / g > THR
                if (not gold) and cdA <= 0 and bA and nodev >= CALM:
                    pendA = True
                bB = g > 1e-9 and cumB / g > THR
                if (not gold) and bB and nodev >= 1:
                    pendB = True

os.makedirs(OUT, exist_ok=True)
with open(os.path.join(OUT, f'lk2_total_{NAME}.tsv'), 'w', encoding='utf-8', newline='\n') as o:
    for k in sorted(tot):
        o.write(f'{k}\t{tot[k]:.6g}\n')
    a = [v[0] for v in perNS.values()]; b = [v[1] for v in perNS.values()]
    o.write(f'reformsA\t{sum(a)}\nreformsB\t{sum(b)}\nnationSeeds\t{len(perNS)}\n')
    o.write(f'maxPerNationSeedA\t{max(a) if a else 0}\nmaxPerNationSeedB\t{max(b) if b else 0}\n')
    for k in (0, 1, 2, 3, 4, 5, 10, 20, 40):
        o.write(f'nationSeedsB_ge{k}\t{sum(1 for x in b if x >= k)}\n')
with open(os.path.join(OUT, f'lk2_mismatch_{NAME}.tsv'), 'w', encoding='utf-8', newline='\n') as o:
    o.write('arm\tseed\tnation\tyear\tmonth\treconRatio\treconRatioAtMaxGdp\tnodev\tpayRate\tclass\n')
    for e in mism:
        o.write('\t'.join(str(x) for x in e) + '\n')
with open(os.path.join(OUT, f'lk2_reformB_{NAME}.tsv'), 'w', encoding='utf-8', newline='\n') as o:
    o.write('arm\tseed\tnation\tyear\tmonth\tphi\tmemToGdpM\tforgivenToGdpM\tburnMemToGdpM\tlegitHit\tmonthsSincePrevB\n')
    for e in evB:
        o.write('\t'.join(str(x) for x in e) + '\n')
with open(os.path.join(OUT, f'lk2_reformA_{NAME}.tsv'), 'w', encoding='utf-8', newline='\n') as o:
    o.write('arm\tseed\tnation\tyear\tmonth\tmemToGdpM\n')
    for e in evA:
        o.write('\t'.join(str(x) if not isinstance(x, float) else f'{x:.4f}' for x in e) + '\n')
with open(os.path.join(OUT, f'lk2_perns_{NAME}.tsv'), 'w', encoding='utf-8', newline='\n') as o:
    o.write('seed\tnation\treformsA\treformsB\n')
    for (s, n), v in sorted(perNS.items()):
        o.write(f'{s}\t{n}\t{v[0]}\t{v[1]}\n')
print('ok', NAME, len(evA), len(evB), len(mism))
