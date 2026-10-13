"""★(314) 注（反証役 e6〔v〕・判定と出力は直していない）：①L1・L2 の〈合格〉は種の中央の比較だけ＝事前登録の種ごとの条件（L1 ≧8/10・L2 ≧6/10）は判定に入っていない（種ごとの数は表で読む）。②不履行の窓〈Q1〜Q160〉は四半期 160（Q161）まで読む＝1 つずれ（直すなら 159 まで）。
(310) 段6 財政と金利：事前登録 measurements/e6_20261013_prereg.txt の読み手（読むだけ・挙動は変えない）。
  PYTHONUTF8=1 python an_e6.py <出力.md> A=<ins> B=<ins>
入力：INS=1 で回した <出力>/ins の nation_s<種>.tsv（全61国が入る＝NATIONS に挙げなくてよい）。
出す物：
 ・L1 英＞仏の歳入÷年GDP（窓 Q5〜Q40＝q4..39・種ごとに 窓内の歳入の和÷GDP の和 を出して種の中央）・種ごとに英＞仏だった種の数
 ・L2 普＞波の歳入÷年GDP（同じ窓・波が窓の一部で消える種は残る四半期だけ）・種ごとの数
 ・L3 国債の金利（bondRateMeanPerYear・窓 Q5〜Q40 の平均を種の中央）で、開幕に国債のある18国のうち蘭が最低か（順位）
 ・記録：主要国の歳入÷年GDP（Q1・Q5〜Q40・Q41〜Q160）・国債の金利・債務÷年GDP（Q1・Q40 末）
限界の自己申告：
 ・nation_s の revenueBaseMeanPerMonth は四半期の月平均（純税収＋間接税・汚職漏れ控除後）。nominalGdpPerYear は名目の年GDP（World.Gdp は総産出＝付加価値でない）。
 ・bondRateMeanPerYear は四半期の月平均の国債金利（年率）。国債が無い国でも金利の項は出る（基礎＋政策金利の項）＝順位は開幕に国債のある18国だけで取る。
 ・国が窓の途中で消える（併合・分裂）種は残る四半期だけで出す。全く無い種は除く（NaN は判定不能で合格にしない）。
 ・種は腕の間で共通の種だけ。"""
import sys, os, re, glob, statistics, collections
sys.stdout.reconfigure(encoding='utf-8')
OUT = sys.argv[1]
ARMS = collections.OrderedDict(a.split('=', 1) for a in sys.argv[2:])
DEBTORS = ['イギリス', 'フランス', 'ネーデルラント', 'アメリカ', 'スペイン', 'オーストリア', 'ロシア', '教皇領', 'ジェノヴァ', 'デンマーク', 'スウェーデン', 'ポルトガル',
           '両シチリア', 'ヴェネツィア', 'バイエルン', 'ザクセン', 'ヴュルテンベルク', 'オスマン']
FOCUS = ['イギリス', 'フランス', 'ネーデルラント', 'ポルトガル', 'プロイセン', 'ポーランド', 'オーストリア', 'ロシア', 'スペイン', 'アメリカ', '清', '日本', 'オスマン']
WIN = [('Q1', 0, 0), ('Q5-40', 4, 39), ('Q41-160', 40, 159)]
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
def med(xs):
    xs = [x for x in xs if x == x]; return statistics.median(xs) if xs else float('nan')
def fm(x, nd=3): return '—' if x != x else f'{x:.{nd}f}'
def cell(xs, nd=3):
    ys = [x for x in xs if x == x]
    return '—' if not ys else f'{statistics.median(ys):.{nd}f} ({min(ys):.{nd}f}〜{max(ys):.{nd}f})'
def load(D):
    R = {}
    for p in glob.glob(os.path.join(D, 'nation_s*.tsv')):
        sd = int(re.search(r'nation_s(\d+)\.tsv$', p).group(1))
        d = collections.defaultdict(dict)
        with open(p, encoding='utf-8') as fh:
            h = fh.readline().rstrip('\n').split('\t'); ix = {x: i for i, x in enumerate(h)}
            for ln in fh:
                r = ln.rstrip('\n').split('\t'); q = int(r[ix['quarter']])
                if q > 160: continue
                d[r[ix['nation']]][q] = (fl(r[ix['revenueBaseMeanPerMonth']]), fl(r[ix['nominalGdpPerYear']]), fl(r[ix['bondRateMeanPerYear']]), fl(r[ix['govDebtStock']]), fl(r[ix['sovereignDefaultCumulativeCount']]))
        R[sd] = d
    return R
def rev_ratio(rows, a, b):
    qs = [rows[q] for q in range(a, b + 1) if q in rows]
    g = sum(x[1] for x in qs)
    return sum(x[0] * 12 for x in qs) / g if qs and g > 0 else float('nan')
def bond(rows, a, b):
    v = [rows[q][2] for q in range(a, b + 1) if q in rows and rows[q][2] == rows[q][2]]
    return sum(v) / len(v) if v else float('nan')
def debt_ratio(rows, q):
    x = rows.get(q)
    return x[3] / x[1] if x and x[1] > 0 else float('nan')
DATA = {a: load(d) for a, d in ARMS.items()}
common = sorted(set.intersection(*[set(v) for v in DATA.values()]))
lines = []; P = lines.append
P(f'# (310) 段6 読み手の出力（種 {len(common)}：{common}）\n')
verd = {}
for a, S in DATA.items():
    P(f'## 腕 {a}（{ARMS[a]}）\n')
    # L1
    uk = [rev_ratio(S[sd]['イギリス'], 4, 39) for sd in common]; fr = [rev_ratio(S[sd]['フランス'], 4, 39) for sd in common]
    n1 = sum(1 for x, y in zip(uk, fr) if x == x and y == y and x > y)
    l1 = med(uk) > med(fr)
    # L2
    pr = [rev_ratio(S[sd].get('プロイセン', {}), 4, 39) for sd in common]; po = [rev_ratio(S[sd].get('ポーランド', {}), 4, 39) for sd in common]
    n2 = sum(1 for x, y in zip(pr, po) if x == x and y == y and x > y); nvalid = sum(1 for x, y in zip(pr, po) if x == x and y == y)
    l2 = (med(pr) > med(po)) if med(po) == med(po) else False
    # L3
    br = {n: [bond(S[sd].get(n, {}), 4, 39) for sd in common] for n in DEBTORS}
    bm = {n: med(v) for n, v in br.items()}
    rank = sorted([n for n in DEBTORS if bm[n] == bm[n]], key=lambda n: bm[n])
    nl = sum(1 for i, sd in enumerate(common) if all(br['ネーデルラント'][i] <= br[n][i] for n in DEBTORS if br[n][i] == br[n][i]) and br['ネーデルラント'][i] == br['ネーデルラント'][i])
    l3 = bool(rank) and rank[0] == 'ネーデルラント'
    verd[a] = (l1, l2, l3)
    P(f'- L1 歳入÷年GDP（Q5-40）英 {cell(uk)} ＞ 仏 {cell(fr)} → {"合格" if l1 else "外れ"}（種ごとに英＞仏 {n1}/{len(common)}）')
    P(f'- L2 歳入÷年GDP（Q5-40）普 {cell(pr)} ＞ 波 {cell(po)} → {"合格" if l2 else "外れ"}（種ごとに普＞波 {n2}/{nvalid}・波が窓に残った種 {nvalid}）')
    P(f'- L3 国債の金利（Q5-40・年率）の順位（低い順・開幕に国債のある18国）：' + ' ＜ '.join(f'{n} {bm[n]*100:.2f}%' for n in rank[:8]) + f' …（蘭 {rank.index("ネーデルラント")+1 if "ネーデルラント" in rank else "—"} 位 → {"合格" if l3 else "外れ"}・種ごとに蘭が最低 {nl}/{len(common)}）\n')
    P('不履行（sovereignDefaultCumulativeCount>0）のある種の数（開幕に国債のある18国・窓＝Q1〜Q40／Q1〜Q160）：')
    P('、'.join(f'{n} {sum(1 for sd in common if (S[sd].get(n, {}).get(39, (0,0,0,0,0))[4] or 0) > 0)}/{sum(1 for sd in common if (S[sd].get(n, {}).get(160, (0,0,0,0,0))[4] or 0) > 0)}' for n in DEBTORS) + chr(10))
    P('| 国 | 歳入÷年GDP Q1 | Q5-40 | Q41-160 | 国債の金利 Q5-40 | Q41-160 | 債務÷年GDP Q1 | Q40 末 |'); P('|---|---|---|---|---|---|---|---|')
    for n in FOCUS:
        rr = [[rev_ratio(S[sd].get(n, {}), x, y) for sd in common] for _, x, y in WIN]
        bb = [[bond(S[sd].get(n, {}), x, y) for sd in common] for _, x, y in WIN[1:]]
        d0 = [debt_ratio(S[sd].get(n, {}), 0) for sd in common]; d40 = [debt_ratio(S[sd].get(n, {}), 39) for sd in common]
        P(f'| {n} | {cell(rr[0])} | {cell(rr[1])} | {cell(rr[2])} | {cell(bb[0])} | {cell(bb[1])} | {cell(d0)} | {cell(d40)} |')
    P('')
if len(ARMS) == 2:
    (a0, a1) = list(ARMS)
    P(f'## {a1} − {a0}（国ごとの歳入÷年GDP Q5-40・種の中央の差）\n')
    for n in FOCUS:
        x = med([rev_ratio(DATA[a0][sd].get(n, {}), 4, 39) for sd in common]); y = med([rev_ratio(DATA[a1][sd].get(n, {}), 4, 39) for sd in common])
        P(f'- {n}: {fm(x)} → {fm(y)}')
P('\n判定：' + '・'.join(f'{a}: L1 {"○" if v[0] else "×"} L2 {"○" if v[1] else "×"} L3 {"○" if v[2] else "×"}' for a, v in verd.items()))
open(OUT, 'wb').write(('\n'.join(lines) + '\n').encode('utf-8'))
print('\n'.join(lines))
