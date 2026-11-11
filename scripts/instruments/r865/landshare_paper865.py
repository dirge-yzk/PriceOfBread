# (865) c71ds 紙の数（読むだけ）：開幕の地代の受け手の割合（今）と、B-2 の候補の割合を当てたときの動き。
# 入力：D:/PoB_work/keep_852/base_ins の cornint_s1..10・nation_s1..10・subst_s1..10・cpop_s1..10（m852 の計器つき 10 種・木 1271053f）
# 出力：D:/PoB_work/r865_work/landshare_paper865.txt
# 限界（自己申告）：
#  - 四半期の末の月の値（q0＝1783 の最初の四半期）。
#  - 輸出の地代は cornint の近似（PaySales の近似＝輸出量を RGO の産出比で割り慣習の取り分を掛けた値）。
#  - rentDomUnattr（所有する RGO の産出が 0 の POP の地代）は国内の地代に含めたまま（帰属だけが無い）。
#  - 不在地主（Colonial.cs:160〜161）で植民地格の州の輸出作物の地代は首都の投資家（近代の層＝bloc1）に数わる。
#  - 層は CnBloc（InstrumentsCornint.cs:24）＝0 閉鎖（貴族＋聖職）・1 近代の上層（資本家ほか）・2 大衆（農民ほか）。
#  - 候補の割合は AI の記憶（要出典確認）。面積の割合を地代の割合と見なす（同じ率の仮定）。
#  - 候補の割合は本国格（Core）の州の地代にだけ当て、植民地格（Colonial）の州の地代は今のまま（subst の tier で分ける）。
import csv, io, statistics as st

BASE = 'D:/PoB_work/keep_852/base_ins'
OUT = 'D:/PoB_work/r865_work/landshare_paper865.txt'
SEEDS = range(1, 11)
Q = '0'

# 候補（AI の記憶・要出典確認）：N 貴族・C 王領と国有地・K 教会・P 農民の自作・B 市民の地主。和 1。
CAND = {
    'フランス':   dict(N=0.22, C=0.03, K=0.08, P=0.37, B=0.30, conf='中（二次資料で幅が合う・表は未照合）'),
    'イギリス':   dict(N=0.72, C=0.02, K=0.05, P=0.17, B=0.04, conf='中（書名は確か・表は未照合）'),
    'ロシア':     dict(N=0.53, C=0.47, K=0.00, P=0.00, B=0.00, conf='中（人口の割合・面積でない・表は未照合）'),
    'スウェーデン': dict(N=0.33, C=0.20, K=0.00, P=0.47, B=0.00, conf='中（戸の単位・表は未照合）'),
    '日本':       dict(N=0.85, C=0.14, K=0.01, P=0.00, B=0.00, conf='中（石高の配り・受け手の定義）'),
    'プロイセン': dict(N=0.60, C=0.30, K=0.02, P=0.08, B=0.00, conf='弱（数の出所が弱い）'),
    'スペイン':   dict(N=0.45, C=0.05, K=0.15, P=0.22, B=0.13, conf='弱'),
    'デンマーク': dict(N=0.80, C=0.10, K=0.02, P=0.08, B=0.00, conf='弱'),
    'ポーランド': dict(N=0.70, C=0.15, K=0.15, P=0.00, B=0.00, conf='弱'),
}


def read_tsv(path):
    raw = open(path, 'rb').read()
    txt = raw.decode('utf-8-sig')
    rows = list(csv.reader(io.StringIO(txt), delimiter='\t'))
    h = rows[0]
    return h, rows[1:]


def f(x):
    try:
        v = float(x)
    except ValueError:
        return 0.0
    return v if v == v else 0.0


per = {}  # nation -> list of dict per seed
for s in SEEDS:
    h, rows = read_tsv(f'{BASE}/cornint_s{s}.tsv')
    ix = {k: i for i, k in enumerate(h)}
    hn, rn = read_tsv(f'{BASE}/nation_s{s}.tsv')
    jn = {k: i for i, k in enumerate(hn)}
    gdp = {}
    for r in rn:
        if r[jn['quarter']] == Q:
            gdp[r[jn['nation']]] = (f(r[jn['nominalGdpPerYear']]), f(r[jn['revenueBaseMeanPerMonth']]), f(r[jn['directTaxNetMeanPerMonth']]))
    hs, rs = read_tsv(f'{BASE}/subst_s{s}.tsv')
    js = {k: i for i, k in enumerate(hs)}
    tier = {}
    for r in rs:
        if r[js['quarter']] != Q:
            continue
        n = r[js['nation']]
        t = r[js['tier']]
        rr = f(r[js['rentToOthersPerMonth']]) + f(r[js['rentToFarmersPerMonth']])
        a = tier.setdefault(n, [0.0, 0.0])
        a[0 if t == 'Core' else 1] += rr
    for r in rows:
        if r[ix['quarter']] != Q:
            continue
        n = r[ix['nation']]
        dom = [f(r[ix[f'rentDomPerMonth{b}']]) for b in range(3)]
        exp = [f(r[ix[f'rentExpPerMonth{b}']]) for b in range(3)]
        g = gdp.get(n, (0.0, 0.0, 0.0))
        t = tier.get(n, [0.0, 0.0])
        per.setdefault(n, []).append(dict(dom=dom, exp=exp, gdp=g[0], rev=g[1], dtax=g[2], core=t[0], col=t[1]))


def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float('nan')


lines = []
lines.append('# (865) 紙の数：開幕（m852・q0＝1783 の最初の四半期の末）の RGO の地代の受け手の層の割合（10 種の中央）')
lines.append('# 列：nation・dom＝国内の地代 /月・exp＝輸出の地代の近似 /月・s0 閉鎖（貴族＋聖職）・s1 近代の上層・s2 大衆（農民）＝(国内＋輸出) の割合・rent12/GDP＝年の地代÷名目GDP・colShare＝国内の地代のうち植民地格の州の割合（subst）')
lines.append('nation\tdom\texp\ts0\ts1\ts2\trent12overGdp\tcolShare\trevBaseOverRent')
summ = {}
for n, L in per.items():
    tot = [sum(d['dom']) + sum(d['exp']) for d in L]
    s = [[(d['dom'][b] + d['exp'][b]) / t if t > 0 else float('nan') for d, t in zip(L, tot)] for b in range(3)]
    r12 = [12 * t / d['gdp'] if d['gdp'] > 0 else float('nan') for d, t in zip(L, tot)]
    cs = [d['col'] / (d['core'] + d['col']) if d['core'] + d['col'] > 0 else float('nan') for d in L]
    rv = [d['rev'] / t if t > 0 else float('nan') for d, t in zip(L, tot)]
    row = dict(dom=med([sum(d['dom']) for d in L]), exp=med([sum(d['exp']) for d in L]), s0=med(s[0]), s1=med(s[1]), s2=med(s[2]), r12=med(r12), col=med(cs), rv=med(rv), n=len(L))
    summ[n] = row
for n in sorted(summ, key=lambda k: -summ[k]['dom']):
    r = summ[n]
    lines.append(f"{n}\t{r['dom']:.1f}\t{r['exp']:.1f}\t{r['s0']:.4f}\t{r['s1']:.4f}\t{r['s2']:.4f}\t{r['r12']:.4f}\t{r['col']:.4f}\t{r['rv']:.4f}")

lines.append('')
lines.append('# 候補（AI の記憶・要出典確認）を本国格の州の地代に当てたときの層の割合と動き（植民地格の州は今のまま）')
lines.append('# 写し：N＋K→s0（閉鎖）・B→s1（近代の上層＝資本家）・P→s2（大衆＝農民）・C→国庫（POP でない＝新しい受け手）')
lines.append('# TV＝Σ|新−今|/2（受け手の層の割合の動き・国庫を含む 4 枠）・toTreasury12overGdp＝国庫へ移る年の地代÷名目GDP・toTreasuryOverRevBase＝それ÷今の歳入の基数（月）・peasantGain12overGdp＝農民へ移る年の地代÷名目GDP')
lines.append('nation\tconf\tnow_s0\tnow_s1\tnow_s2\tnew_s0\tnew_s1\tnew_s2\tnew_state\tTV\ttoTreasury12overGdp\ttoTreasuryOverRevBase\tpeasantGain12overGdp')
for n, c in CAND.items():
    if n not in summ:
        lines.append(f'{n}\t(表に無い)')
        continue
    r = summ[n]
    core = 1.0 - (r['col'] if r['col'] == r['col'] else 0.0)
    # 今の割合のうち植民地格の州の分は層の割合のまま残す近似（植民地格の分は今の層の割合で按分と見なす）
    nb = [r['s0'] * (1 - core) + (c['N'] + c['K']) * core,
          r['s1'] * (1 - core) + c['B'] * core,
          r['s2'] * (1 - core) + c['P'] * core]
    ns_ = c['C'] * core
    now = [r['s0'], r['s1'], r['s2'], 0.0]
    new = nb + [ns_]
    tv = sum(abs(a - b) for a, b in zip(new, now)) / 2
    tt = ns_ * r['r12']
    tr = ns_ / r['rv'] if r['rv'] and r['rv'] == r['rv'] and r['rv'] > 0 else float('nan')
    pg = (nb[2] - r['s2']) * r['r12']
    lines.append(f"{n}\t{c['conf']}\t{r['s0']:.4f}\t{r['s1']:.4f}\t{r['s2']:.4f}\t{nb[0]:.4f}\t{nb[1]:.4f}\t{nb[2]:.4f}\t{ns_:.4f}\t{tv:.4f}\t{tt:.4f}\t{tr:.4f}\t{pg:.4f}")

out = '\n'.join(lines) + '\n'
data = out.encode('utf-8')
fo = open(OUT, 'wb')
fo.write(data)
fo.close()
