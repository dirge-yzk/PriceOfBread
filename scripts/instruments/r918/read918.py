# (918) c79lo 読み手（読むだけ・設計の材料）：盤面 m905（keep_912/base_ins）とヌル腕（keep_905/n1・n2）の生で、
#   ① B-2b の国家の枠（C*）を国庫へ入れたときの開幕の跳ね（今の歳入の基数との比）を取り直す（(865)/(891) の 1.42・0.84 の再取得）
#   ② ★(912) ① の (a)(b) の材料＝名簿の国の鉱山の RGO の数（静的・world.json と Catalog の規則から）と、地代の桁の上限（生の列から）
# 使い方：python -X utf8 -I read918.py <出力.txt>
# 入力（読むだけ）：landshare_s・nation_s・cornint_s（種 1〜10）・PriceOfBread.SimCore/world.json
# 限界（自己申告）：
#  ①国家の枠の国庫へは〈非賦存×今の持ち主が貴族×非植民地格の国内の正の地代〉×C*（landshare_s の rentNonEndowByOwnerClassNoblePerQuarter）＝B-0 の構築（Simulation.cs:968〜:972）が欄を置く集合と同じ判別。
#    輸出の地代は landshare_s の expRentPerQuarter（賦存・植民地格の大農園を含む全て）しか無い＝輸出の側は〈下限 0〉〜〈上限 C*×expRent 全て〉の幅で書く。
#  ②〈今の歳入の基数〉は nation_s の revenueBaseMeanPerMonth（(891) と同じ列）。B-2b の後は貴族の所得が減る＝他の税の基数も動く（この読みは一次の近似）。
#  ③鉱山の地代は生に財ごとの列が無い（landshare_s は賦存／非賦存だけ・cornint は穀物／全て）＝上限＝min(非賦存×貴族の地代, 国内の地代−穀物の地代)。下限は出せない。
#  ④鉱山の RGO の数は静的（BuildProvince の州×Catalog.IsMineral の 14 財−州の resources に載る財）。希少鉱物は今の係数で硫黄だけ（ScarceMineralOnlySulfur=1）＝共有世界で非産地の硫黄は Productivity=0（Simulation.cs:850）＝欄は付くが地代 0。
#    産地の発見の表（Catalog.Sites）で名簿の国に当たるのは〈九州の硫黄〉（北九州・1880）だけ（Catalog.cs:798〜）。原油は機械の投入（OilNeedsMachinery）＝開幕の地代はほぼ 0 の見込み（確かめていない）。
#  ⑤q0＝1783 の最初の四半期。w＝q0〜q11 の平均。種 1〜10 の中央。
import csv, io, json, statistics as st, sys

OUT = sys.argv[1]
ARMS = {'m905': 'D:/PoB_work/keep_912/base_ins', 'n1': 'D:/PoB_work/keep_905/n1', 'n2': 'D:/PoB_work/keep_905/n2'}
WORLD = 'C:/Users/dirge/Documents/PriceOfBread/PriceOfBread.SimCore/world.json'
SEEDS = range(1, 11)
QS = [str(q) for q in range(12)]
# (891) material §C の割り直しの C*（国家の枠）。数の出所は AI の記憶（要出典確認）＝この台本は動きの桁だけを読む。
CSTAR = {'ロシア': 0.470, 'プロイセン': 0.294, 'ポーランド': 0.154, '日本': 0.136, 'スウェーデン': 0.198,
         'デンマーク': 0.101, 'スペイン': 0.056, 'フランス': 0.022, 'イギリス': 0.020}
MINERALS = {0: '鉄鉱石', 1: '石炭', 2: '銅鉱石', 3: '鉛鉱石', 4: '金鉱石', 5: '硫黄', 7: '石灰石', 8: '珪砂', 9: '粘土', 10: '原油',
            89: 'クロム', 107: '錫鉱石', 108: '硝石', 110: '銀'}
SCARCE = {5}   # Coefficients.cs:1343 ScarceMineralOnlySulfur=1 → Catalog.cs:337〜:341 で希少鉱物は硫黄だけ（金・銀・クロム・錫・硝石は非産地でも ×0.4 で採れる）


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


def load(base):
    per = {}   # (nation) -> list over seeds of dict(q0=..., w=...)
    for s in SEEDS:
        hl, rl = read_tsv(f'{base}/landshare_s{s}.tsv')
        il = {k: i for i, k in enumerate(hl)}
        hn, rn = read_tsv(f'{base}/nation_s{s}.tsv')
        jn = {k: i for i, k in enumerate(hn)}
        hc, rc = read_tsv(f'{base}/cornint_s{s}.tsv')
        jc = {k: i for i, k in enumerate(hc)}
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
            co = f(r[il['rentColonialPerQuarter']]) / m
            ex = f(r[il['expRentPerQuarter']]) / m
            rev, gdp = nat.get((n, q), (float('nan'), float('nan')))
            tot, gr = cor.get((n, q), (float('nan'), float('nan')))
            c = CSTAR[n]
            d = dict(neN=neN, ne=ne, en=en, co=co, ex=ex, rev=rev, gdp=gdp,
                     lo=c * neN / rev if rev > 0 else float('nan'),
                     hi=c * (neN + ex) / rev if rev > 0 else float('nan'),
                     lo12=12 * c * neN / gdp if gdp > 0 else float('nan'),
                     endowSh=en / (ne + en) if ne + en > 0 else float('nan'),
                     nobleSh=neN / ne if ne > 0 else float('nan'),
                     nonGrainUp=(min(neN, tot - gr) / neN) if neN > 0 and tot == tot else float('nan'),
                     nonGrainUp12=12 * min(neN, tot - gr) / gdp if gdp > 0 and tot == tot else float('nan'))
            acc.setdefault(n, {})[q] = d
        for n, byq in acc.items():
            e = per.setdefault(n, {'q0': [], 'w': []})
            if '0' in byq:
                e['q0'].append(byq['0'])
            if len(byq) == 12:
                e['w'].append({k: st.fmean([byq[q][k] for q in QS if byq[q][k] == byq[q][k]] or [float('nan')]) for k in byq['0']})
    return {n: {key: {k: med([d[k] for d in e[key]]) for k in (e[key][0] if e[key] else {})} for key in ('q0', 'w')} | {'nseed': len(e['q0'])} for n, e in per.items()}


out = []
P = out.append
P('# (918) read918.py の出力（盤面 m905＝keep_912/base_ins・ヌル腕 keep_905/n1・n2・種 1〜10 の中央）')
data = {a: load(b) for a, b in ARMS.items()}
P('')
P('## ① B-2b：国家の枠 C* を国庫へ（今の率のまま）＝国庫へ÷今の歳入の基数（lo＝国内だけ・hi＝輸出の地代を全て足した上限）')
P('nation\tC*\tarm\tkey\tnseed\tlo_overRev\thi_overRev\tlo_12overGdp\tnobleOfNonEndow\tendowShareOfDom\trevBasePerMonth\tneNoblePerMonth\texpPerMonth')
for n in CSTAR:
    for a in ARMS:
        x = data[a].get(n)
        if not x:
            P(f'{n}\t{CSTAR[n]}\t{a}\t-\t0')
            continue
        for key in ('q0', 'w'):
            d = x[key]
            P(f"{n}\t{CSTAR[n]}\t{a}\t{key}\t{x['nseed']}\t{d['lo']:.3f}\t{d['hi']:.3f}\t{d['lo12']:.4f}\t{d['nobleSh']:.3f}\t{d['endowSh']:.3f}\t{d['rev']:.1f}\t{d['neN']:.1f}\t{d['ex']:.1f}")
P('')
P('## ②-1 鉱山の地代の上限（非賦存×貴族の地代のうち穀物でない分の上限の割合・年÷GDP）＝min(neNoble, 国内−穀物)÷neNoble')
P('nation\tarm\tkey\tnonGrainUpShare\tnonGrainUp_12overGdp')
for n in CSTAR:
    for a in ('m905',):
        x = data[a].get(n)
        if x:
            for key in ('q0', 'w'):
                d = x[key]
                P(f"{n}\t{a}\t{key}\t{d['nonGrainUp']:.3f}\t{d['nonGrainUp12']:.4f}")
P('')
P('## ②-2 鉱山の RGO の数（静的・BuildProvince の州×IsMineral 14 財−州の resources）')
w = json.load(open(WORLD, 'rb'))
P('nation\tfarmLordShare\tprovinces\tmineRgoNonEndow\tnonScarceNonEndow\tscarceNonEndow(地代0)\tmineEndowed(欄なし)\tendowedMineralsSeen')
for nd in w['nations']:
    n = nd['name']
    if n not in CSTAR:
        continue
    prov = nd.get('provinces', [])
    tot = ns = sc = ed = 0
    seen = {}
    for p in prov:
        res = set(p.get('resources', []))
        for g in MINERALS:
            if g in res:
                ed += 1
                seen[MINERALS[g]] = seen.get(MINERALS[g], 0) + 1
            else:
                tot += 1
                if g in SCARCE:
                    sc += 1
                else:
                    ns += 1
    P(f"{n}\t{nd.get('farmLordShare')}\t{len(prov)}\t{tot}\t{ns}\t{sc}\t{ed}\t{','.join(f'{k}{v}' for k, v in seen.items())}")
P('')
P('## ③ 率の欄（world.json）：farmLordShare・landTaxStage・initialSerfEmancipation・rule（名簿の国）')
P('nation\tfarmLordShare\tlandTaxStage\tinitialSerfEmancipation\tpolity\trule')
for nd in w['nations']:
    if nd['name'] in CSTAR:
        P(f"{nd['name']}\t{nd.get('farmLordShare')}\t{nd.get('landTaxStage')}\t{nd.get('initialSerfEmancipation')}\t{nd.get('polity')}\t{nd.get('rule')}")
open(OUT, 'wb').write(('\n'.join(out) + '\n').encode('utf-8'))
print('wrote', OUT, len(out))
