"""(639) 線 fsc：支出の引き上げの先（社会保障・年金・教育）に買える物があるか。読むだけ。
入力＝計器つき10種の生の1フォルダ（fiscal_s・fsx_s）。使い方：python an_fsc_social.py <ins フォルダ> [国名,...]
出す物：国ごとに、国庫÷線が 2 以上の月（その四半期の fsx の比）での 社会保障・年金・教育 の 請求の合計 と 台帳（払い）の合計 と 払われた割合（種の中央値）・請求が正の月の割合。"""
import csv, sys, glob, re, os, io, statistics as st
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', newline=None)
csv.field_size_limit(1 << 30)
d = sys.argv[1]
NAT = ['フランス', 'オーストリア', '日本', 'イギリス', 'アメリカ', 'プロイセン', 'ロシア', 'オスマン', '清']
if len(sys.argv) > 2: NAT = sys.argv[2].split(',')
def rows(f):
    with open(f, 'rb') as fh:
        rd = csv.reader(fh.read().decode('utf-8').splitlines(), delimiter='\t'); h = next(rd)
        ix = {n: i for i, n in enumerate(h)}
        for r in rd:
            if len(r) == len(h): yield ix, r
def fl(x):
    try: return float(x)
    except: return float('nan')
def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float('nan')
ITEMS = [('社会保障', 'claimWelfarePerMonth', 'ledgerWelfarePerMonth'), ('年金', 'claimPensionPerMonth', 'ledgerPensionPerMonth'),
         ('教育', 'claimLastMonthEducationPerMonth', 'ledgerEducationPerMonth'), ('高等教育', 'claimHigherEduPerMonth', 'ledgerEducationPerMonth')]
res = {}
for ff in sorted(glob.glob(os.path.join(d, 'fiscal_s*.tsv')), key=lambda s: int(re.search(r'_s(\d+)', s).group(1))):
    sd = int(re.search(r'_s(\d+)', ff).group(1))
    tl = {}
    for ix, r in rows(os.path.join(d, f'fsx_s{sd}.tsv')):
        if not r[ix['time']].startswith('q'): continue
        n = r[ix['nation']]
        if n not in NAT: continue
        t, l = fl(r[ix['treasuryEnd']]), fl(r[ix['lineEnd']])
        if l == l and l > 0 and t == t: tl[(n, int(r[ix['year']]), r[ix['time']])] = t / l
    acc = {}
    for ix, r in rows(ff):
        n = r[ix['nation']]
        if n not in NAT: continue
        mi = int(r[ix['monthIndex']]); y = int(r[ix['year']])
        # monthIndex から四半期（3か月）。fsx の time は q<n>・fiscal の monthIndex は月の通し番号（助走の後を 0 起点とみなせない場合は割り当てない）
        t = tl.get((n, y, 'q%d' % ((mi - 1) // 3)))
        if t is None or t < 2: continue
        for nm, cc, lc in ITEMS:
            c, p = fl(r[ix[cc]]), fl(r[ix[lc]])
            a = acc.setdefault((n, nm), [0, 0, 0.0, 0.0])
            a[0] += 1
            if c == c and c > 0: a[1] += 1
            if c == c: a[2] += max(0.0, c)
            if p == p: a[3] += max(0.0, -p) if nm != '教育' and nm != '高等教育' else max(0.0, -p)
    for k, a in acc.items(): res.setdefault(k, []).append(a)
print('# 入力', d, '（国庫÷線≥2 の月）')
print('国\t項目\t月数(種の中央値)\t請求が正の月の割合\t払い÷請求（種の中央値）')
for n in NAT:
    for nm, _, _ in ITEMS:
        L = res.get((n, nm))
        if not L: continue
        m = med([a[0] for a in L])
        if not m or m < 12: continue
        print(f'{n}\t{nm}\t{m:.0f}\t{med([a[1] / a[0] for a in L]):.2f}\t{med([(a[3] / a[2]) if a[2] > 0 else float("nan") for a in L]):.2f}')
