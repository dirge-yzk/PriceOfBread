# (1189) 問い3 の続き：国ごとに、主食の不足の行（foodMetShare＜0.9・人口 1 万以上）・人口の重みの不足 Σ人口×max(0,0.9−f)・飢えの死（pop_s scope N）を対の腕で並べる。
#  母集合：nation_s の全ての国×四半期（人口 1 万以上・非有限を除く）。飢えの死は pop_s の scope N の全ての行（計器の国は毎四半期・他の国は年 1 行）。
#  使い方: python -X utf8 -I h1189b.py <名A> <insA> <名B> <insB> <種の起点> <種の終わり> [上位の数]
import csv, math, os, sys, collections
sys.stdout.reconfigure(newline='\n')
def qn(v): return int(str(v).lstrip('q'))
def fl(v):
    try: return float(v)
    except (ValueError, TypeError): return float('nan')
def read(B, sb, se):
    rows = collections.Counter(); dfc = collections.Counter(); fam = collections.Counter(); warq = collections.Counter(); popq = collections.Counter()
    for s in range(sb, se + 1):
        for r in csv.DictReader(open(os.path.join(B, f'nation_s{s}.tsv'), encoding='utf-8'), delimiter='\t'):
            fm, pk, aw = fl(r.get('foodMetShare')), fl(r.get('populationK')), fl(r.get('atWarMonths'))
            if not (math.isfinite(fm) and pk >= 10): continue
            n = r['nation']; rows[n] += 1 if fm < 0.9 else 0; dfc[n] += pk * max(0.0, 0.9 - fm); warq[n] += 1 if aw > 0 else 0; popq[n] += pk
        for r in csv.DictReader(open(os.path.join(B, f'pop_s{s}.tsv'), encoding='utf-8'), delimiter='\t'):
            if r['scope'] != 'N': continue
            d = fl(r['deathsFamineK'])
            if d == d: fam[r['nation']] += d
    return rows, dfc, fam, warq, popq
nA, A, nB, B, sb, se = sys.argv[1:7]; top = int(sys.argv[7]) if len(sys.argv) > 7 else 12
a, b = read(A, int(sb), int(se)), read(B, int(sb), int(se))
nat = set(a[2]) | set(b[2])
tot = [sum(x.values()) for x in a], [sum(x.values()) for x in b]
print(f'# {nA}={A} 対 {nB}={B}・種 {sb}..{se}')
print(f'全体\t不足の行 {tot[0][0]}/{tot[1][0]}\t人口×不足 {tot[0][1]:.0f}/{tot[1][1]:.0f}\t飢えの死K {tot[0][2]:.0f}/{tot[1][2]:.0f}\t戦時の国×四半期 {tot[0][3]}/{tot[1][3]}')
print('国\t飢えの死K A/B（差）\t不足の行 A/B\t人口×不足 A/B\t戦時の国×四半期 A/B\t人口の和の比 A÷B')
for n in sorted(nat, key=lambda n: -abs(a[2][n] - b[2][n]))[:top]:
    print(f'{n}\t{a[2][n]:.0f}/{b[2][n]:.0f}（{a[2][n] - b[2][n]:+.0f}）\t{a[0][n]}/{b[0][n]}\t{a[1][n]:.0f}/{b[1][n]:.0f}\t{a[3][n]}/{b[3][n]}\t{a[4][n] / max(b[4][n], 1e-9):.3f}')
# 飢えの死の集中：上位 5 国の取り分
for nm, x in ((nA, a), (nB, b)):
    s = sorted(x[2].values(), reverse=True); t = sum(s)
    print(f'{nm} 飢えの死の上位 5 国の取り分 {sum(s[:5]) / max(t, 1e-9):.3f}・不足の行の上位 5 国の取り分 {sum(sorted(x[0].values(), reverse=True)[:5]) / max(sum(x[0].values()), 1):.3f}')
