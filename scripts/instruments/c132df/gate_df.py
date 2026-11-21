# (1284) c132df：史実の桁の線と記録の読み手（形 (e) の C1＋C2＝兌換の停止の門を不履行の導火線と同じ述語にして判定の前へ）。
#   読む表：mintcap_s（全ての国・四半期・law と onGold）・nation_s（全ての国・四半期・sovereignDefaultCumulativeCount）・fiscal_s（計器の 9 国・月）。
#   エンジンの関数は呼ばない（表だけを読む）。
# 使い方: python -X utf8 -I gate_df.py <生（D:/… の ins）> <種0> <種1> <ラベル>
#
# 本位の停止の事象＝同じ国の続く四半期で onGold 1→0 かつ後の四半期の law が金属（1 銀・2 金）。
#   法を不換へ変えた離脱（law 0）は数えない（GoldSuspensions も数えない型）。不履行の RepudiateDebt の離脱は含む（四半期の表では分けられない＝記録に名乗る）。
#   四半期の表なので、1 四半期の内に離れて戻った往復は見えない（下側の数え）。
# 門（史実の桁の線・10 倍）：
#   G1 英の本位の停止の回数（種の平均）：史実 3（1797・1914・1931〈推〉）＝[0.3, 30] の外で赤。
#   G2 全ての国の本位の停止の回数（種の平均）：史実 約 50〈推〉（1797 英・1811 墺・1814 米・1837 米・1861 米・1866 伊・1914 の参戦国・1931〜36 の約 30 国ほか）＝[5, 500] の外で赤。
# 記録（向き）：英の不履行の種の数・1930 年代の全ての国の不履行（種の平均）・本位の外の続きの長さ（四半期の中央・最大）・英の最初の停止の年・計器の 9 国の新旧の述語の影（月）。
import sys, csv, collections, statistics
raw, s0, s1, lab = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
UK = 'イギリス'
def num(x):
    try: return float(x)
    except (TypeError, ValueError): return float('nan')
print(f'# (1284) gate_df {lab} {raw} 種 {s0}〜{s1}')
ns = s1 - s0 + 1
uk_susp = []; all_susp = []; uk_def_seeds = 0; d1930 = []; runs = []; uk_runs = []; uk_first = []; uk_years = []
shadow = collections.Counter()
for s in range(s0, s1 + 1):
    prev = {}; cnt = 0; ukc = 0; ukyrs = []; open_run = {}
    for r in csv.DictReader(open(f'{raw}/mintcap_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
        n = r['nation']; g = r['onGold']; law = r['law']; p = prev.get(n); prev[n] = (g, law)
        if p is not None and p[0] == '1' and g == '0' and law in ('1', '2'):
            cnt += 1; open_run[n] = 0
            if n == UK: ukc += 1; ukyrs.append(int(r['year']))
        elif g == '0' and n in open_run: open_run[n] += 1
        elif g == '1' and n in open_run:
            runs.append(open_run[n] + 1)
            if n == UK: uk_runs.append(open_run[n] + 1)
            del open_run[n]
    for n, k in open_run.items():   # 終わりまで戻らない続き（右の打ち切り）
        runs.append(k + 1)
        if n == UK: uk_runs.append(k + 1)
    all_susp.append(cnt); uk_susp.append(ukc); uk_years.append(ukyrs)
    if ukyrs: uk_first.append(ukyrs[0])
    hist = {}; ukd = 0; c30 = 0
    for r in csv.DictReader(open(f'{raw}/nation_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
        n = r['nation']; v = num(r['sovereignDefaultCumulativeCount']); p = hist.get(n); hist[n] = v
        if p is not None and v > p:
            if n == UK: ukd += 1
            if 1930 <= int(r['year']) <= 1939: c30 += 1
    uk_def_seeds += 1 if ukd > 0 else 0; d1930.append(c30)
    # 計器の 9 国の月：金本位の月に新の述語（r＜1・利払い÷歳入≧0.67）が真で旧（r＜0.7）が偽の月＝旗 1 で早く離れる月の影（助走と正貨カバーは見ない＝近似）
    try:
        for r in csv.DictReader(open(f'{raw}/fiscal_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
            if r['onGold'] != '1': continue
            pr = num(r['payRateShare']); itr = num(r['interestToRevenueShareNow']); dbt = num(r['govDebtStock'])
            new = pr < 1 and dbt > 1e-9 and itr >= 0.67; old = pr < 0.7
            shadow[('new', r['nation'])] += new; shadow[('old', r['nation'])] += old
            shadow['newOnly'] += new and not old; shadow['oldOnly'] += old and not new; shadow['both'] += new and old
    except FileNotFoundError:
        pass
mu = lambda xs: sum(xs) / len(xs) if xs else float('nan')
g1 = mu(uk_susp); g2 = mu(all_susp)
print(f'G1 英の本位の停止の回数 種の平均 {g1:.2f}（種ごと {uk_susp}）門 [0.3, 30] → {"緑" if 0.3 <= g1 <= 30 else "赤"}')
print(f'G2 全ての国の本位の停止の回数 種の平均 {g2:.2f}（種ごと {all_susp}）門 [5, 500] → {"緑" if 5 <= g2 <= 500 else "赤"}')
print(f'記録 英の停止の年 {uk_years}・最初の年の中央 {statistics.median(uk_first) if uk_first else "なし"}（史実 1797）')
print(f'記録 英の不履行の種の数 {uk_def_seeds}/{ns}')
print(f'記録 1930 年代の全ての国の不履行 種の平均 {mu(d1930):.2f}（種ごと {d1930}・史実 約 5〈推〉・モデルが少ない側）')
if runs: print(f'記録 本位の外の続き（四半期・右の打ち切りを含む）件 {len(runs)}・中央 {statistics.median(runs)}・最大 {max(runs)}・英 件 {len(uk_runs)} 中央 {statistics.median(uk_runs) if uk_runs else "-"} 最大 {max(uk_runs) if uk_runs else "-"}')
print(f'記録 計器の 9 国・金本位の月の述語の影（全種の和）：新だけ {shadow["newOnly"]}・旧だけ {shadow["oldOnly"]}・両方 {shadow["both"]}')
print('GATEDONE')
