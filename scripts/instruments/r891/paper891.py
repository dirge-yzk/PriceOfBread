# (891) c75ds 紙の数（読むだけ）：盤面 m873 の開幕の地代の受け手の割合（今）と、(c) の水準の候補（幅の中点を和 1 へ割り直した値）を当てたときの動き。
# 元：scripts/instruments/r865/landshare_paper865.py（m852 の版）を写して、盤面・ヌル腕・幅と中点・B-2a と B-2b の分け・土地税の重なりを足した。
# 使い方：python -X utf8 -I paper891.py <出力.txt>
# 入力（読むだけ）：D:/PoB_work/keep_873/base_ins・n1・n2 の cornint_s・nation_s・subst_s（種 1〜10）と measurements/m865_20261105/landshare_paper865.txt（m852 の数）
# 限界（自己申告）：
#  ①四半期の末の月の値（q0＝1783 の最初の四半期の末）。q0〜q11（1783〜85 の 12 四半期）の平均も並べる。
#  ②輸出の地代は cornint の近似（InstrumentsCornint.cs:82〜:97）。
#  ③賦存の RGO（州の resources に載る財＝大農園の上書き Simulation.cs:843〜:846 の母集合）を外せない＝どの表にも RGO ごとの地代が無い＝動きは上限の見積り（(865) 反証役 ③ のまま）。
#    外した数は B-0 の計器 landshare_s（賦存と非賦存を分ける列）が出す。
#  ④層は CnBloc（InstrumentsCornint.cs:24）＝0 閉鎖（貴族＋聖職）・1 近代の上層・2 大衆。貴族→聖職は層の動き 0。
#  ⑤候補の割合は AI の記憶（要出典確認）。面積（または人口・戸・石高）の割合を地代の割合と見なす（同じ率の仮定）。
#  ⑥候補は本国格と統合格（BuildProvince の州）の地代に当て、植民地格の州は今のまま（subst の tier で分ける・subst の tier は Core と Colonial の 2 値＝統合格は Core 側に数わる見込み〔確かめていない〕）。
#  ⑦土地税の増分は上限（免税の度合を 0.3＝PrivilegeTaxDiscount の最大で置く・Privileges.cs:41〜:45 は政体と王権の割合で 0〜0.3）。
import csv, io, statistics as st, sys

ARMS = {'m873': 'D:/PoB_work/keep_873/base_ins', 'n1': 'D:/PoB_work/keep_873/n1', 'n2': 'D:/PoB_work/keep_873/n2'}
M852 = 'C:/Users/dirge/Documents/PriceOfBread/measurements/m865_20261105/landshare_paper865.txt'
OUT = sys.argv[1]
SEEDS = range(1, 11)
QS = [str(q) for q in range(12)]

# 候補（AI の記憶・要出典確認）：(下限, 上限) の幅。N 貴族・C 王領と国有地・K 教会・P 農民の自作・B 市民の地主。中点を和 1 へ比例で割り直す。
CAND = {
    'フランス':     dict(N=(0.20, 0.25), C=(0.01, 0.03), K=(0.06, 0.10), P=(0.30, 0.40), B=(0.20, 0.30), conf='中'),
    'イギリス':     dict(N=(0.70, 0.75), C=(0.01, 0.03), K=(0.03, 0.06), P=(0.15, 0.20), B=(0.02, 0.05), conf='中'),
    'ロシア':       dict(N=(0.50, 0.56), C=(0.44, 0.50), K=(0.0, 0.0), P=(0.0, 0.0), B=(0.0, 0.0), conf='中'),
    'スウェーデン': dict(N=(0.30, 0.35), C=(0.15, 0.25), K=(0.0, 0.0), P=(0.45, 0.52), B=(0.0, 0.0), conf='中'),
    '日本':         dict(N=(0.83, 0.86), C=(0.13, 0.14), K=(0.01, 0.015), P=(0.0, 0.0), B=(0.0, 0.0), conf='中'),
    'プロイセン':   dict(N=(0.55, 0.65), C=(0.25, 0.33), K=(0.01, 0.03), P=(0.05, 0.10), B=(0.0, 0.0), conf='弱'),
    'スペイン':     dict(N=(0.35, 0.50), C=(0.03, 0.08), K=(0.12, 0.18), P=(0.15, 0.30), B=(0.08, 0.18), conf='弱'),
    'デンマーク':   dict(N=(0.75, 0.85), C=(0.05, 0.15), K=(0.01, 0.03), P=(0.05, 0.10), B=(0.0, 0.0), conf='弱'),
    'ポーランド':   dict(N=(0.65, 0.75), C=(0.12, 0.18), K=(0.10, 0.15), P=(0.0, 0.0), B=(0.0, 0.0), conf='弱'),
}
B2A = ['フランス', 'イギリス', 'スペイン', 'プロイセン', 'デンマーク', '日本', 'ポーランド']   # 率の欄の意味が率だけの国（スウェーデン・米は外す・露は B-2a で動かない）
SLOTS = ['N', 'C', 'K', 'P', 'B']


def mids(c):
    m = {k: (c[k][0] + c[k][1]) / 2 for k in SLOTS}
    t = sum(m.values())
    return {k: m[k] / t for k in SLOTS}, t


def read_tsv(path):
    raw = open(path, 'rb').read()
    txt = raw.decode('utf-8-sig')
    rows = list(csv.reader(io.StringIO(txt), delimiter='\t'))
    return rows[0], rows[1:]


def f(x):
    try:
        v = float(x)
    except ValueError:
        return float('nan')
    return v


def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float('nan')


def load(base):
    """nation -> {'q0': [per seed dict], 'w': [per seed dict（q0〜q11 の平均）]}"""
    per = {}
    for s in SEEDS:
        h, rows = read_tsv(f'{base}/cornint_s{s}.tsv')
        ix = {k: i for i, k in enumerate(h)}
        hn, rn = read_tsv(f'{base}/nation_s{s}.tsv')
        jn = {k: i for i, k in enumerate(hn)}
        hs, rs = read_tsv(f'{base}/subst_s{s}.tsv')
        js = {k: i for i, k in enumerate(hs)}
        nat = {}
        for r in rn:
            q = r[jn['quarter']]
            if q in QS:
                nat[(r[jn['nation']], q)] = (f(r[jn['nominalGdpPerYear']]), f(r[jn['revenueBaseMeanPerMonth']]), f(r[jn['landTaxEndShare']]))
        tier = {}
        for r in rs:
            q = r[js['quarter']]
            if q not in QS:
                continue
            rr = f(r[js['rentToOthersPerMonth']]) + f(r[js['rentToFarmersPerMonth']])
            if rr != rr:
                rr = 0.0
            a = tier.setdefault((r[js['nation']], q), [0.0, 0.0])
            a[0 if r[js['tier']] == 'Core' else 1] += rr
        acc = {}
        for r in rows:
            q = r[ix['quarter']]
            if q not in QS:
                continue
            n = r[ix['nation']]
            dom = [f(r[ix[f'rentDomPerMonth{b}']]) for b in range(3)]
            exp = [f(r[ix[f'rentExpPerMonth{b}']]) for b in range(3)]
            dom = [x if x == x else 0.0 for x in dom]
            exp = [x if x == x else 0.0 for x in exp]
            g = nat.get((n, q), (float('nan'),) * 3)
            t = tier.get((n, q), [0.0, 0.0])
            tot = sum(dom) + sum(exp)
            d = dict(tot=tot, s=[(dom[b] + exp[b]) / tot if tot > 0 else float('nan') for b in range(3)],
                     r12=12 * tot / g[0] if g[0] and g[0] > 0 else float('nan'),
                     col=t[1] / (t[0] + t[1]) if t[0] + t[1] > 0 else float('nan'),
                     rv=g[1] / tot if tot > 0 else float('nan'), lt=g[2], rev=g[1], gdp=g[0])
            acc.setdefault(n, {})[q] = d
        for n, byq in acc.items():
            e = per.setdefault(n, {'q0': [], 'w': []})
            if '0' in byq:
                e['q0'].append(byq['0'])
            ks = ['tot', 'r12', 'col', 'rv', 'lt', 'rev', 'gdp']
            if len(byq) == 12:
                wd = {k: st.fmean([byq[q][k] for q in QS if byq[q][k] == byq[q][k]] or [float('nan')]) for k in ks}
                wd['s'] = [st.fmean([byq[q]['s'][b] for q in QS if byq[q]['s'][b] == byq[q]['s'][b]] or [float('nan')]) for b in range(3)]
                e['w'].append(wd)
    summ = {}
    for n, e in per.items():
        out = {}
        for key in ('q0', 'w'):
            L = e[key]
            if not L:
                continue
            out[key] = dict(tot=med([d['tot'] for d in L]), s0=med([d['s'][0] for d in L]), s1=med([d['s'][1] for d in L]), s2=med([d['s'][2] for d in L]),
                            r12=med([d['r12'] for d in L]), col=med([d['col'] for d in L]), rv=med([d['rv'] for d in L]), lt=med([d['lt'] for d in L]), n=len(L))
        summ[n] = out
    return summ


S = {a: load(p) for a, p in ARMS.items()}

# m852 の数（(865) の表 1 本目）
m852 = {}
raw = open(M852, 'rb').read().decode('utf-8')
sec = 0
for line in raw.split('\n'):
    if line.startswith('nation\tdom'):
        sec = 1
        continue
    if sec == 1:
        if not line.strip() or line.startswith('#'):
            break
        c = line.split('\t')
        m852[c[0]] = dict(dom=float(c[1]), s0=float(c[3]), s1=float(c[4]), s2=float(c[5]), r12=float(c[6]), col=float(c[7]), rv=float(c[8]))

L = []
P = L.append
P('# (891) c75ds 紙の数（読むだけ）：盤面 m873（keep_873/base_ins）とヌル腕 n1・n2 の開幕の RGO の地代の受け手（10 種の中央）。限界は台本の頭の注 ①〜⑦。')
P('')
P('[A] 今の受け手（q0＝1783 の最初の四半期の末・m873）とヌル腕の幅（3 本の中央の最大−最小）・m852 の数（(865)）との差')
P('nation\tdom12\ts0\ts1\ts2\trent12overGdp\tcolShare\trevBaseOverRent\tlandTaxEndShare\tw_s0(3arms)\tw_r12(3arms)\tm852_s0\tm852_r12\tm852_rv\td_r12(m873-m852)')
base = S['m873']
order = sorted(base, key=lambda k: -(base[k].get('q0', {}).get('tot', 0) or 0))
for n in order:
    r = base[n].get('q0')
    if not r:
        continue
    vals0 = [S[a][n]['q0']['s0'] for a in S if n in S[a] and 'q0' in S[a][n]]
    valsr = [S[a][n]['q0']['r12'] for a in S if n in S[a] and 'q0' in S[a][n]]
    w0 = max(vals0) - min(vals0) if vals0 else float('nan')
    wr = max(valsr) - min(valsr) if valsr else float('nan')
    o = m852.get(n)
    P(f"{n}\t{r['tot']:.1f}\t{r['s0']:.4f}\t{r['s1']:.4f}\t{r['s2']:.4f}\t{r['r12']:.4f}\t{r['col']:.4f}\t{r['rv']:.4f}\t{r['lt']:.4f}\t{w0:.2e}\t{wr:.2e}\t"
      + (f"{o['s0']:.4f}\t{o['r12']:.4f}\t{o['rv']:.4f}\t{r['r12'] - o['r12']:+.4f}" if o else 'nan\tnan\tnan\tnan'))
cnt99 = sum(1 for n in base if 'q0' in base[n] and base[n]['q0']['s0'] >= 0.99)
cnt1 = sum(1 for n in base if 'q0' in base[n] and base[n]['q0']['s0'] >= 0.99995)
r12s = sorted((base[n]['q0']['r12'], n) for n in base if 'q0' in base[n] and base[n]['q0']['r12'] == base[n]['q0']['r12'])
P(f'# 行数 {len([n for n in base if "q0" in base[n]])}・s0≧0.99 の国 {cnt99}・s0≧0.99995（四捨五入で 1.0000）の国 {cnt1}・年の地代÷名目GDP の下の 3 国 {r12s[:3]}・上の 3 国 {r12s[-3:]}')
P('')
P('[A2] 時期の揺れ：q0 と q0〜q11（1783〜85）の平均（m873・10 種の中央）')
P('nation\tr12_q0\tr12_w\ts0_q0\ts0_w\ts2_q0\ts2_w\trv_q0\trv_w')
for n in list(CAND) + ['清', 'オスマン', 'オーストリア', 'アメリカ', 'ネーデルラント']:
    if n not in base or 'w' not in base[n]:
        P(f'{n}\t(表に無い)')
        continue
    a, b = base[n]['q0'], base[n]['w']
    P(f"{n}\t{a['r12']:.4f}\t{b['r12']:.4f}\t{a['s0']:.4f}\t{b['s0']:.4f}\t{a['s2']:.4f}\t{b['s2']:.4f}\t{a['rv']:.4f}\t{b['rv']:.4f}")
P('')
P('[B] 候補（AI の記憶・要出典確認）の幅・中点・和 1 への割り直し（N 貴族・C 王領と国有地・K 教会・P 農民の自作・B 市民の地主）')
P('nation\tconf\tN\tC\tK\tP\tB\tmidSum\tN*\tC*\tK*\tP*\tB*')
NORM = {}
for n, c in CAND.items():
    m, t = mids(c)
    NORM[n] = m
    rng = '\t'.join(f'{c[k][0]:.3f}-{c[k][1]:.3f}' for k in SLOTS)
    P(f"{n}\t{c['conf']}\t{rng}\t{t:.4f}\t" + '\t'.join(f'{m[k]:.4f}' for k in SLOTS))
P('')


def moves(n, mode, key='q0', arm='m873'):
    """mode: 'a'＝B-2a（国家の枠を貴族へ畳む）・'ab'＝B-2a＋B-2b（国家の枠を国庫へ）。
    返り値：今の割合を本国の分だけ候補で置き換えたときの、層と枠ごとの移る年の地代÷名目GDP。"""
    r = S[arm][n][key]
    m = NORM[n]
    core = 1.0 - (r['col'] if r['col'] == r['col'] else 0.0)
    N = m['N'] + (m['C'] if mode == 'a' else 0.0)
    C = 0.0 if mode == 'a' else m['C']
    K, Pp, B = m['K'], m['P'], m['B']
    # 今の本国の分：s0 は閉鎖（貴族）・s1・s2（植民地格の分は今の層のまま残す近似＝(865) と同じ）
    new_b0 = r['s0'] * (1 - core) + (N + K) * core
    new_b1 = r['s1'] * (1 - core) + B * core
    new_b2 = r['s2'] * (1 - core) + Pp * core
    new_c = C * core
    tvb = (abs(new_b0 - r['s0']) + abs(new_b1 - r['s1']) + abs(new_b2 - r['s2']) + new_c) / 2
    # POP の動き（貴族→聖職を含む）：今の本国の閉鎖は貴族だけ（聖職の地代は 0＝(865) §1-3 と同じ前提）
    tvp = tvb + K * core
    return dict(core=core, b0=new_b0, b1=new_b1, b2=new_b2, c=new_c, tvb=tvb, tvp=tvp,
                toP=(new_b2 - r['s2']) * r['r12'], toB=(new_b1 - r['s1']) * r['r12'], toK=K * core * r['r12'],
                toC=new_c * r['r12'], toCrev=(new_c / r['rv'] if r['rv'] and r['rv'] > 0 else float('nan')),
                dLandTaxRev=(r['lt'] * ((new_b2 - r['s2']) + (new_b1 - r['s1'])) * 0.3 / r['rv'] if r['rv'] and r['rv'] > 0 and r['lt'] == r['lt'] else float('nan')),
                r12=r['r12'])


P('[C] B-2a（国家の枠を今の受け手＝貴族へ畳む・率の欄の意味が率だけの国）：q0 と q0〜q11 の平均・m873')
P('# toP・toB・toK＝農民・市民（資本家）・聖職者へ移る年の地代÷名目GDP。tvb＝層の動き（閉鎖・近代・大衆）。tvp＝POP の動き（貴族→聖職を含む）。')
P('# dLandTaxRev＝土地税の増分（上限・免税の度合 0.3）÷今の歳入の基数（農民と市民へ移った地代が免税を受けない分）。')
P('nation\tkey\tcore\tnew_b0\tnew_b1\tnew_b2\ttvb\ttvp\ttoP\ttoB\ttoK\tdLandTaxRev')
for n in B2A:
    for key in ('q0', 'w'):
        x = moves(n, 'a', key)
        P(f"{n}\t{key}\t{x['core']:.4f}\t{x['b0']:.4f}\t{x['b1']:.4f}\t{x['b2']:.4f}\t{x['tvb']:.4f}\t{x['tvp']:.4f}\t{x['toP']:.4f}\t{x['toB']:.4f}\t{x['toK']:.4f}\t{x['dLandTaxRev']:.4f}")
P('')
P('[D] B-2b（国家の枠を国庫へ・枠ごとの率なし＝今の率のまま）：国庫へ移る年の地代÷名目GDP と、それ÷今の歳入の基数（月）')
P('nation\tkey\tC*\tnew_state\ttoC_12overGdp\ttoC_overRevBase')
for n in CAND:
    for key in ('q0', 'w'):
        x = moves(n, 'ab', key)
        P(f"{n}\t{key}\t{NORM[n]['C']:.4f}\t{x['c']:.4f}\t{x['toC']:.4f}\t{x['toCrev']:.4f}")
P('')
# [E] 紙の no-go の規則を、旗 0（データ無し）の盤面と候補の盤面の両方に当てる（★(879)：恒真でないことの確かめ）
# 規則（AI の案）：名簿の全ての国で、B-2a の紙の動き（toP＋toB＋toK・q0〜q11 の平均）が、同じ量のヌル腕の幅（今の受け手の割合で数えた同じ量の 3 本の最大−最小）以下なら no-go。
#   旗 0（データ無し）では動きは定義で 0。ヌル腕の幅は、今の受け手のまま（s1＋s2＋聖職 0）の年の地代÷GDP の 3 本の最大−最小。
P('[E] 紙の no-go の規則の確かめ（★(879)）：規則＝名簿の全ての国で〈紙の動き（toP＋toB＋toK・q0〜q11）〉≦〈ヌル腕の幅〉なら no-go')
P('nation\tnull_width(s1+s2)*r12\tmove_flag0\tmove_cand\tnogo_flag0\tnogo_cand')
all0, allc = True, True
for n in B2A:
    v = []
    for a in S:
        r = S[a][n]['w']
        v.append((r['s1'] + r['s2']) * r['r12'])
    wdt = max(v) - min(v)
    x = moves(n, 'a', 'w')
    mv = x['toP'] + x['toB'] + x['toK']
    n0 = 0.0 <= wdt
    nc = mv <= wdt
    all0 = all0 and n0
    allc = allc and nc
    P(f'{n}\t{wdt:.3e}\t0\t{mv:.4f}\t{n0}\t{nc}')
P(f'# 旗 0（データ無し）の盤面：no-go＝{all0}（回さない側へ倒れる＝正しい）・候補の盤面：no-go＝{allc}（回す側）＝規則は両方の値を返す＝恒真でない')
P('')
# [F] 層の動きが 0 で POP だけ動く国（貴族→聖職）
P('[F] 層の動き 0 で POP（聖職）だけ動く国（B-2a）：tvb≦1e-9 かつ toK>0')
for n in B2A:
    x = moves(n, 'a', 'w')
    if x['tvb'] <= 1e-9 and x['toK'] > 0:
        P(f"{n}\ttoK={x['toK']:.5f}")
out = '\n'.join(L) + '\n'
fo = open(OUT, 'wb')
fo.write(out.encode('utf-8'))
fo.close()
