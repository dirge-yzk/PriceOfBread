"""(171) 史実比較の桁（新しく読む量・読むだけ）＝10年の窓・10種の中央：
  ①兵士÷人口の最大（窓の中の四半期の最大・A 行）②軍事費÷年GDP（窓の和・台帳の陸＋海÷GDP の和・Q 行と A 行）③借入÷年GDP（窓の和・台帳の借入）
  ④戦死÷交戦の兵士の月（%/月・cas の Y 行・世界の和）⑤列強3以上の塊の長さの最大（月・w171）
史実【推・記憶の概数】＝下の H 表。判定＝比（シミュ÷史実）10倍以上か1/10以下で NG・3〜10倍は記録。床＝兵÷人口 0.1%・軍÷GDP 1%・借入÷GDP 1%。
python an_hist171.py <計器の出力ディレクトリ> [ラベル] [--seeds N]（(245) 旧の注入の行を標準の計器 SimCore/Instruments.cs の表へ畳んだ）"""
import sys, os, statistics as st, collections
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import rows, num
args = sys.argv[1:]; maxsd = 99
if '--seeds' in args:
    i = args.index('--seeds'); maxsd = int(args[i + 1]); del args[i:i + 2]
D = args[0]; LAB = args[1] if len(args) > 1 else D
W = (1910, 1919)
NAT = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'オーストリア', 'アメリカ']
# 史実【推】1910-19：兵士÷人口の最大%（大戦の頂点）・軍事費÷年GDP%（10年の平均）・借入÷年GDP%（10年の平均）
H = {'イギリス': (8.0, 20.0, 15.0), 'フランス': (12.0, 25.0, 15.0), 'プロイセン': (11.0, 25.0, 15.0), 'ロシア': (4.0, 15.0, 8.0), 'オーストリア': (9.0, 15.0, 10.0), 'アメリカ': (4.0, 4.0, 4.0)}
FL = (0.1, 1.0, 1.0)


A = collections.defaultdict(list)   # (sd, name) -> [(q+1, gdp, pop, sold)]　A の四半期は 1 始まり（旧の A 行の q+1）
Qd = collections.defaultdict(dict)   # (sd,name) -> q -> (army+navy 累計, borrow 累計)　⚠Qd の q は 0 始まり（旧の Q 行の q）＝A の窓と1四半期ずれて引く（旧のまま・恒等のため直していない）
for sd, r in rows(D, 'nation', maxsd):
    if r['nation'] not in NAT: continue
    Qd[(sd, r['nation'])][int(r['quarter'])] = (-(num(r['ledgerArmyCumulative']) + num(r['ledgerNavyCumulative'])), num(r['ledgerBorrowCumulative']))
    y = int(r['year'])
    if num(r['totalPopulationK']) > 0 and W[0] <= y <= W[1]: A[(sd, r['nation'])].append((int(r['quarter']) + 1, num(r['nominalGdpPerYear']), num(r['populationK']), num(r['soldierK'])))
seeds = sorted({k[0] for k in A})


def judge(s, h, floor):
    # ★(182) 反証役 vm2#1：README §1-2 の式＝シミュ÷max(史実, 床)（床は史実の側だけ・両方が床の下なら可）。旧はシミュの側にも床を掛けた＝史実が床の数倍以下の窓で「少なすぎ」の NG が出なかった。
    if s != s: return 'nan', '—'
    if s < floor and h < floor: return '床の下', '可'
    r = s / max(h, floor)
    return f'{r:.2f}', ('NG' if (r >= 10 or r <= 0.1) else '記録' if (r >= 3 or r <= 1 / 3) else '可')


print(f'######## {LAB}（種 {len(seeds)}・窓 {W[0]}-{W[1]}・種の中央）')
print('国 | 兵士÷人口の最大% | 史実 | 比 | 判定 | 軍事費÷年GDP% | 史実 | 比 | 判定 | 借入÷年GDP% | 史実 | 比 | 判定')
for n in NAT:
    mx, mg, bg = [], [], []
    for sd in seeds:
        v = A.get((sd, n))
        if not v: continue
        mx.append(max(100 * x[3] / x[2] for x in v if x[2] > 0))
        qs = sorted(x[0] for x in v); q0, q1 = qs[0], qs[-1]
        qq = Qd.get((sd, n), {})
        if q0 - 1 in qq and q1 in qq:
            gdp = sum(x[1] / 4 for x in v)
            mg.append(100 * (qq[q1][0] - qq[q0 - 1][0]) / gdp); bg.append(100 * (qq[q1][1] - qq[q0 - 1][1]) / gdp)
    out = [n]
    for val, k in ((mx, 0), (mg, 1), (bg, 2)):
        s = st.median(val) if val else float('nan'); r, j = judge(s, H[n][k], FL[k])
        out += [f'{s:.2f}', f'{H[n][k]}', r, j]
    print(' | '.join(out))
# ④ 戦死の率（10年ごと・世界）
per = collections.defaultdict(lambda: collections.defaultdict(lambda: [0.0, 0.0]))
for sd, r in rows(D, 'casualty', maxsd):
    a = per[int(r['year']) // 10 * 10][sd]; a[0] += float(r['soldierMonthsK']); a[1] += float(r['deadK'])
HR = {1790: 0.8, 1800: 0.8, 1810: 0.8, 1850: 0.9, 1860: 0.8, 1870: 1.0, 1910: 0.8}
print('\n# 戦死÷交戦の国の兵士の月（%/月・10年の窓・種の中央）／史実【推】＝その10年の主な戦争（ナポレオン戦争・クリミア・南北戦争・普仏戦争・第一次大戦）の戦死÷（平均の兵力×月数）')
for d in sorted(per):
    if d not in HR: continue
    v = [100 * a[1] / a[0] for a in per[d].values() if a[0] > 0]
    s = st.median(v); r, j = judge(s, HR[d], 0.01)
    print(f'{d}年代 | {s:.3f} | 史実 {HR[d]} | 比 {r} | {j}')
