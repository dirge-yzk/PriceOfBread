# (1058) 問い2 の局所の大きさ（読むだけ・近似）：首都州（sellr の国ごとの最初の州〈推〉）の調達の財の行で prevExportAll÷lastDemand の分布
# prevExportAll は清算の輸出も含む＝調達の上界。値段の式（MarketFlow.cs:248）が足す LastExport の局所の重みの目安。
import sys, os, math
from collections import defaultdict
MAJ = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'アメリカ', '清']
PROC = {'石炭', '原油', 'キニーネ', '缶詰', '帆船', '汽船'}
OUT = sys.argv[1]; ARMS = [a.split('=', 1) for a in sys.argv[2:]]
out = []
for nm, d in ARMS:
    bins = defaultdict(lambda: [0, 0, 0, 0]); ex = []
    for s in range(1, 11):
        p = os.path.join(d, 'sellr_s%d.tsv' % s)
        if not os.path.exists(p): continue
        cap = {}; seen = set()
        with open(p, encoding='utf-8') as f:
            ix = {k: i for i, k in enumerate(f.readline().rstrip('\n').split('\t'))}
            for ln in f:
                v = ln.rstrip('\n').split('\t')
                y = v[ix['year']]; nat = v[ix['ownerNation']]; reg = v[ix['region']]; g = v[ix['good']]
                if (y, nat) not in cap: cap[(y, nat)] = reg
                if nat not in MAJ or reg != cap[(y, nat)] or g not in PROC or (y, reg, g) in seen: continue
                seen.add((y, reg, g))
                e = float(v[ix['prevExportAll']]); dm = float(v[ix['lastDemand']])
                b = bins[g]; b[0] += 1
                if dm > 0 and e / dm > 0.1: b[1] += 1
                if dm > 0 and e / dm > 0.5: b[2] += 1
                if e > 0: b[3] += 1
                if dm > 0 and e / dm > 0.5 and len(ex) < 8: ex.append('%s 種%d %s %s %s 輸出 %.4g 需要 %.4g' % (y, s, nat, reg, g, e, dm))
    out.append('== %s %s' % (nm, d))
    for g, b in sorted(bins.items()):
        out.append('  %s 行 %d 輸出＞0 %d 輸出÷需要＞0.1 %d ＞0.5 %d' % (g, b[0], b[3], b[1], b[2]))
    out += ['  例 ' + x for x in ex]
with open(OUT, 'w', encoding='utf-8') as f:
    f.write('# (1058) q2local.py %s\n' % ' '.join(sys.argv[2:]))
    f.write('\n'.join(out) + '\n')
print(len(out))
