# (1285) 問い2：割譲の sameCore（Diplomacy.cs:2955 の ""=="" ）に当たり得た講和を数える＝勝者と敗者がその年に空の札の区間にいた組（年で突き合わせ＝近似）
import sys, csv
raw, sb, ns, eps = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
E = {}
for r in csv.DictReader(open(eps, encoding='utf-8'), delimiter='\t'):
    E.setdefault((int(r['seed']), r['nat']), []).append((int(r['year0']), int(r['lastyear'])))
def emp(s, n, y): return any(a <= y <= b for a, b in E.get((s, n), []))
both = one = tot = 0; ex = []
for s in range(sb, sb + ns):
    for r in csv.DictReader(open(f"{raw}/cessp_s{s}.tsv", encoding='utf-8'), delimiter='\t'):
        y = int(r['year']); tot += 1; a, b = emp(s, r['winner'], y), emp(s, r['loser'], y)
        if a and b: both += 1; ex.append((s, y, r['winner'], r['loser'], r['loserRegionCount']))
        elif a or b: one += 1
print(f"{raw} 講和 {tot}・勝者と敗者の両方が空の区間の年 {both}・片方だけ {one}")
for e in ex[:10]: print('  ', *e)
