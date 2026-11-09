# (984) c89dd 塵の債務の門の台本（読むだけ）：Pop.Take の床の線の門 G1（働いた）・G3（上の側）と記録の量を、計器つきの腕の生から数える。
#   python -X utf8 -I gate984.py <名> <ins フォルダ> [--seeds 1 10]
#   エンジンの関数を呼ばない・事前登録の文から独立に書いた（塵の定義は read917_dust.py と同じ＝0 ＜ debt ≦1e-9・人の居る＝popK ＞1e-12）。
#   G1a：cpop_s の商人の行のうち人の居る行の塵の割合（種の和）。
#   G1b：cearn_s の billBlockedDustClearings の和（(920)＝清算の頭で商人の債務が 0 ＜Debt≦1e-12 のため手形を振り出せなかった清算の数）。
#   G3：cpop_s の全ての行で debt ＜0 の行の数（返し過ぎ＝この形に固有の誤り）。
#   記録：billBlockedClearings の和・塵の続きの本数（始まりの数）と中央・植民地格の州と本国格の州の importPaidPerQuarter の和・cpop の savings ＜0 の行。
import sys, os
from collections import defaultdict
args = sys.argv[1:]
s0, s1 = 1, 10
if '--seeds' in args:
    i = args.index('--seeds'); s0, s1 = int(args[i + 1]), int(args[i + 2]); del args[i:i + 3]
tag, d = args[0], args[1]
DEAD = 1e-12
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
def med(v):
    if not v: return float('nan')
    v = sorted(v); return v[(len(v) - 1) // 2]
tot = defaultdict(float)
print(f'# 名 {tag}・フォルダ {d}・種 {s0}〜{s1}')
print('seed\tliveRows\tliveDust\tdustShare\tdustRuns\trunMedian\tnegDebtRows\tnegSavRows\tdustLowSavRows\tbillBlockedDust\tbillBlocked\timportPaidColonial\timportPaidCore')
seeds = 0
for s in range(s0, s1 + 1):
    pp = os.path.join(d, f'cpop_s{s}.tsv'); pe = os.path.join(d, f'cearn_s{s}.tsv')
    if not (os.path.exists(pp) and os.path.exists(pe)): continue
    seeds += 1
    live = dust = neg = negs = lowsav = 0; ser = defaultdict(list)
    with open(pp, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        nd = len(h) - ix['debt']; iq, ir, ipi, icl, ipk, isv = ix['quarter'], ix['region'], ix['popIndex'], ix['className'], ix['popK'], ix['savings']
        for line in f:
            tail = line.rstrip('\n').rsplit('\t', nd)
            if tail[1].startswith('-') and fl(tail[1]) < 0: neg += 1
            if '\t-' in line:
                x = line.rstrip('\n').split('\t')
                if fl(x[isv]) < -1e-9: negs += 1
            if '\t商人\t' not in line: continue
            x = line.rstrip('\n').split('\t')
            if x[icl] != '商人' or not (fl(x[ipk]) > DEAD): continue
            D = fl(tail[1]); live += 1
            dz = 0 < D <= 1e-9
            if dz:
                dust += 1
                if not (fl(x[isv]) > 1e-9): lowsav += 1
            ser[(x[ir], x[ipi])].append((int(x[iq]), dz))
    runs = []
    for v in ser.values():
        run = 0; last = None
        for qq, c in v:
            if c and run and last is not None and qq == last + 1: run += 1
            else:
                if run: runs.append(run)
                run = 1 if c else 0
            last = qq
        if run: runs.append(run)
    bbd = bb = 0; ipc = ipm = 0.0
    with open(pe, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        it, iip, ib, ibd = ix['tier'], ix['importPaidPerQuarter'], ix['billBlockedClearings'], ix['billBlockedDustClearings']
        for line in f:
            x = line.rstrip('\n').split('\t')
            bb += int(x[ib]); bbd += int(x[ibd]); v = fl(x[iip])
            if v == v:
                if x[it] == 'Colonial': ipc += v
                elif x[it] == 'Core': ipm += v
    sh = dust / live if live else float('nan')
    print(f'{s}\t{live}\t{dust}\t{sh:.6f}\t{len(runs)}\t{med(runs)}\t{neg}\t{negs}\t{lowsav}\t{bbd}\t{bb}\t{ipc:.6g}\t{ipm:.6g}')
    for k, v in (('live', live), ('dust', dust), ('runs', len(runs)), ('neg', neg), ('negs', negs), ('lowsav', lowsav), ('bbd', bbd), ('bb', bb), ('ipc', ipc), ('ipm', ipm)): tot[k] += v
r = lambda a, b: a / b if b else float('nan')
print(f'計\t{int(tot["live"])}\t{int(tot["dust"])}\t{r(tot["dust"], tot["live"]):.6f}\t{int(tot["runs"])}\t-\t{int(tot["neg"])}\t{int(tot["negs"])}\t{int(tot["lowsav"])}\t{int(tot["bbd"])}\t{int(tot["bb"])}\t{tot["ipc"]:.6g}\t{tot["ipm"]:.6g}')
print(f'# 種 {seeds}・塵の続きの始まり÷人の居る行＝{r(tot["runs"], tot["live"]):.6f}（旗 1 で四半期の末に残る塵の下限の目安）')
