# (1009) c93wn：ラベル y・q%4==3 の格で非 Minor に見えた侵略の標的が、1 つ前のラベル（y−1・q%4==3）の行で何の格だったかを数える（読むだけ）。
# 使い方：python -X utf8 -I wr1009_rankcheck.py <keep のルート> <出力のフォルダ>
# 読み：格（Diplomacy.RankOf・Diplomacy.cs:593）は外交の段の中で 1 年ごとに求め直す（Diplomacy.cs:950・RankUpdateIntervalQuarters 4）。
#   1 つ前の行で Minor なら、その侵略は求め直しの前の格（Minor）で評価された（CanInvade の World.cs:2189 と矛盾しない）と読める。
import sys, os, collections
ROOT, OUT = sys.argv[1], sys.argv[2]
S = {'base_ins': range(1, 11), 'n1': range(1, 11), 'n2': range(1, 11), 'a11': range(11, 21)}
out = []
for b, ss in S.items():
    c = collections.Counter()
    for s in ss:
        rank = {}
        L = open(f'{ROOT}/{b}/score_s{s}.tsv', 'rb').read().decode('utf-8').splitlines()
        h = L[0].split('\t'); ix = {k: i for i, k in enumerate(h)}
        for l in L[1:]:
            x = l.split('\t'); q = int(x[0])
            if q % 4 == 3 or q == 0: rank[(int(x[1]), x[2])] = x[ix['rankInForce']]
        for l in open(f'{ROOT}/{b}/invade_s{s}.tsv', 'rb').read().decode('utf-8').splitlines()[1:]:
            x = l.split('\t'); y = int(x[0]); t = x[2]
            if rank.get((y, t)) in ('Middle', 'Secondary', 'Great'): c[str(rank.get((y - 1, t)))] += 1
    out.append(f'{b}: ラベル y で非 Minor の標的 {sum(c.values())} 件・ラベル y−1 の格 ' + '・'.join(f'{k} {v}' for k, v in c.most_common()))
os.makedirs(OUT, exist_ok=True)
open(os.path.join(OUT, 'wr1009_rankcheck.txt'), 'wb').write(('\n'.join(out) + '\n').encode('utf-8'))
print('\n'.join(out))
