"""(354) 流れM 切り替え後＝戦時段の解禁の段0 の読み手（読むだけ）。
python an_m6_war.py <ins のフォルダ（nation_s*.tsv と fiscal_s*.tsv）> [国=イギリス,フランス,アメリカ,プロイセン,日本,ロシア]      （PYTHONUTF8=1）
入力＝標準の計器（`INS=1 POB_INSTRUMENTS_NATIONS=… bash batch.sh run <腕> N` の <出力>/ins）の nation_s（四半期・全ての国）と fiscal_s（月・挙げた国）。
出す物（窓ごと・国ごと・種ごと〔種の中央ではない〕）：
 W1 交戦中の四半期の割合・その中で 総動員（mobilizedAtDecisionFlag）・戦時段の解禁（warStageOpenAfterGovFlag）・AI が最上段を選んだ（aiPickedWarStageFlag）の割合
 W2 解禁を〈交戦中〉へ広げたら増える四半期（腕 S＝規模≧1/1.2 か 存亡＞0 の四半期・腕 S2＝規模≧1/1.2 の四半期）の割合と、規模・存亡の分布（≧1.2 の上げる線・存亡＞0.5 の線）
 W3 交戦中の四半期の軍事費の段（目標・施行）の平均と、軍事費の施行の割合
 W4 国庫の蓄え÷年GDP（窓の始まりの月）・戦費の帳簿（陸＋海＋戦費）÷年GDP・借入÷年GDP（窓の合計÷窓の年数）
限界の自己申告：規模・存亡は AI が決める時点の値（warScaleRatioAtDecision・homeLosingScoreAtDecision＝NaN は数えない）・窓の境の四半期の割り方は年で切る・種 4 本（種の中央ではない）・
腕 S・S2 の増える四半期は〈解禁が開いた〉だけを数える＝施行の登りと国庫の蓄えと借入の門が通るかは別（(203) §1）。"""
import sys, os, glob, csv, math, statistics
D = sys.argv[1]
NAT = (sys.argv[2] if len(sys.argv) > 2 else 'イギリス,フランス,アメリカ,プロイセン,日本,ロシア').split(',')
WINS = [('1783-1849', 1783, 1849), ('1850-1913', 1850, 1913), ('1914-1918', 1914, 1918), ('1939-1945', 1939, 1945)]
LINE_UP, LINE_DOWN, LOSE_UP = 1.2, 1 / 1.2, 0.5

def fl(x):
    try: return float(x)
    except Exception: return float('nan')

_C = {}
def rd(p):
    if p not in _C:
        with open(p, encoding='utf-8', newline='') as f:
            _C[p] = list(csv.DictReader(f, delimiter='\t'))
    return _C[p]

def pct(a, b): return (a / b * 100) if b else float('nan')

seeds = sorted(glob.glob(os.path.join(D, 'nation_s*.tsv')), key=lambda p: int(os.path.basename(p)[8:-4]))
print(f'#### 入力 {len(seeds)} 種  国 {NAT}')
for nat in NAT:
    for (wn, y0, y1) in WINS:
        rows = []
        for p in seeds:
            sd = os.path.basename(p)[8:-4]
            q = [r for r in rd(p) if r['nation'] == nat and y0 <= int(r['year']) <= y1]
            nq = len(q); war = [r for r in q if fl(r['atWarAtDecisionFlag']) == 1]
            nw = len(war)
            if nw == 0:
                rows.append(f"  種{sd}: 四半期{nq}・交戦中 0"); continue
            mob = sum(1 for r in war if fl(r['mobilizedAtDecisionFlag']) == 1)
            opn = sum(1 for r in war if fl(r['warStageOpenAfterGovFlag']) == 1)
            pick = sum(1 for r in war if fl(r['aiPickedWarStageFlag']) == 1)
            sc = [fl(r['warScaleRatioAtDecision']) for r in war]; lo = [fl(r['homeLosingScoreAtDecision']) for r in war]
            up = sum(1 for s, l in zip(sc, lo) if (s >= LINE_UP) or (l > LOSE_UP))
            sS = sum(1 for r, s, l in zip(war, sc, lo) if fl(r['warStageOpenAfterGovFlag']) != 1 and ((s >= LINE_DOWN) or (l > 0)))
            sS2 = sum(1 for r, s, l in zip(war, sc, lo) if fl(r['warStageOpenAfterGovFlag']) != 1 and (s >= LINE_DOWN))
            tgt = statistics.mean(fl(r['militaryStageTargetEndStage']) for r in war)
            enf = statistics.mean(fl(r['militaryStageEnforcedEndStage']) for r in war)
            enf4 = sum(1 for r in war if fl(r['militaryStageEnforcedEndStage']) >= 4) # 段4＝戦時段の施行
            ms = statistics.mean(fl(r['militarySpendingEndShare']) for r in war)
            rows.append(f"  種{sd}: 四半期{nq}・交戦中 {nw}（{pct(nw,nq):.0f}%）| 総動員 {pct(mob,nw):.0f}%・解禁 {pct(opn,nw):.0f}%・AI が最上段を選んだ {pct(pick,nw):.0f}% | 上げる線以上〔規模≧1.2 か 存亡＞0.5〕{pct(up,nw):.0f}%・"
                        f"S の増分 {pct(sS,nw):.0f}%・S2 の増分 {pct(sS2,nw):.0f}% | 段 目標{tgt:.2f}・施行{enf:.2f}（施行が段4 {pct(enf4,nw):.0f}%）・軍事費の施行の割合 {ms:.3f}")
        print(f'== {nat} {wn}'); print('\n'.join(rows))

print('\n#### W4 国庫・戦費・借入（fiscal_s・月）')
for nat in NAT:
    for (wn, y0, y1) in [('1900-1913', 1900, 1913), ('1914-1918', 1914, 1918), ('1930-1938', 1930, 1938), ('1939-1945', 1939, 1945)]:
        out = []
        for p in sorted(glob.glob(os.path.join(D, 'fiscal_s*.tsv')), key=lambda p: int(os.path.basename(p)[8:-4])):
            sd = os.path.basename(p)[8:-4]
            nsp = os.path.join(D, f'nation_s{sd}.tsv')
            gdp = {}
            for r in rd(nsp):
                if r['nation'] == nat: gdp[int(r['year'])] = fl(r['nominalGdpPerYear'])
            m = [r for r in rd(p) if r['nation'] == nat and y0 <= int(r['year']) <= y1]
            if not m: continue
            g = statistics.mean(gdp[y] for y in range(y0, y1 + 1) if y in gdp and gdp[y] == gdp[y])
            first = fl(m[0]['treasuryEndStock'])
            yrs = (y1 - y0 + 1)
            mil = sum(fl(r['ledgerArmyPerMonth']) + fl(r['ledgerNavyPerMonth']) + fl(r['ledgerWarCostPerMonth']) for r in m) / yrs / g * 100
            bor = sum(fl(r['ledgerBorrowPerMonth']) for r in m) / yrs / g * 100
            out.append(f"種{sd}: 国庫÷年GDP {first/g:.2f}・戦費の帳簿÷年GDP {mil:.1f}%・借入÷年GDP {bor:.1f}%")
        print(f'== {nat} {wn}: ' + ' | '.join(out))
