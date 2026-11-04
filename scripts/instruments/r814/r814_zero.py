"""(814) 州の稼ぎが事実上 0（1 人あたりの月の稼ぎ earnedPerMonth/populationK < 閾値）になる州×四半期を全ての組で数える（読むだけ）。
使い方: python -I r814_zero.py <出力.tsv> <ラベル>=<ディレクトリ> ...
各 (組, 種, 州) について：稼ぎ≒0 の四半期の数・最初の年・最長の連続・その間の採取と工場の産出が 0 の四半期の割合・雇用の中央・tier・持ち主の国（最初）・
ピーク人口と最後の人口・1/4 未満に落ちたか。"""
import sys, os, math, collections
def fl(x):
    try: return float(x)
    except: return float('nan')
TH = 1e-6
out = sys.argv[1]; sets = [a.split('=',1) for a in sys.argv[2:]]
o = open(out, 'w', encoding='utf-8', newline='\n')
o.write('\t'.join(['set','seed','region','tier','nationFirstZero','zeroQ','firstZeroYear','longestRunQ','runStartYear','runEndYear','prodZeroShareInZero','empMedianInZero','peakK','endK','minOverPeak','below4','nQ'])+'\n')
for lab, d in sets:
    for sd in range(1, 21):
        p = os.path.join(d, 'region_s%d.tsv' % sd)
        if not os.path.exists(p): continue
        rows = collections.defaultdict(list)
        with open(p, encoding='utf-8') as fh:
            h = fh.readline().rstrip('\n').split('\t'); ix = {k:i for i,k in enumerate(h)}
            for line in fh:
                a = line.rstrip('\n').split('\t')
                if len(a) < len(h): continue
                rows[a[ix['region']]].append((int(a[ix['quarter']]), int(a[ix['year']]), a[ix['nation']], a[ix['tier']], fl(a[ix['populationK']]), fl(a[ix['earnedPerMonth']]),
                                              fl(a[ix['factoryOutputRefValuePerMonth']]), fl(a[ix['extractionOutputRefValuePerMonth']]), fl(a[ix['employmentShare']])))
        for reg, rr in rows.items():
            z = [r for r in rr if r[4] >= 1 and math.isfinite(r[5]) and r[5] / r[4] < TH]
            if not z: continue
            best = cur = 0; bs = be = None; cs = None; prevq = None
            for r in rr:
                isz = r[4] >= 1 and math.isfinite(r[5]) and r[5] / r[4] < TH
                if isz:
                    if cur == 0: cs = r[1]
                    cur += 1
                    if cur > best: best = cur; bs = cs; be = r[1]
                else: cur = 0
            pz = sum(1 for r in z if r[6] == 0 and r[7] == 0) / len(z)
            em = sorted(r[8] for r in z)[len(z)//2]
            pk = 0; mn = float('inf'); b4 = 0
            for r in rr:
                if not math.isfinite(r[4]): continue
                if r[4] > pk: pk = r[4]
                if pk > 0: mn = min(mn, r[4] / pk)
                if pk >= 1 and r[4] < pk / 4: b4 = 1
            o.write('\t'.join(str(v) for v in [lab, sd, reg, z[0][3], z[0][2], len(z), z[0][1], best, bs, be, format(pz,'.3f'), format(em,'.3f'), format(pk,'.4g'), format(rr[-1][4],'.4g'), format(mn,'.3f'), b4, len(rr)])+'\n')
o.close(); print('書いた', out)
