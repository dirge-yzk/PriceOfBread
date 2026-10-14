"""(324) 流れM 段5 段0 読み手＝読むだけ・挙動は変えない。Runner の副命令 fleetweight の TSV を読む。
python an_m5_static.py <fleetweight.tsv> [<fleetweight2.tsv> …]      （PYTHONUTF8=1）
出す物（断面ごと・warm／q4／q12／q19／q40／q80／q160）：
 ・T1 主要国（英・仏・蘭・日・清・米・露・普）の海の必要量 needSea と重み w（A＝越境の交易額・B1＝交易額÷年の名目GDP・B2＝TradeContactShare）
 ・T2 清÷英・日÷英・仏÷英・蘭÷英の海の必要量と目標の艦隊（want＝needSea÷海の熟練）の比（A に対する B1・B2）
 ・T3 重みの上位8国（B1）とその海の必要量の A に対する倍率＝小さい開いた国が注意の受け皿になっていないか
 ・T4 目標の艦隊（払える側の上限つき targetRef）と開幕の艦の目安 19 国（fixlist 段5 D1）の Spearman（A・B1・B2）
 ・T5 全ての国の海の必要量の合計（A に対する比）と、倍率の最大・最小の国
限界の自己申告：静的＝同じ盤面で重みだけを入れ替えた1回の再計算（艦隊の積み増し・他国の応答は動かさない）。warm の行は stored の Need* ではなく warm の盤面の再計算
（stored は開幕の1回の計算＝艦隊が無いので重みの影響を受けない）。seed 1 本の読み。fixlist の目安は記憶による【推】（出典未取得）。"""
import sys, csv, collections, statistics

HIST = {'イギリス': 150, 'ロシア': 150, 'オスマン': 90, 'フランス': 75, 'スペイン': 65, 'ネーデルラント': 35, 'デンマーク': 30, 'ヴェネツィア': 30,
        'スウェーデン': 25, '両シチリア': 25, 'ポルトガル': 12, 'プロイセン': 0, 'ハノーファー': 0, 'ライン諸邦': 0, 'アメリカ': 0, '日本': 0,
        'ハワイ王国': 0, 'エチオピア': 0, 'ペルシャ': 0}
MAIN = ['イギリス', 'フランス', 'ネーデルラント', '日本', '清', 'アメリカ', 'ロシア', 'プロイセン']
TIMES = ['warm', 'q4', 'q12', 'q19', 'q40', 'q80', 'q160']

def ranks(x):
    s = sorted(range(len(x)), key=lambda i: x[i]); r = [0.0] * len(x); i = 0
    while i < len(s):
        j = i
        while j + 1 < len(s) and x[s[j + 1]] == x[s[i]]: j += 1
        for k in range(i, j + 1): r[s[k]] = (i + j) / 2
        i = j + 1
    return r
def spearman(a, b):
    ra, rb = ranks(a), ranks(b); n = len(a)
    ma, mb = sum(ra) / n, sum(rb) / n
    nu = sum((x - ma) * (y - mb) for x, y in zip(ra, rb))
    da = sum((x - ma) ** 2 for x in ra) ** .5; db = sum((y - mb) ** 2 for y in rb) ** .5
    return nu / (da * db) if da * db > 0 else float('nan')
def f(x, nd=4):
    return f'{x:.{nd}g}'

for path in sys.argv[1:]:
    rows = list(csv.DictReader(open(path, encoding='utf-8'), delimiter='\t'))
    by = collections.defaultdict(dict)
    for r in rows:
        by[r['time']][r['nation']] = {k: (float(v) if k not in ('time', 'nation') else v) for k, v in r.items()}
    print(f'######## {path}（{len(rows)}行）')
    for t in TIMES:
        R = by.get(t)
        if not R: continue
        print(f'\n== {t}（国 {len(R)}）')
        print('T1 国\tNavalPower\t艦\tw(A)\tw(B1)\tw(B2)\tneedSea A\tB1\tB2\tB1/A\tB2/A')
        for n in MAIN:
            r = R.get(n)
            if not r: continue
            print(f"{n}\t{f(r['navalPower'])}\t{f(r['ships'],5)}\t{f(r['wA'])}\t{f(r['wB1'])}\t{f(r['wB2'])}\t{f(r['needSeaA'])}\t{f(r['needSeaB1'])}\t{f(r['needSeaB2'])}\t{f(r['needSeaB1']/r['needSeaA']) if r['needSeaA']>0 else '—'}\t{f(r['needSeaB2']/r['needSeaA']) if r['needSeaA']>0 else '—'}")
        uk = R.get('イギリス')
        if uk and uk['needSeaA'] > 0:
            print('T2 相手÷英（海の必要量／目標の艦隊 want）A\tB1\tB2')
            for n in ['清', 'フランス', 'ネーデルラント', '日本', 'アメリカ', 'ロシア']:
                r = R.get(n)
                if not r: continue
                def ratio(key, k2=None):
                    return r[key] / uk[key] if uk[key] > 0 else float('nan')
                print(f"  {n}÷英 needSea {f(ratio('needSeaA'))}\t{f(ratio('needSeaB1'))}\t{f(ratio('needSeaB2'))}   want {f(ratio('wantA'))}\t{f(ratio('wantB1'))}\t{f(ratio('wantB2'))}   targetRef {f(ratio('targetRefA'))}\t{f(ratio('targetRefB1'))}\t{f(ratio('targetRefB2'))}")
        top = sorted(R.values(), key=lambda r: -r['wB1'])[:8]
        print('T3 B1 の重みの上位8国：国\tw(B1)\tw(A)\t開放度\tneedSea A→B1（倍率）\t艦')
        for r in top:
            print(f"  {r['nation']}\t{f(r['wB1'])}\t{f(r['wA'])}\t{f(r['openness'])}\t{f(r['needSeaA'])}→{f(r['needSeaB1'])}（{f(r['needSeaB1']/r['needSeaA']) if r['needSeaA']>0 else '—'}）\t{f(r['ships'],5)}")
        sA = sum(r['needSeaA'] for r in R.values()); sB1 = sum(r['needSeaB1'] for r in R.values()); sB2 = sum(r['needSeaB2'] for r in R.values())
        ratios = sorted(((r['needSeaB1'] / r['needSeaA'], r['nation']) for r in R.values() if r['needSeaA'] > 1e-9))
        print(f'T5 海の必要量の合計 A {f(sA)}／B1 {f(sB1)}（{f(sB1/sA)}）／B2 {f(sB2)}（{f(sB2/sA)}）。B1/A の最小 {f(ratios[0][0])}（{ratios[0][1]}）・最大 {f(ratios[-1][0])}（{ratios[-1][1]}）・中央 {f(statistics.median([x for x,_ in ratios]))}')
        names = [n for n in HIST if n in R]
        if len(names) >= 10:
            h = [HIST[n] for n in names]
            for key, lab in (('targetRefA', 'A'), ('targetRefB1', 'B1'), ('targetRefB2', 'B2')):
                v = [R[n][key] for n in names]
                print(f'T4 {lab} 目標の艦隊 targetRef×史実の目安の Spearman（{len(names)}国）{f(spearman(v, h))}', end='  ')
            print()
