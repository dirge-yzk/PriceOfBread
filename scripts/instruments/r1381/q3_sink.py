# (1381) 読む線：sellr_s の laborWeight（＝(値段−投入費)×前の需要の寄り）のうち、その州でほとんど作られていない財（lastProduced < 0.02×max(lastDemand,1)）に乗った割合＝〈労働の重みの吸い込み〉。労働財（労働力・農業労働力・高度労働力）は除く（fraction=1 の別枠）。
# 使い方 python q3_sink.py <ins> <種> <出力> [州,州,...|ALLHOME]  ・州を省くと英の本国 5 州
import sys, collections
d, sd, out = sys.argv[1], int(sys.argv[2]), sys.argv[3]
regs = (sys.argv[4] if len(sys.argv) > 4 else 'バーミンガム,ロンドン,マンチェスター,グラスゴー,ダブリン').split(',')
LAB = ('労働力', '農業労働力', '高度労働力')
f = lambda s: float(s) if s not in ('', 'NaN') else 0.0
T = collections.defaultdict(float); S = collections.defaultdict(float); G = collections.defaultdict(lambda: collections.defaultdict(float)); seen = set()
with open('%s/sellr_s%d.tsv' % (d, sd), encoding='utf-8') as fh:
    h = fh.readline().rstrip('\n').split('\t'); I = {k: i for i, k in enumerate(h)}
    for l in fh:
        c = l.rstrip('\n').split('\t'); r = c[I['region']]
        if r not in regs: continue
        g = c[I['good']]
        if g in LAB: continue
        key = (int(c[I['year']]), r, g)
        if key in seen: continue
        seen.add(key)
        lw = max(0.0, f(c[I['laborWeight']])); k = (key[0], r); T[k] += lw
        if f(c[I['lastProduced']]) < 0.02 * max(f(c[I['lastDemand']]), 1.0): S[k] += lw; G[k][g] += lw
with open(out, 'w', encoding='utf-8', newline='\n') as fo:
    fo.write('year\tregion\tlwTotal\tlwSink\tsinkShare\ttopSink\n')
    for k in sorted(T, key=lambda t: (t[1], t[0])):
        top = sorted(G[k].items(), key=lambda t: -t[1])[:3]
        fo.write('%d\t%s\t%.0f\t%.0f\t%.3f\t%s\n' % (k[0], k[1], T[k], S[k], S[k] / T[k] if T[k] > 0 else 0, ','.join('%s:%.0f' % t for t in top)))
