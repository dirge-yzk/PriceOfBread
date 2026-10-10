"""(176) 戦時の財政の読み手（読むだけ）＝hist の A 行（GDP・人口・兵士・交戦の月）と C 行（台帳の四半期差分・国債）。
  国×10年の窓（1783-89・1790年代…1940年代）・種ごとの和の比→種の中央：
    ①軍事費÷年GDP（全ての四半期）②同・戦時の四半期だけ（交戦か内戦の月>0）③借入÷年GDP（全）④同・戦時 ⑤切り下げ÷年GDP（戦時）
    ⑥国債÷年GDP（窓の最後の四半期）
  史実比較の桁（10年の窓の平均・史実は記憶の概数【推】）＝下の H 表のセルだけ判定（比10倍以上か1/10以下で NG・3〜10倍は記録・床 1%・国債は床 5%）。
python an_fin176.py <計器の出力ディレクトリ（nation 表）> [ラベル] [--seeds N]（(245) 旧の注入の行を標準の計器 SimCore/Instruments.cs の表へ畳んだ）"""
import sys, os, statistics as st, collections
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import rows as table_rows, num
args = sys.argv[1:]; maxsd = 99
if '--seeds' in args:
    i = args.index('--seeds'); maxsd = int(args[i + 1]); del args[i:i + 2]
D = args[0]; LAB = args[1] if len(args) > 1 else D
NAT = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'オーストリア', 'アメリカ', '日本']
WIN = [(1783, 1789)] + [(y, y + 9) for y in range(1790, 1950, 10)]
WIN[-1] = (1940, 1950)
# 史実【推・記憶の概数】＝10年の窓の平均（%）：軍事費÷年GDP・借入÷年GDP／国債÷年GDP（窓の終わり）
HM = {('イギリス', 1800): 13, ('イギリス', 1810): 12, ('イギリス', 1910): 20, ('イギリス', 1940): 25,
      ('フランス', 1800): 8, ('フランス', 1910): 25, ('フランス', 1940): 15,
      ('プロイセン', 1810): 6, ('プロイセン', 1910): 25, ('プロイセン', 1940): 25,
      ('ロシア', 1910): 15, ('ロシア', 1940): 25, ('オーストリア', 1910): 15,
      ('アメリカ', 1860): 6, ('アメリカ', 1910): 4, ('アメリカ', 1940): 17, ('日本', 1900): 8, ('日本', 1940): 20}
HB = {('イギリス', 1800): 5, ('イギリス', 1810): 4, ('イギリス', 1910): 15, ('イギリス', 1940): 12,
      ('フランス', 1910): 15, ('プロイセン', 1910): 15, ('ロシア', 1910): 8, ('オーストリア', 1910): 10,
      ('アメリカ', 1860): 5, ('アメリカ', 1910): 4, ('アメリカ', 1940): 8, ('日本', 1940): 15}
# 英の国債÷年GDP（窓の終わり＝1819・1829・…）【推・記憶の概数】＝1821 の頂点 約2.6 から 1913 の約0.3 へ下り・1919 約1.35・1946 約2.5
HD = {('イギリス', 1810): 240, ('イギリス', 1820): 210, ('イギリス', 1830): 170, ('イギリス', 1840): 140, ('イギリス', 1850): 110, ('イギリス', 1860): 85,
      ('イギリス', 1870): 65, ('イギリス', 1880): 50, ('イギリス', 1890): 35, ('イギリス', 1900): 30, ('イギリス', 1910): 135, ('イギリス', 1920): 170,
      ('イギリス', 1930): 140, ('イギリス', 1940): 220,
      ('フランス', 1910): 170, ('アメリカ', 1860): 30, ('アメリカ', 1940): 120, ('日本', 1940): 200}


def win(y):
    for i, (a, b) in enumerate(WIN):
        if a <= y <= b: return i
    return None


A = {}   # (sd, q, idx) -> (yr, name, alive, gdp, war)　q は 1 始まり（旧の A 行の q+1）
for sd, r in table_rows(D, 'nation', maxsd):
    if r['nation'] not in NAT: continue
    A[(sd, int(r['quarter']) + 1, int(r['nationIndex']))] = (int(r['year']), r['nation'], 1 if num(r['totalPopulationK']) > 0 else 0, num(r['nominalGdpPerYear']), num(r['atWarMonths']) > 0 or num(r['civilWarMonths']) > 0)
# acc[name][sd][wi] = [gdpAll, milAll, borAll, gdpWar, milWar, borWar, devWar, lastQ, debtLast, gdpLast, warQ, nQ]
acc = collections.defaultdict(lambda: collections.defaultdict(lambda: collections.defaultdict(lambda: [0.0] * 12)))
prev = {}   # (sd, idx) -> 前の四半期の台帳の累計（借入・切り下げ・陸・海）＝四半期の差分は累計の差（最初の四半期は0から）
for sd, r in table_rows(D, 'nation', maxsd):
    idx = int(r['nationIndex'])
    cum = (num(r['ledgerBorrowCumulative']), num(r['ledgerDevalueCumulative']), num(r['ledgerArmyCumulative']), num(r['ledgerNavyCumulative']))
    pv = prev.get((sd, idx), (0.0, 0.0, 0.0, 0.0)); prev[(sd, idx)] = cum
    q = int(r['quarter']) + 1
    a = A.get((sd, q, idx))
    if a is None: continue
    yr, name, alive, gdp, war = a
    if alive != 1 or not (gdp > 0): continue
    wi = win(yr)
    if wi is None: continue
    bor = cum[0] - pv[0]; dev = cum[1] - pv[1]; mil = -((cum[2] - pv[2]) + (cum[3] - pv[3])); debt = num(r['govDebtStock'])
    v = acc[name][sd][wi]; g = gdp / 4
    v[0] += g; v[1] += mil; v[2] += bor; v[11] += 1
    if war: v[3] += g; v[4] += mil; v[5] += bor; v[6] += dev; v[10] += 1
    if q >= v[7]: v[7] = q; v[8] = debt; v[9] = gdp


def med(x):
    x = [y for y in x if y == y]
    return st.median(x) if x else float('nan')


def judge(s, h, floor):
    # ★(182) 反証役 vm2#1：README §1-2 の式＝シミュ÷max(史実, 床)（床は史実の側だけ・両方が床の下なら可）。旧はシミュの側にも床を掛けた＝史実が床の数倍以下の窓で「少なすぎ」の NG が出なかった。
    if s != s: return 'nan', '—'
    if s < floor and h < floor: return '床の下', '可'
    r = s / max(h, floor)
    return f'{r:.2f}', ('NG' if (r >= 10 or r <= 0.1) else '記録' if (r >= 3 or r <= 1 / 3) else '可')


seeds = sorted({k[0] for k in A})
print(f'######## {LAB}（種 {len(seeds)}・10年の窓・種ごとの和の比→種の中央・%）')
print('国 | 窓 | 軍÷GDP 全 | 軍÷GDP 戦時 | 戦時の四半期の割合 | 借入÷GDP 全 | 借入÷GDP 戦時 | 切り下げ÷GDP 戦時 | 国債÷GDP（窓の終わり）')
ngs = []
for n in NAT:
    for wi, (y0, y1) in enumerate(WIN):
        rows = [acc[n][sd][wi] for sd in seeds if acc[n][sd][wi][11] > 0]
        if not rows: continue
        mA = med([100 * v[1] / v[0] for v in rows if v[0] > 0]); mW = med([100 * v[4] / v[3] for v in rows if v[3] > 0])
        bA = med([100 * v[2] / v[0] for v in rows if v[0] > 0]); bW = med([100 * v[5] / v[3] for v in rows if v[3] > 0])
        dW = med([100 * v[6] / v[3] for v in rows if v[3] > 0]); wq = med([v[10] / v[11] for v in rows])
        dl = med([100 * v[8] / v[9] for v in rows if v[9] > 0])
        cells = []
        for H, val, fl_, lab in ((HM, mA, 1.0, '軍÷GDP'), (HB, bA, 1.0, '借入÷GDP'), (HD, dl, 5.0, '国債÷GDP')):
            h = H.get((n, y0 if y0 != 1783 else 1780))
            if h is None: continue
            r, j = judge(val, h, fl_)
            cells.append(f'{lab} 史実{h}→比{r} {j}')
            if j == 'NG': ngs.append(f'{n} {y0}-{y1} {lab} {val:.2f}%（史実 {h}%・比 {r}）')
        print(f'{n} | {y0}-{y1} | {mA:.2f} | {mW:.2f} | {wq:.2f} | {bA:.2f} | {bW:.2f} | {dW:.2f} | {dl:.1f}' + ('  ← ' + '・'.join(cells) if cells else ''))
print('\n# 史実比較の桁の NG（10年の窓の平均・史実【推】）')
for x in ngs: print('  ' + x)
if not ngs: print('  なし')
