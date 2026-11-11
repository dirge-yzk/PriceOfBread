# (939) c82ln 読み手（読むだけ・設計の材料）：盤面 m933（keep_933/base_ins）とヌル腕（keep_933/n1・n2）の生で、
#   ① B-2b（露・普の国家の枠を国営持分へ畳む＝案 S1）の開幕の跳ねと門 G-S1〜G-S4・no-go の紙を取り直す（(918) read918.py の盤面 m905 → m933）
#   ② B-2a で 4 枠になった名簿 7 国の今の欄の数と、露・普の 5 枠の値との関係
#   ③ 鉱山の地代の上限（生に財ごとの列が無い＝上限だけ・(918) と同じ定義）
# 使い方：python -X utf8 -I read939.py <出力.txt>
# 入力（読むだけ）：landshare_s・nation_s・cornint_s・fiscal_s（種 1〜10）・PriceOfBread.SimCore/world.json（main 84688034）
# 限界（自己申告）：
#  ①国家の枠の期待（crownExp）＝c×〈非賦存×今の持ち主が貴族×非植民地格の国内の正の地代〉（landshare_s の rentNonEndowByOwnerClassNoblePerQuarter・受け口の時点の持ち主の階級）。
#    案 S1 の国家の枠＝rent_pre×(1−f)×c。今の盤面で RGO の f は 0 と読む（出力 ② の fiscal_s の ledgerStateFactoryPerMonth が露・普で q0 も 1783〜85 も 0＝工場を含む国の和で国営の収入が無い）＝受け口の rent＝rent_pre。
#    輸出の地代は expRentPerQuarter（賦存・植民地格の大農園を含む全て）だけ＝輸出の側は〈下限 0〉〜〈上限 c×expRent 全て〉の幅。
#  ②〈今の歳入の基数〉は nation_s の revenueBaseMeanPerMonth（(891)・(918) と同じ列）。B-2b の後は貴族の所得が減る＝他の税の基数も動く（一次の近似）。
#  ③旗 0 の〈実際の側〉の代わり＝fiscal_s の ledgerStateFactoryPerMonth（国営工場の区分の台帳・工場を含む国の和）。月の行は monthIndex の小さい順の頭 3 行を q0・1783〜1785 を w とする（landshare_s の四半期と月の揃えは確かめていない）。
#  ④鉱山の地代は生に財ごとの列が無い＝上限＝min(非賦存×貴族の地代, 国内の地代−穀物の地代)÷非賦存×貴族の地代（(918) ③ と同じ）。下限は出せない。
#  ⑤q0＝最初の四半期。w＝q0〜q11 の平均。種 1〜10 の中央。
#  ⑥5 枠の値は B-2-0 材料（docs/202611/20261106/…B-2-0…_材料.md:110・:113）の割り直し N*/C*/K*/P*/B* を枠の順（0 貴族・1 教会・2 農民・3 市民・4 国家）へ並べ替えた物（AI の記憶・要出典確認）。
import csv, io, json, statistics as st, sys

OUT = sys.argv[1]
ARMS = {'m933': 'D:/PoB_work/keep_933/base_ins', 'n1': 'D:/PoB_work/keep_933/n1', 'n2': 'D:/PoB_work/keep_933/n2'}
WORLD = 'C:/Users/dirge/Documents/PriceOfBread/PriceOfBread.SimCore/world.json'
SEEDS = range(1, 11)
QS = [str(q) for q in range(12)]
# 5 枠（0 貴族・1 教会・2 農民・3 市民・4 国家）＝B-2b の候補（露・普だけ）
FIVE = {'ロシア': [0.530, 0.0, 0.0, 0.0, 0.470], 'プロイセン': [0.609, 0.020, 0.076, 0.0, 0.294]}
# 参考（B-2a で国家の枠を貴族へ畳んだ国の C*・畳んだまま残る分）
CSTAR = {'ロシア': 0.470, 'プロイセン': 0.294, 'ポーランド': 0.154, '日本': 0.136, 'スウェーデン': 0.198,
         'デンマーク': 0.101, 'スペイン': 0.056, 'フランス': 0.022, 'イギリス': 0.020}
NAMES = list(CSTAR)


def read_tsv(path):
    txt = open(path, 'rb').read().decode('utf-8-sig')
    rows = [r for r in csv.reader(io.StringIO(txt), delimiter='\t') if r and not r[0].startswith('#')]
    return rows[0], rows[1:]


def f(x):
    try:
        return float(x)
    except ValueError:
        return float('nan')


def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float('nan')


def cnorm(n):
    v = FIVE.get(n)
    return v[4] / sum(v) if v else 0.0


def load(base):
    per = {}
    for s in SEEDS:
        hl, rl = read_tsv(f'{base}/landshare_s{s}.tsv')
        il = {k: i for i, k in enumerate(hl)}
        hn, rn = read_tsv(f'{base}/nation_s{s}.tsv')
        jn = {k: i for i, k in enumerate(hn)}
        hc, rc = read_tsv(f'{base}/cornint_s{s}.tsv')
        jc = {k: i for i, k in enumerate(hc)}
        hf, rf = read_tsv(f'{base}/fiscal_s{s}.tsv')
        jf = {k: i for i, k in enumerate(hf)}
        nat = {}
        for r in rn:
            q = r[jn['quarter']]
            if q in QS and r[jn['nation']] in CSTAR:
                nat[(r[jn['nation']], q)] = (f(r[jn['revenueBaseMeanPerMonth']]), f(r[jn['nominalGdpPerYear']]))
        cor = {}
        for r in rc:
            q = r[jc['quarter']]
            if q in QS and r[jc['nation']] in CSTAR:
                tot = f(r[jc['rentDomTotalPerMonth']])
                gr = sum(f(r[jc[f'rentGrainDomPerMonth{b}']]) for b in range(3))
                cor[(r[jc['nation']], q)] = (tot, gr)
        led = {}
        for r in rf:
            n = r[jf['nation']]
            if n in CSTAR:
                led.setdefault(n, []).append((int(r[jf['monthIndex']]), int(r[jf['year']]), f(r[jf['ledgerStateFactoryPerMonth']])))
        ledq0 = {}
        ledw = {}
        for n, xs in led.items():
            xs.sort()
            ledq0[n] = st.fmean([x[2] for x in xs[:3]])
            ledw[n] = st.fmean([x[2] for x in xs if x[1] <= 1785] or [float('nan')])
        acc = {}
        for r in rl:
            q = r[il['quarter']]
            n = r[il['nation']]
            if q not in QS or n not in CSTAR:
                continue
            m = f(r[il['monthsInQuarter']]) or 3.0
            neN = f(r[il['rentNonEndowByOwnerClassNoblePerQuarter']]) / m
            ne = f(r[il['rentNonEndowPerQuarter']]) / m
            en = f(r[il['rentEndowPerQuarter']]) / m
            ex = f(r[il['expRentPerQuarter']]) / m
            actN = f(r[il['landRentActualNoblePerQuarter']]) / m
            rev, gdp = nat.get((n, q), (float('nan'), float('nan')))
            tot, gr = cor.get((n, q), (float('nan'), float('nan')))
            c = cnorm(n)
            cs = CSTAR[n]
            ce = c * neN
            d = dict(neN=neN, ne=ne, en=en, ex=ex, rev=rev, gdp=gdp, actN=actN,
                     crownExp=ce,
                     lo=ce / rev if rev > 0 else float('nan'),
                     hi=c * (neN + ex) / rev if rev > 0 else float('nan'),
                     loStar=cs * neN / rev if rev > 0 else float('nan'),
                     lo12=12 * ce / gdp if gdp > 0 else float('nan'),
                     gs4wrong=(neN + en) / neN if neN > 0 else float('nan'),
                     nobleNewOld=(actN - ce) / actN if actN > 0 else float('nan'),
                     endowSh=en / (ne + en) if ne + en > 0 else float('nan'),
                     nobleSh=neN / ne if ne > 0 else float('nan'),
                     nonGrainUp=(min(neN, tot - gr) / neN) if neN > 0 and tot == tot else float('nan'),
                     fieldCnt=f(r[il['rgoFieldCount']]), nullCnt=f(r[il['rgoNullCount']]), endowCnt=f(r[il['rgoEndowCount']]),
                     fieldEndowCnt=f(r[il['rgoFieldEndowCount']]), fieldColCnt=f(r[il['rgoFieldColonialCount']]), fieldReg=f(r[il['fieldRegionCount']]),
                     slotDiff=f(r[il['maxRelDiffSlotVsActual']]), sumDev=f(r[il['fieldSumMaxAbsDev']]), fallback=f(r[il['slotFallbackCount']]))
            acc.setdefault(n, {})[q] = d
        for n, byq in acc.items():
            e = per.setdefault(n, {'q0': [], 'w': []})
            if '0' in byq:
                d0 = dict(byq['0'])
                d0['led'] = ledq0.get(n, float('nan'))
                d0['ledOverExp'] = d0['led'] / d0['crownExp'] if d0['crownExp'] > 0 else float('nan')
                e['q0'].append(d0)
            if len(byq) == 12:
                dw = {k: st.fmean([byq[q][k] for q in QS if byq[q][k] == byq[q][k]] or [float('nan')]) for k in byq['0']}
                dw['led'] = ledw.get(n, float('nan'))
                dw['ledOverExp'] = dw['led'] / dw['crownExp'] if dw['crownExp'] > 0 else float('nan')
                dw['slotDiff'] = max(byq[q]['slotDiff'] for q in QS)
                e['w'].append(dw)
    return {n: {key: {k: med([d[k] for d in e[key]]) for k in (e[key][0] if e[key] else {})} for key in ('q0', 'w')}
            | {'nseed': len(e['q0']), 'slotDiffMax': max([d['slotDiff'] for d in e['w']] or [float('nan')])} for n, e in per.items()}


out = []
P = out.append
P('# (939) read939.py の出力（盤面 m933＝keep_933/base_ins・ヌル腕 keep_933/n1・n2・種 1〜10 の中央）')
data = {a: load(b) for a, b in ARMS.items()}
P('')
P('## ① B-2b 案 S1：国家の枠 c（5 枠を和で割った 5 個目）×非賦存×貴族の国内の地代＝crownExp。lo＝crownExp÷歳入の基数・hi＝輸出の地代を全て足した上限・loStar＝(918) の C* のまま')
P('nation\tc\tarm\tkey\tnseed\tlo\thi\tloStar\tlo12overGdp\tnobleOfNonEndow\tendowShareOfDom\trevBasePerMonth\tneNoblePerMonth\texpPerMonth\tcrownExpPerMonth')
for n in NAMES:
    for a in ARMS:
        x = data[a].get(n)
        if not x:
            P(f'{n}\t{cnorm(n):.4f}\t{a}\t-\t0')
            continue
        for key in ('q0', 'w'):
            d = x[key]
            P(f"{n}\t{cnorm(n):.4f}\t{a}\t{key}\t{x['nseed']}\t{d['lo']:.3f}\t{d['hi']:.3f}\t{d['loStar']:.3f}\t{d['lo12']:.4f}\t{d['nobleSh']:.3f}\t{d['endowSh']:.3f}\t{d['rev']:.1f}\t{d['neN']:.1f}\t{d['ex']:.1f}\t{d['crownExp']:.1f}")
P('')
P('## ② 門の紙（露・普・m933）：G-S1 の旗 0＝台帳の国営工場の区分 ledgerSF÷crownExp（工場を含む上限・0 に近ければ赤）・G-S4 の誤りの形（賦存にも国家の枠）＝(neN+en)÷neN・貴族の LandRent の新÷旧＝(actNoble−crownExp)÷actNoble')
P('nation\tarm\tkey\tledgerSFPerMonth\tledgerSF_over_crownExp\tGS4_wrongRatio\tnobleLandRentNewOld\tslotDiffMax(w)')
for n in FIVE:
    for a in ARMS:
        x = data[a].get(n)
        if not x:
            continue
        for key in ('q0', 'w'):
            d = x[key]
            P(f"{n}\t{a}\t{key}\t{d['led']:.3f}\t{d['ledOverExp']:.4f}\t{d['gs4wrong']:.4f}\t{d['nobleNewOld']:.4f}\t{x['slotDiffMax'] if key == 'w' else d['slotDiff']:.3g}")
P('')
P('## ③ no-go の紙：規則〈露・普の q0〜q11（w）で crownExp÷歳入の基数（lo）が、ヌル腕の幅（m933・n1・n2 の lo の最大−最小）以下なら回さない〉')
P('nation\tdata\tlo_m933\tlo_n1\tlo_n2\twidth\tverdict')
for n in FIVE:
    los = [data[a][n]['w']['lo'] for a in ARMS if n in data[a]]
    width = max(los) - min(los)
    P(f"{n}\t5枠の候補\t{los[0]:.4f}\t{los[1]:.4f}\t{los[2]:.4f}\t{width:.4f}\t{'go' if los[0] > width else 'no-go'}")
    P(f"{n}\t旗0（データ無し＝c=0）\t0\t0\t0\t{width:.4f}\tno-go")
P('')
P('## ④ 欄の数（q0・m933）：B-2a の名簿 7 国（4 枠）と露・瑞（データ無し）')
P('nation\tarm\trgoFieldCount\trgoNullCount\trgoEndowCount\trgoFieldEndowCount\trgoFieldColonialCount\tfieldRegionCount\tslotFallbackCount')
for n in NAMES:
    x = data['m933'].get(n)
    if x:
        d = x['q0']
        P(f"{n}\tm933\t{d['fieldCnt']:.0f}\t{d['nullCnt']:.0f}\t{d['endowCnt']:.0f}\t{d['fieldEndowCnt']:.0f}\t{d['fieldColCnt']:.0f}\t{d['fieldReg']:.0f}\t{d['fallback']:.0f}")
P('')
P('## ⑤ 鉱山の地代の上限（m933）：非賦存×貴族の地代のうち穀物でない分の割合（上限）')
P('nation\tkey\tnonGrainUpShare')
for n in NAMES:
    x = data['m933'].get(n)
    if x:
        for key in ('q0', 'w'):
            P(f"{n}\t{key}\t{x[key]['nonGrainUp']:.3f}")
P('')
P('## ⑥ world.json の今の値（main 84688034）と 5 枠の候補')
w = json.load(open(WORLD, 'rb'))
P('nation\tlandOwnership(今)\tfarmLordShare\tlandTaxStage\tinitialSerfEmancipation\t5枠の候補(0貴族,1教会,2農民,3市民,4国家)\t旗0の畳み[0]+[4]を和で割る\t今の[0]との差')
for nd in w['nations']:
    n = nd['name']
    if n not in CSTAR:
        continue
    lo = nd.get('landOwnership')
    five = FIVE.get(n)
    fold = ''
    diff = ''
    if five:
        s5 = sum(five)
        fold = f'{(five[0] + five[4]) / s5:.6f}'
        if lo:
            diff = f'{(five[0] + five[4]) / s5 - lo[0] / sum(lo):.2e}'
    P(f"{n}\t{lo}\t{nd.get('farmLordShare')}\t{nd.get('landTaxStage')}\t{nd.get('initialSerfEmancipation')}\t{five}\t{fold}\t{diff}")
open(OUT, 'wb').write(('\n'.join(out) + '\n').encode('utf-8'))
print('wrote', OUT, len(out))
