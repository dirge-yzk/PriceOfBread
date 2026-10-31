# (702) 合流：〈壊す〉③ 片方向の錠（最大の段に張り付く国×四半期の割合が、ヌル腕の最大の 2 倍以上の列）の読み手（読むだけ）。
# 定義は igi4_667/lock.py と gaud_682/null_raw675.py の写し（scalars_s の法の 9 列）。ハードコードのパスを引数へ替えただけ。
# 使い方：python -X utf8 -I lock3.py <腕のラベル>=<ins>[,種の起点] ... --arm <腕のラベル> --refs <ラベル,ラベル,...>   （複数の --arm/--refs の組は ; で区切る：--pairs "M:m694,N1,N2;S11:A11"）
import csv, collections, sys
COLS = ['educationLaw','suffrage','censorship','bureaucracy','socialSecurityLawCode','antitrustLawCode','womenRightsCode','childLaborLegal','workHoursValue']
def run(d, S):
    cnt = collections.Counter(); tot = 0
    for s in S:
        f = open(f'{d}/scalars_s{s}.tsv', encoding='utf-8'); r = csv.reader(f, delimiter='\t'); h = next(r)
        idx = {c: h.index(c) for c in COLS}
        rows = [x for x in r]
        mx = {c: max(float(x[idx[c]]) for x in rows) for c in COLS}
        for x in rows:
            tot += 1
            for c in COLS:
                if float(x[idx[c]]) == mx[c] and mx[c] > min(float(y[idx[c]]) for y in rows[:1]): cnt[c] += 1
        f.close()
    return cnt, tot
args = sys.argv[1:]; pairs = ''
if '--pairs' in args:
    i = args.index('--pairs'); pairs = args[i + 1]; args = args[:i] + args[i + 2:]
res = {}
for a in args:
    lab, rest = a.split('=', 1); d, _, sb = rest.partition(','); sb = int(sb or 1)
    res[lab] = run(d, range(sb, sb + 10))
for lab, (c, t) in res.items(): print(lab, t, ' '.join(f'{k[:8]}={c[k]/t:.4f}' for k in COLS))
for p in pairs.split(';'):
    if not p: continue
    arm, refs = p.split(':'); refs = refs.split(',')
    worst = (0, '')
    for k in COLS:
        mxref = max(res[r][0][k] / res[r][1] for r in refs)
        v = res[arm][0][k] / res[arm][1]
        worst = max(worst, (v / mxref if mxref > 0 else 0, k))
    print(arm, '対', ','.join(refs), '最大の段に張り付く割合 腕÷基準の最大（列ごとの最悪）=%.2f（%s）（2 以上で該当）' % worst)
