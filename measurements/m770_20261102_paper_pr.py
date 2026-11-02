# (770) c6pr の紙の予言の台本（読むだけ・開幕の盤面・m764 の陸グラフ）。
# 入力＝measurements/m770_20261102_probe_PaperProbe.cs.txt の探針（Tests に置いて回す使い捨て）が吐く paper_raw.tsv
#   （内陸州×同じ国の港の対ごとに gcKm・landKm〈∞は inf〉・港の海盆。行の順＝州の並び×港の並び＝WirePortDistances と同じ先着の順）。
# 規則：旧＝大圏の最小（同率は先着）／新＝港ごとに eff＝(陸路∞ ? 大圏 : 陸路) の最小（同率は先着）／変異（波54 の形）＝陸路が有限の港だけから陸路の最小・有限なしは大圏の最小。
# python -X utf8 m770_20261102_paper_pr.py <paper_raw.tsv>
import sys, math, statistics as st, collections
K, N = 1.0, 10000.0          # InlandPortDistanceDecay の既定・Diplomacy.DistanceNormKm
dec = lambda km: 1.0 / (1.0 + K * km / N)
groups = collections.OrderedDict()
with open(sys.argv[1], encoding='utf-8') as fh:
    head = fh.readline().rstrip('\n').split('\t'); ix = {h: i for i, h in enumerate(head)}
    for ln in fh:
        c = ln.rstrip('\n').split('\t')
        if len(c) < len(head): continue
        key = (c[ix['nation']], c[ix['region']])
        land = float('inf') if c[ix['landKm']] == 'inf' else float(c[ix['landKm']])
        groups.setdefault(key, []).append((c[ix['port']], float(c[ix['gcKm']]), land, c[ix['basin']], c[ix['nearestName']], float(c[ix['nearestKm']])))
def eff(g, l): return g if math.isinf(l) else l
rows = []; allInf = []; chg = []; mutChg = []
for (nat, reg), ps in groups.items():
    if not ps: continue
    old = min(ps, key=lambda p: p[1])                      # 先着＝min は最初の最小を返す
    new = min(ps, key=lambda p: eff(p[1], p[2]))
    fin = [p for p in ps if not math.isinf(p[2])]
    mut = min(fin, key=lambda p: p[2]) if fin else old
    if not fin: allInf.append((nat, reg, old[0], old[1]))
    effOld = eff(old[1], old[2]); effNew = eff(new[1], new[2])
    rows.append((nat, reg, old, new, effOld, effNew, dec(effNew) / dec(old[1])))
    if new[0] != old[0]: chg.append((nat, reg, old[0], new[0], old[1], effOld, effNew, old[3], new[3]))
    if mut[0] != old[0]: mutChg.append((nat, reg, old[0], mut[0], old[1], eff(old[1], old[2]), eff(mut[1], mut[2])))
print('inland states (with a port)', len(rows))
print('new-rule port changes', len(chg))
for c in chg: print('   %s %s %s -> %s  gcOld=%.0f effOld=%.0f effNew=%.0f  basin %s -> %s' % (c[0], c[1], c[2], c[3], c[4], c[5], c[6], c[7], c[8]))
rt = [r[6] for r in rows]
print('rail decay ratio (new/old, old = gc-based as in the m764 engine): median %.4f min %.4f n<0.99 %d  sum(1-ratio)=%.4f' % (st.median(rt), min(rt), sum(1 for x in rt if x < 0.99), sum(1 - x for x in rt)))
print('  of which ratio>1 (new closer than old gc-based): %d, sum(ratio-1 where >1)=%.4f' % (sum(1 for x in rt if x > 1), sum(x - 1 for x in rt if x > 1)))
mn = min(rows, key=lambda r: r[6]); print('  min row', mn[0], mn[1], 'old', mn[2][0], 'new', mn[3][0], 'ratio %.4f' % mn[6])
bch = [c for c in chg if c[7] != c[8]]
print('basin changes', len(bch), [(c[0], c[1], c[7], c[8]) for c in bch])
print('all-land-inf states (gc fallback)', len(allInf), [(a[0], a[1], a[2], '%.0f' % a[3]) for a in allInf])
# 旧の PortLandKm（portGc までの陸路。∞は gc）との比（fareLand の新÷旧）
fl = [r[5] / r[4] for r in rows if r[4] > 0]
print('fareLand-like ratio effNew/effOld: median %.4f min %.4f n<0.99 %d' % (st.median(fl), min(fl), sum(1 for x in fl if x < 0.99)))
u2 = [c for c in chg if c[6] > 2 * c[4]]
print('U2 (new rule): changed rows with effNew > 2*gcOld: %d / %d' % (len(u2), len(chg)))
print('mutation (finite-only, wave-54 form): changes %d' % len(mutChg))
mu2 = [c for c in mutChg if c[6] > 2 * c[4]]
print('  changed rows with landNew > 2*gcOld: %d / %d  U1 violations (effNew > effOld+1e-9): %d' % (len(mu2), len(mutChg), sum(1 for c in mutChg if c[6] > c[5] + 1e-9)))
for c in mutChg: print('   mut %s %s %s -> %s gcOld=%.0f effOld=%.0f effNew=%.0f' % c)
# U2 の母集団：旧の eff が 2×大圏を超える州（ここで港が変われば landKmNew>2×gcKm になりうる）
big = [(r[0], r[1], r[2][0], r[2][1], r[4]) for r in rows if r[4] > 2 * r[2][1]]
print('states with effOld > 2*gcOld: %d / %d' % (len(big), len(rows)), ['%s %s %s gc=%.0f eff=%.0f' % b for b in big])
