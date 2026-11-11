# (816) 旗 1 の腕 B の全ての本走の注入：票の外にある金鉱の需要の割合（excluded/world）と、州が入って票の和が跳ねた注入（入った票の割合の推定 >0.1）を数える。
import math
F1 = 'D:/PoB_work/keep_813/rej/c12svB_10/ins'
rows = []
with open('D:/PoB_work/r816_work/out/a1_entries.tsv', 'rb') as f:
    t = f.read().decode('utf-8').splitlines()
h = t[0].split('\t')
for l in t[1:]:
    rows.append(dict(zip(h, l.split('\t'))))
def fl(x):
    try: return float(x)
    except: return float('nan')
out = ['seed\tnRun\texclShareMedian\texclShareP90\texclShareMax\tnBigEntry(newShare>0.1)\tnBigEntryRelBelow0.5\tminRel\tminRelSeq\tminRelYear\tminRelIsBigEntry\tnRelBelow0.3\tnRelBelow0.5\tbigEntryYears']
ev = ['seed\tseq\tyear\tdN\tdW\tnewShareEst\tlnNewMeanW1Est\tusedRel\tprevUsedRel\tclampHit']
for s in range(1, 11):
    R = [r for r in rows if r['board'] == 'flag1' and int(r['seed']) == s]
    run = [r for r in R if r['metalGoldDemand'] != 'nan']
    ex = sorted(fl(r['excludedDemand']) / fl(r['metalGoldDemand']) for r in run if fl(r['metalGoldDemand']) > 0)
    big = []
    for i, r in enumerate(R):
        if r['metalGoldDemand'] == 'nan': continue
        if int(r['dN']) > 0 and fl(r['newWeightShareEst']) > 0.1:
            big.append(r)
            ev.append('\t'.join((str(s), r['seq'], r['year'], r['dN'], r['dW'], r['newWeightShareEst'], r['lnNewMeanW1Est'], r['usedRel'], R[i-1]['usedRel'], r['clampHit'])))
    mn = min(run, key=lambda r: fl(r['usedRel']))
    bigset = {r['seq'] for r in big}
    out.append('\t'.join(str(x) for x in (s, len(run), f'{ex[len(ex)//2]:.3f}', f'{ex[int(len(ex)*0.9)]:.3f}', f'{ex[-1]:.3f}', len(big), sum(1 for r in big if fl(r['usedRel']) < 0.5), mn['usedRel'], mn['seq'], mn['year'], int(mn['seq'] in bigset), sum(1 for r in run if fl(r['usedRel']) < 0.3), sum(1 for r in run if fl(r['usedRel']) < 0.5), ','.join(sorted({r['year'] for r in big})))))
open('D:/PoB_work/r816_work/out/a3_seeds.tsv', 'wb').write(('\n'.join(out) + '\n').encode('utf-8'))
open('D:/PoB_work/r816_work/out/a3_bigentries.tsv', 'wb').write(('\n'.join(ev) + '\n').encode('utf-8'))
