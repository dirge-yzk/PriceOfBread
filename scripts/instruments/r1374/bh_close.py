# (1374) 読む線：popclose_s の閉じた勘定で 1 州の人口の変化を期間ごとに分ける（出生・通常の死・飢えの死・暴力の死・その他の死・州間・国境越え・専門職・駐屯・残り）。
# 使い方 python bh_close.py <ins> <種> <州> [期間の年数]
import sys, collections, math
d, sd, reg = sys.argv[1], int(sys.argv[2]), sys.argv[3]
step = int(sys.argv[4]) if len(sys.argv) > 4 else 10
def f(x):
    try: v = float(x); return v if math.isfinite(v) else 0.0
    except ValueError: return 0.0
K = ['deltaPopulationK','birthsK','deathsNormalK','deathsFamineK','killedViolentK','killedOtherK','migrationInK','migrationOutK','crossBorderInK','crossBorderOutK','specialistInK','specialistOutK','garrisonInK','garrisonOutK','colonyMergeInK','residualK']
S = collections.defaultdict(collections.Counter); P0 = {}; P1 = {}; N = collections.Counter()
with open('%s/popclose_s%d.tsv' % (d, sd), encoding='utf-8') as fh:
    h = fh.readline().rstrip('\n').split('\t')
    for line in fh:
        r = dict(zip(h, line.rstrip('\n').split('\t')))
        if r['region'] != reg: continue
        b = (int(r['year']) - 1783) // step
        if b not in P0: P0[b] = f(r['prevPopulationK'])
        P1[b] = f(r['populationK']); N[b] += 1
        for k in K: S[b][k] += f(r[k])
print('期間\t始め\t終わり\t変化\t出生\t通常の死\t飢えの死\t暴力の死\tその他の死\t州間の純\t国境越えの純\t専門職の純\t駐屯の純\t残り\t粗出生率‰\t粗死亡率‰')
T = collections.Counter()
for b in sorted(S):
    s = S[b]; mid = (P0[b] + P1[b]) / 2; yrs = N[b] / 4.0
    for k in K: T[k] += s[k]
    print('%d-%d\t%.0f\t%.0f\t%.0f\t%.0f\t%.0f\t%.0f\t%.0f\t%.0f\t%.0f\t%.0f\t%.0f\t%.0f\t%.0f\t%.1f\t%.1f' % (1783 + step * b, min(1950, 1782 + step * (b + 1)), P0[b], P1[b], s['deltaPopulationK'], s['birthsK'], -s['deathsNormalK'], -s['deathsFamineK'], -s['killedViolentK'], -s['killedOtherK'], s['migrationInK'] - s['migrationOutK'], s['crossBorderInK'] - s['crossBorderOutK'], s['specialistInK'] - s['specialistOutK'], s['garrisonInK'] - s['garrisonOutK'], s['residualK'], 1000 * s['birthsK'] / mid / yrs, 1000 * (s['deathsNormalK'] + s['deathsFamineK'] + s['killedOtherK']) / mid / yrs))
s = T
print('合計\t\t\t%.0f\t%.0f\t%.0f\t%.0f\t%.0f\t%.0f\t%.0f\t%.0f\t%.0f\t%.0f\t%.0f' % (s['deltaPopulationK'], s['birthsK'], -s['deathsNormalK'], -s['deathsFamineK'], -s['killedViolentK'], -s['killedOtherK'], s['migrationInK'] - s['migrationOutK'], s['crossBorderInK'] - s['crossBorderOutK'], s['specialistInK'] - s['specialistOutK'], s['garrisonInK'] - s['garrisonOutK'], s['residualK']))
