# (1423) 同じ盤面の新÷旧（記録）：armsfill_s の国ごとの 1880 年以後の戦力の列（militaryIndex＝p.Military・landPowerIndex）と兵数の平均の比。
import sys, collections
def agg(d):
    s = collections.defaultdict(lambda: [0.0, 0.0, 0.0, 0])
    for k in range(1, 11):
        rows = open(f'{d}/armsfill_s{k}.tsv', encoding='utf-8').read().splitlines()
        h = rows[0].split('\t'); ix = {c: i for i, c in enumerate(h)}
        mi = len(h) - 1 - h[::-1].index('landPowerIndex') - 1   # 戦力の列は landPowerIndex の 1 つ前（計器の並び＝sp.Military, sp.LandPower）
        for r in rows[1:]:
            c = r.split('\t')
            if int(c[ix['year']]) < 1880: continue
            a = s[c[ix['nation']]]; a[0] += float(c[mi]); a[1] += float(c[ix['landPowerIndex']]); a[2] += float(c[ix['soldierK']]); a[3] += 1
    return s, h[mi]
new, col = agg(sys.argv[1]); old, _ = agg(sys.argv[2])
print(f'# 列 {col}・landPowerIndex・soldierK の 1880 年以後の平均（10 種）の新÷旧')
for n in sorted(new):
    a, b = new[n], old[n]
    print(f'{n}\tmilitary {a[0]/b[0]:.4f}\tland {a[1]/b[1]:.4f}\tsoldierK {a[2]/b[2]:.4f}')
