# (1149) 案 C の変わる範囲の紙の予言：国×月のうち〈自国は知らない・世界のどこかは知る〉の月（railspend_s の monthsPre を、世界で最初に discEnd＝1 が出た四半期で分ける）。
#   python -X utf8 -I prewin.py <ins> <種の始め> <種の終わり>
import sys, os
ins, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
A = B = M = 0; amtA = amtB = 0.0
for s in range(s0, s1 + 1):
    p = os.path.join(ins, f'railspend_s{s}.tsv')
    rows = []
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in f:
            c = ln.rstrip('\n').split('\t'); rows.append(c)
    fq = min((int(c[ix['q']]) for c in rows if c[ix['discEnd']] == '1'), default=10**9)
    for c in rows:
        q = int(c[ix['q']]); mp = int(c[ix['monthsPre']]); mo = int(c[ix['months']])
        rail = float(c[ix['preBuildRailAmtGold']]) + float(c[ix['preMaintRailAmtGold']])
        M += mo
        if q < fq: A += mp; amtA += rail
        else: B += mp; amtB += rail
print(f'== {ins} 種 {s0}〜{s1}：国×月 {M}・自国も世界も知らない月 {A}（{A / M:.1%}）・自国は知らず世界は知る月 {B}（{B / M:.1%}）')
print(f'   発見の前の線路の払い（金）：世界も知らない側 {amtA:.4g}・世界は知る側 {amtB:.4g}（後者の割合 {amtB / (amtA + amtB):.1%}）')
