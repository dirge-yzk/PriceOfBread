# 問い1：ある国の州が四半期ごとにどこへ移ったかを region_s から読む（読むだけ）
# 使い方: python q1_owner_moves.py <生のフォルダ> <種> <国名> <出力>
import sys, os, csv, collections
d, seed, nat, outp = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4]
p = os.path.join(d, f"region_s{seed}.tsv")
own = collections.defaultdict(dict)   # q -> region -> (nation, popK, tier, active)
with open(p, encoding='utf-8') as f:
    r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
    for row in r:
        q = int(row[ix['quarter']])
        own[q][row[ix['region']]] = (row[ix['nation']], float(row[ix['populationK']]), row[ix['tier']], row[ix['activeFlag']])
qs = sorted(own)
seedname = os.path.basename(p)
with open(outp, 'w', encoding='utf-8') as o:
    o.write(f"# {seedname} 国={nat}：その国の州が次の四半期に別の国へ移った行（州・人口千人・tier・行き先）\n")
    o.write("quarter\tyear_q\tregion\ttier\tpopK\tto\n")
    for a, b in zip(qs, qs[1:]):
        for reg, (n, pk, t, act) in own[a].items():
            if n != nat: continue
            nb = own[b].get(reg)
            if nb is None: o.write(f"{b}\t\t{reg}\t{t}\t{pk:.1f}\t(行が消えた)\n"); continue
            if nb[0] != nat: o.write(f"{b}\t\t{reg}\t{t}\t{pk:.1f}\t{nb[0]}\n")
        for reg, nb in own[b].items():
            if nb[0] == nat and own[a].get(reg, ('',))[0] != nat:
                o.write(f"{b}\t\t{reg}\t{nb[2]}\t{nb[1]:.1f}\t(入った・元={own[a].get(reg,('?',))[0]})\n")
