# (1135) c110rn 段 R の need の基底の紙（事前登録 D:/PoB_work/d1135_work/prereg.txt の [1]〜[5]）。
# 呼び方：python -X utf8 -I needbasis_rn.py <ins の生> <ラベル> <出力の txt>
# 読む表：trrail_s・trnation_s・region_s（種 1〜10）。書き込みは出力の txt だけ。
import sys, os, glob, math
from collections import defaultdict

ins, label, outp = sys.argv[1], sys.argv[2], sys.argv[3]

def q(xs, p):
    if not xs: return float('nan')
    return xs[min(len(xs) - 1, int(p * len(xs)))]

lnpot, lnout, lnpop = [], [], []
ratio_out_pot = []
rows = 0; low_old = 0; low_static = 0; tzero = 0; lowk = [0, 0]
shrink4 = 0; outzero = 0; grow4 = 0
phi_ratio = []
seeds = 0
for path in sorted(glob.glob(os.path.join(ins, 'trrail_s*.tsv'))):
    sd = path.rsplit('_s', 1)[1].split('.')[0]
    rp = os.path.join(ins, f'region_s{sd}.tsv'); tp = os.path.join(ins, f'trnation_s{sd}.tsv')
    if not (os.path.exists(rp) and os.path.exists(tp)): continue
    seeds += 1
    reg = {}
    with open(rp, encoding='utf-8') as f:
        hd = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(hd)}
        iy, inat, irg, iact = ix['year'], ix['nation'], ix['region'], ix['activeFlag']
        ifo, ieo, ipop = ix['factoryOutputRefValuePerMonth'], ix['extractionOutputRefValuePerMonth'], ix['populationK']
        for line in f:
            c = line.rstrip('\n').split('\t')
            if c[iact] != '1': continue
            try: reg[(c[irg], int(c[iy]))] = (c[inat], max(0.0, float(c[ifo]) + float(c[ieo])), max(0.0, float(c[ipop])))
            except ValueError: pass
    natY = defaultdict(float); natP = defaultdict(float)
    for (rg, y), (nn, yv, pv) in reg.items(): natY[(nn, y)] += yv; natP[(nn, y)] += pv
    tg = {}
    with open(tp, encoding='utf-8') as f:
        hd = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(hd)}
        for line in f:
            c = line.rstrip('\n').split('\t')
            try: tg[(int(c[ix['year']]), c[ix['nation']])] = float(c[ix['railTargetIndex']])
            except (ValueError, IndexError, KeyError): pass
    prev = {}
    with open(path, encoding='utf-8') as f:
        hd = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(hd)}
        iy, inat, irg, iw, ineed, iphi, it = ix['year'], ix['nation'], ix['region'], ix['waterFlag'], ix['needEnd'], ix['phiEnd'], ix['railTrackEnd']
        for line in f:
            c = line.rstrip('\n').split('\t')
            need = float(c[ineed])
            if not (need > 0) or c[iw] == '1': continue
            y = int(c[iy]); nn = c[inat]; rg = c[irg]
            t = tg.get((y, nn))
            if not (t and t > 0): continue
            rv = reg.get((rg, y))
            if rv is None or rv[0] != nn or not (natY[(nn, y)] > 0) or not (natP[(nn, y)] > 0): continue
            rows += 1
            spot = need / t; sout = rv[1] / natY[(nn, y)]; spop = rv[2] / natP[(nn, y)]
            T = float(c[it]); phi = float(c[iphi])
            if phi < 0.99: low_old += 1
            nneed = t * sout
            phin = (1.0 if T > 0 else 0.0) if not (nneed > 0) else min(1.0, max(0.0, T / nneed))
            if phin < 0.99: low_static += 1
            if not (T > 0): tzero += 1
            for kk in (0, 1):
                nk = nneed * (12.0 if kk == 0 else 1.0 / 12.0)
                pk = (1.0 if T > 0 else 0.0) if not (nk > 0) else min(1.0, max(0.0, T / nk))
                if pk < 0.99: lowk[kk] += 1
            if phi > 0: phi_ratio.append(phin / phi)
            if not (sout > 0): outzero += 1
            if sout < 0.25 * spot: shrink4 += 1
            if sout > 4 * spot: grow4 += 1
            if spot > 0: ratio_out_pot.append(sout / spot)
            key = (nn, rg)
            pv = prev.get(key)
            if pv is not None and pv[0] == y - 1:
                if spot > 0 and pv[1] > 0: lnpot.append(abs(math.log(spot / pv[1])))
                if sout > 0 and pv[2] > 0: lnout.append(abs(math.log(sout / pv[2])))
                if spop > 0 and pv[3] > 0: lnpop.append(abs(math.log(spop / pv[3])))
            prev[key] = (y, spot, sout, spop)
for xs in (lnpot, lnout, lnpop, ratio_out_pot, phi_ratio): xs.sort()
out = [f'== {label} {ins} 種 {seeds}',
       f'P の行 {rows}・前年比の行 pot {len(lnpot)}・out {len(lnout)}・pop {len(lnpop)}',
       f'W_pot 中央 {q(lnpot, .5):.4f}・90 点 {q(lnpot, .9):.4f}',
       f'W_out 中央 {q(lnout, .5):.4f}・90 点 {q(lnout, .9):.4f}',
       f'W_pop 中央 {q(lnpop, .5):.5f}・90 点 {q(lnpop, .9):.5f}',
       f'φ＜0.99 今 {low_old / max(1, rows):.4f}・静的な紙（同じ T で need を産出の比へ） {low_static / max(1, rows):.4f}',
       f'〈追加〉T＝0 の行 {tzero / max(1, rows):.4f}・静的な紙 need×12 の φ＜0.99 {lowk[0] / max(1, rows):.4f}・need÷12 の φ＜0.99 {lowk[1] / max(1, rows):.4f}',
       f'S_out÷S_pot 10／50／90 点 {q(ratio_out_pot, .1):.3f}／{q(ratio_out_pot, .5):.3f}／{q(ratio_out_pot, .9):.3f}',
       f'φ 新÷旧（静的・旧 φ＞0） 10／50／90 点 {q(phi_ratio, .1):.3f}／{q(phi_ratio, .5):.3f}／{q(phi_ratio, .9):.3f}',
       f'中継の港の型 S_out＜S_pot÷4 {shrink4 / max(1, rows):.4f}・S_out＝0 {outzero / max(1, rows):.5f}・S_out＞4×S_pot {grow4 / max(1, rows):.4f}']
with open(outp, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(out) + '\n')
print('\n'.join(out))
