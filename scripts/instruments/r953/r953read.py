# (953) c84tb 読むだけの台本：盤面 m947（keep_947/base_ins・計器つき 10 種）の旗 0 の生から、
# 基準を選ぶ X の材料の数を取る。エンジンを回さない・生を書き換えない。
# 使い方: python -X utf8 -I r953read.py D:/PoB_work/keep_947/base_ins > r953read_m947.txt
import sys, csv, math, statistics as st
from collections import defaultdict

RAW = sys.argv[1] if len(sys.argv) > 1 else 'D:/PoB_work/keep_947/base_ins'
SEEDS = range(1, 11)
NATIONS = ['イギリス', '清', 'フランス', '日本', 'プロイセン', 'ロシア', 'オスマン', 'オーストリア', 'スペイン', 'アメリカ', 'ネーデルラント', '朝鮮']
ERAS = [(1783, 1814), (1815, 1869), (1870, 1913), (1914, 1950)]

def era_of(y):
    for a, b in ERAS:
        if a <= y <= b: return f'{a}-{b}'
    return None

def rd(path):
    with open(path, 'rb') as f:
        t = f.read().decode('utf-8')
    lines = t.split('\n')
    hdr = lines[0].rstrip('\r').split('\t')
    for ln in lines[1:]:
        ln = ln.rstrip('\r')
        if not ln: continue
        yield dict(zip(hdr, ln.split('\t')))

def fl(x):
    try:
        v = float(x)
        return v if math.isfinite(v) else None
    except Exception:
        return None

def q(xs, p):
    xs = sorted(xs)
    if not xs: return float('nan')
    k = (len(xs) - 1) * p
    i = int(math.floor(k)); j = min(i + 1, len(xs) - 1)
    return xs[i] + (xs[j] - xs[i]) * (k - i)

adm = defaultdict(list)            # (nation, era) -> adminFillShare
series = defaultdict(dict)         # (seed, nation) -> {quarter: row-subset}
for s in SEEDS:
    for r in rd(f'{RAW}/admrule_s{s}.tsv'):
        n = r.get('nation'); y = int(r['year']); v = fl(r.get('adminFillShare'))
        if n in NATIONS and v is not None and era_of(y): adm[(n, era_of(y))].append(v)
    for r in rd(f'{RAW}/nation_s{s}.tsv'):
        n = r.get('nation')
        if n not in NATIONS: continue
        series[(s, n)][int(r['quarter'])] = (int(r['year']), fl(r.get('priceLevel')), fl(r.get('landTaxEndShare')),
                                             fl(r.get('bondDebtToGdpYearMeanRatio')), fl(r.get('bondRateMeanPerYear')))

print('# r953read（盤面 m947・keep_947/base_ins・種 1〜10・旗 0）')
print('# [A] 行政の充足 adminFillShare（admrule_s・人口加重）の中央 [四分位]・国×時期（10 種をまとめた国×四半期）')
for n in NATIONS:
    cells = []
    for a, b in ERAS:
        xs = adm[(n, f'{a}-{b}')]
        cells.append(f'{a}-{b}: ' + (f'{q(xs,.5):.2f} [{q(xs,.25):.2f},{q(xs,.75):.2f}] n={len(xs)}' if xs else '行なし'))
    print(f'{n}\t' + ' | '.join(cells))

print('\n# [B] 物価水準 priceLevel：凍結から 10 年（40 四半期）後の比 P(t+40)/P(t) の |log| の中央と、比＜1/1.2（デフレで定額が 2 割超重くなる）・＞1.2 の割合（凍結の月を全ての四半期に仮に置く）')
print('#     ＋1 年の物価の変化（4 四半期）が −2% 未満の年の割合（定額が重くなる向き）')
for n in NATIONS:
    out = []
    for a, b in ERAS:
        lg, lo, hi, dfl, tot = [], 0, 0, 0, 0
        for s in SEEDS:
            ser = series.get((s, n), {})
            for qq, (y, p, *_r) in ser.items():
                if not (a <= y <= b) or p is None or p <= 0: continue
                p4 = ser.get(qq + 4); p40 = ser.get(qq + 40)
                if p4 and p4[1] and p4[1] > 0:
                    tot += 1
                    if p4[1] / p < 0.98: dfl += 1
                if p40 and p40[1] and p40[1] > 0:
                    r10 = p40[1] / p; lg.append(abs(math.log(r10)))
                    if r10 < 1/1.2: lo += 1
                    if r10 > 1.2: hi += 1
        if lg:
            out.append(f'{a}-{b}: |log|中央 {q(lg,.5):.3f}・＜1/1.2 {lo/len(lg):.2f}・＞1.2 {hi/len(lg):.2f}・デフレの年 {dfl/max(1,tot):.2f}')
        else:
            out.append(f'{a}-{b}: 行なし')
    print(f'{n}\t' + ' | '.join(out))

def away(x):  # MidpointRounding.AwayFromZero（Government.Q5・TaxStage と同じ丸め）
    return math.floor(abs(x) + 0.5) * (1 if x >= 0 else -1)
def tax_stage(x):  # Government.TaxStage(LandTax)：Q5（0.05 刻み）÷LandTaxTop 0.4×3 段・0..3 に挟む
    q5 = away(x / 0.05) * 0.05
    return max(0, min(3, int(away(q5 / 0.4 * 3))))

print('\n# [C] 土地税の段（Government.TaxStage を台本で写した・landTaxEndShare から）の変化の回数（種ごと・1783-1950）の中央 [最小,最大]・段＞0 の四半期の割合・上げ／下げの回数（10 種の和）')
for n in NATIONS:
    ch, pos, tot, up, dn = [], 0, 0, 0, 0
    for s in SEEDS:
        ser = series.get((s, n), {})
        prev = None; c = 0
        for qq in sorted(ser):
            v = ser[qq][2]
            if v is None: continue
            v = tax_stage(v)
            tot += 1
            if v > 0: pos += 1
            if prev is not None and v != prev:
                c += 1
                if v > prev: up += 1
                else: dn += 1
            prev = v
        if ser: ch.append(c)
    if ch:
        print(f'{n}\t変化 中央 {q(ch,.5):.0f} [{min(ch)},{max(ch)}]・率＞0 {pos/max(1,tot):.2f}・増 {up}／減 {dn}')

print('\n# [D] 国債の残高÷名目 GDP（年）bondDebtToGdpYearMeanRatio と国債の金利 bondRateMeanPerYear の中央 [四分位]（時期ごと）')
print('# 注：fiscalStressAtDecisionIndex は決定の時だけ書く列（Instruments.cs:2501）＝四半期の生ではほぼ 0＝財政の危機の物差しに使わない。国家の支払い率 FiscalFundingRatio（所得税の導入の前提 Government.cs:658）は国×四半期の列が無い')
for n in NATIONS:
    out = []
    for a, b in ERAS:
        fs, br = [], []
        for s in SEEDS:
            for qq, (y, p, lt, f, bnd) in series.get((s, n), {}).items():
                if a <= y <= b:
                    if f is not None: fs.append(f)
                    if bnd is not None and bnd > 0: br.append(bnd)
        out.append(f'{a}-{b}: 債務比{q(fs,.5):.2f} [{q(fs,.25):.2f},{q(fs,.75):.2f}]・金利 {q(br,.5):.3f} [{q(br,.25):.3f},{q(br,.75):.3f}]' if fs else f'{a}-{b}: 行なし')
    print(f'{n}\t' + ' | '.join(out))
print('\n# 限界（自己申告）：[B] は国の物価水準だけで定額と収穫比例の差を近似した（地代は産出・取り分・州でも動く＝(911) §0 の 4 の型）。[A] は人口加重の国の平均＝州ごとの査定の網は読めない。[C] は四半期末の率の変化＝四半期の中の往復は数えない。')
