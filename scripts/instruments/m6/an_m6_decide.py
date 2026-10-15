"""(354) 流れM 段0 の判定の読み手＝読むだけ。Runner の副命令 navyneed の TSV（nn_s1.tsv・nn_s5.tsv・nn_s1789.tsv）を読む。
python an_m6_decide.py <TSVのフォルダ>      （PYTHONUTF8=1）
出す物：Q（Q-W 清の必要量の英比を重み U で見る・Q-X 陸の項の割合・Q-E 英の目標÷払える額・Q-T 清の交易額÷英・受け皿の検査・英の取り分と 1914 の Spearman）／
S（候補 B・C・D の篩い①〜③＝清÷英の必要量の A に対する倍率・英の取り分・仏÷英の倍率）／
L（海の金の割合 s の分母＝max(陸+内の必要量, ForcePotential) の内訳と、重みの基数ごとの s の反実仮想〔力∝GDP×s で A に較正・粗い〕）／
P（力と艦の数・目標・払える額・港の無い国の艦）。
限界の自己申告：静的＝同じ盤面で重みだけを入れ替えた 1 回の再計算（艦隊の積み増し・他国の応答は動かさない）・種 3 本（種の中央ではない）・
L の反実仮想は〈力∝GDP×海の金の割合〉という粗い写しで、基数 A の実測に合わせるだけ（検証していない）。"""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from an_m6_static import load, ratio, spearman, TON1914
D = sys.argv[1] if len(sys.argv) > 1 else '.'
SEEDS = ('s1', 's5', 's1789')
def T(sd): return load(os.path.join(D, f'nn_{sd}.tsv'))
def sh(r, b):
    sea = r['needSea' + b]; land = max(r['needLand'] + r['needInt'], r['forcePot'])
    return sea / (sea + land) if sea + land > 0 else 0

print('#### Q  Q-W 清の必要量÷英（A と U）・Q-X 陸の項の割合（清/英）・Q-E 英の目標÷払える額・Q-T 清の交易額÷英・英の力÷必要量・重みの清/英')
for tm in ('q148', 'q308', 'q504', 'q524'):
    for sd in SEEDS:
        d = T(sd)[tm]; uk = d['イギリス']; c = d['清']
        a = ratio(c['needSeaA'], uk['needSeaA']); u = ratio(c['needSeaU'], uk['needSeaU'])
        print(f"{tm} {sd}: 清/英 A {a:.3f} U {u:.3f} (U/A英比 {u/a if a else float('nan'):.2f})  陸の項 清 {ratio(c['needSeaCross'], c['needSeaA']):.2f} 英 {ratio(uk['needSeaCross'], uk['needSeaA']):.2f}"
              f"  英目標÷払える {ratio(uk['targetRefA'], uk['affordRef']):.2f}  清交易÷英 {ratio(c['tradeSm'], uk['tradeSm']):.2f}  英力/必要 {ratio(uk['navalPower'], uk['needSeaA']):.2f}  w清/英 {c['wA']/uk['wA']:.2f}")
print('\n#### 受け皿の検査（重みの上位3国に、名目GDP 12位より下で相対重み≥3.0 の国がある断面の数・12断面中）')
for b in 'ABCD':
    cnt = 0; rows = []
    for tm in ('q148', 'q308', 'q504', 'q524'):
        for sd in SEEDS:
            d = T(sd)[tm]
            rk = {n: i + 1 for i, (n, _) in enumerate(sorted(d.items(), key=lambda kv: -kv[1]['gdpYear']))}
            top = sorted(d.items(), key=lambda kv: -kv[1]['w' + b])[:3]
            hit = [(n, round(x['w' + b], 1), rk[n]) for n, x in top if rk[n] > 12 and x['w' + b] >= 3.0]
            if hit: cnt += 1; rows.append((tm, sd, hit))
    print(b, '当たる断面', cnt, '/12')
    for r in rows: print('   ', r)
print('\n#### 英の取り分（英の海の必要量÷全ての国の和）と 1914 のトン数との Spearman（目標の艦隊・n=8）')
for tm in ('q504', 'q524'):
    for sd in SEEDS:
        d = T(sd)[tm]; uk = d['イギリス']
        tot = {b: sum(x['needSea' + b] for x in d.values()) for b in 'ABCDU'}
        ns = [n for n in TON1914 if n in d]
        print(tm, sd, '取り分', ' '.join(f"{b}{ratio(uk['needSea'+b], tot[b]):.3f}" for b in 'AUBCD'), '| Spearman目標',
              ' '.join(f"{b}{spearman([d[n]['targetRef'+b] for n in ns], [TON1914[n] for n in ns]):.2f}" for b in 'AUBCD'),
              '| 力', f"{spearman([d[n]['navalPower'] for n in ns], [TON1914[n] for n in ns]):.2f}")

print('\n#### S  候補の篩い①清÷英の必要量の A に対する倍率 ②英の取り分の A に対する倍率 ③仏÷英の倍率（B）')
for tm in ('q308', 'q504'):
    for sd in SEEDS:
        d = T(sd)[tm]; uk = d['イギリス']; c = d['清']; f = d['フランス']
        a = ratio(c['needSeaA'], uk['needSeaA'])
        rr = {b: ratio(c['needSea' + b], uk['needSea' + b]) / a for b in 'BCDU'}
        tt = {b: sum(x['needSea' + b] for x in d.values()) for b in 'ABCD'}
        sp = {b: ratio(uk['needSea' + b], tt[b]) / ratio(uk['needSeaA'], tt['A']) for b in 'BCD'}
        fr = ratio(f['needSeaB'], uk['needSeaB']) / ratio(f['needSeaA'], uk['needSeaA'])
        print(tm, sd, f"清/英 A={a:.3f} B/A={rr['B']:.2f} C/A={rr['C']:.2f} D/A={rr['D']:.2f} U/A={rr['U']:.2f} | 英の取り分 B {sp['B']:.2f} C {sp['C']:.2f} D {sp['D']:.2f} | 仏 B/A {fr:.2f}")

print('\n#### L  海の金の割合 s＝海の必要量÷(海＋max(陸+内, ForcePotential))（A の木）')
names = ['イギリス', 'フランス', 'ロシア', 'アメリカ', '日本', '清', 'プロイセン']
for tm in ('q308', 'q504', 'q524'):
    for sd in SEEDS:
        d = T(sd)[tm]; out = []
        for n in names:
            r = d[n]; li = r['needLand'] + r['needInt']; fp = r['forcePot']
            out.append(f"{n[:2]}: 海{r['needSeaA']:.3g} 陸{r['needLand']:.3g} 内{r['needInt']:.3g} fp{fp:.3g} {'fp律速' if fp > li else '陸内律速'} s{sh(r,'A'):.2f}")
        print(tm, sd, ' | '.join(out))
print('\n#### L2 反実仮想：力÷英 を基数ごとの s で書き換えた予測（A の実測→U/B/C/D）')
for tm in ('q308', 'q504', 'q524'):
    for sd in SEEDS:
        d = T(sd)[tm]; uk = d['イギリス']; row = []
        for n in ['フランス', 'ロシア', 'アメリカ', '日本', '清', 'プロイセン']:
            r = d[n]; p = ratio(r['navalPower'], uk['navalPower']); pr = []
            for b in 'UBCD':
                k = (sh(r, b) / sh(r, 'A')) / (sh(uk, b) / sh(uk, 'A')) if sh(r, 'A') > 0 and sh(uk, 'A') > 0 else float('nan')
                pr.append(p * k)
            row.append(f"{n}:{p:.2f}→" + '/'.join(f"{x:.2f}" for x in pr))
        print(tm, sd, '  '.join(row))

print('\n#### P  力・艦・目標・払える額・港の無い国の艦')
for tm in ('q308', 'q504'):
    for sd in SEEDS:
        d = T(sd)[tm]
        for n in ['イギリス', 'フランス', 'ロシア', 'アメリカ', '日本', '清', 'プロイセン']:
            r = d[n]
            print(f"{tm} {sd} {n:<6} 力{r['navalPower']:.2f} 艦{r['ships']:.0f} 目標{r['targetRefA']:.3g} 払える{r['affordRef']:.3g} 艦/目標{ratio(r['ships'], r['targetRefA']):.3g} 海の金{r['spendShare']:.3f} GDP{r['gdpYear']:.3g}")
for sd in SEEDS:
    S = T(sd)
    for tm in ('q148', 'q308', 'q504', 'q524'):
        d = S[tm]; totP = sum(r['navalPower'] for r in d.values())
        lk = sorted([(n, r['navalPower']) for n, r in d.items() if r['port'] == 0 and r['ships'] > 0], key=lambda x: -x[1])
        print(sd, tm, f"港の無い国の艦＝{len(lk)}国・海軍力の和 {sum(x[1] for x in lk):.2f}／世界 {totP:.1f}", [(n, round(p, 2)) for n, p in lk[:3]])
