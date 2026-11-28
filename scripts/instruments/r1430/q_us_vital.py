"""(1430) アメリカの国の行（pop_s の scope N）の 10 年ごとの 出生÷人口・平常の死÷人口・回復期の月の数。読むだけ。python -I -X utf8 q_us_vital.py <dir> <種> [国]"""
import sys, os, collections
d, s = sys.argv[1], sys.argv[2]; NAT = sys.argv[3] if len(sys.argv) > 3 else 'アメリカ'
h = None; acc = collections.OrderedDict()
for l in open(os.path.join(d, f'pop_s{s}.tsv'), encoding='utf-8'):
    v = l.rstrip('\n').split('\t')
    if h is None: h = {k: i for i, k in enumerate(v)}; continue
    if v[h['scope']] != 'N' or v[h['nation']] != NAT: continue
    k = int(float(v[h['year']])) // 10 * 10
    a = acc.setdefault(k, [0.0, 0.0, 0.0, 0.0, 0.0, 0])
    a[0] += float(v[h['birthsK']]); a[1] += float(v[h['deathsNormalK']]); a[2] += float(v[h['popMeanK']]); a[3] += float(v[h['recoveringMonths']]); a[4] += float(v[h['monthsCount']]); a[5] += 1
print('年代\t人口の平均\t出生(年‰)\t平常の死(年‰)\t純(年‰)\t回復期の月/月')
for k, a in acc.items():
    p = a[2] / a[5]; yrs = a[5] / 4
    print(f'{k}\t{p:.0f}\t{a[0]/yrs/p*1000:.1f}\t{a[1]/yrs/p*1000:.1f}\t{(a[0]-a[1])/yrs/p*1000:.1f}\t{a[3]:.0f}/{a[4]:.0f}')
