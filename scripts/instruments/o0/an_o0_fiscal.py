"""(283) 線O0 の読み手の足し（読むだけ）：(279) §5-3 b)c) と §5-2 行4 の事前登録のうち an_o0.py に無かった3つの数を fiscal_s（月ごと）から出す。
python an_o0_fiscal.py <計器の出力ディレクトリ（…/ins）> [ラベル]
入力＝fiscal_s<seed>.tsv（POB_INSTRUMENTS_NATIONS に挙げた国の月ごと＝帯の計器つきは英・仏・蘭・葡）。列は SimCore/Instruments.cs の FiscalRow。
出す表（10種の和・国ごと）：
 ① 仏と蘭（と英・葡）の門閉＋不足の月＝borrowBinder が gateClosed の月（払えない額があって借入の門が閉じていた月）。種ごとの月数も
 ② ゾンビの月（門に依らない定義＝interestAtLimitFlag＝1・payRateShare≧1・(借入＋切り下げ)＞0）の月数・うち払えない額＞0（unpaidBeforeDevaluationShare＞0）の月数
    ・うち払えない額＝0（当座借越だけを切り下げで埋めた月）・ゾンビの月の切り下げ÷請求（devaluedToClaimShare）の中央と四分位。
    ⚠月数は nation_s の zombieMonthsCount の和と突き合わせる（自己検査）＝(283) 実測で行の数の方が多い：fiscal_s の r（payRateShare）は G10 に丸めて出る
    ＝切り下げが払えない額をちょうど埋めた月の r＝1−ε が「1」になる。nation_s の列（C# の r≧1 の厳密な比較）は r＝1−ε の月を落とす＝この表の月数は〈r が 1 に丸まる月〉を含む上限
 ③ 不履行の2区分：defaultCount が増えた月ごとに、その月と直前の連続の月（insolvencyMonths＞0 が続いた月＝計数が積もった窓）で
    Σ切り下げ（devaluedPerMonth）と Σ切り下げの後に払えなかった額（claimTotalPerMonth×(1−payRateShare)）を比べ、
    インフレで払った分が上回る〈インフレ区分〉か、そうでない〈払えない区分〉かに分ける。
⚠ 計器の自己申告の限界：①挙げた4国だけ（世界の不履行 107 の大半は fiscal_s に無い）②窓は insolvencyMonths の連続（計数が 0 へ戻った月で切れる）＝
  不履行の月の行の insolvencyMonths は 0 へ戻った後の値なので窓に含めて数える ③払えなかった額は切り下げの後の (1−r)×請求（計数の入力と同じ）
  ④試合の最初の月の行は無い（月の決算の写しは助走の後の最初の月から）＝最初の月の不履行は窓の頭が欠ける。"""
import sys, os, collections
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import rows, seeds as seeds_of, num

D = sys.argv[1]; LAB = sys.argv[2] if len(sys.argv) > 2 else D
seeds = seeds_of(D, 'fiscal')
by = collections.defaultdict(list)   # (seed, nation) -> 行（ファイルの順）
for sd, r in rows(D, 'fiscal'):
    by[(sd, r['nation'])].append(r)
nats = sorted({n for (_, n) in by})

def pos(x):
    return x if x == x and x > 0 else 0.0

def quant(v, q):
    if not v: return float('nan')
    s = sorted(v); i = q * (len(s) - 1); lo = int(i); hi = min(lo + 1, len(s) - 1)
    return s[lo] + (s[hi] - s[lo]) * (i - lo)

gate = collections.Counter(); gate_seed = collections.defaultdict(collections.Counter)
zomb = collections.Counter(); zomb_unpaid = collections.Counter(); zomb_od = collections.Counter(); zomb_dev = collections.defaultdict(list)
d_infl = collections.Counter(); d_unpaid = collections.Counter(); d_detail = collections.defaultdict(list)
for (sd, nat), rs in by.items():
    rs.sort(key=lambda r: int(r['monthIndex']))
    prev_dc = None
    for i, r in enumerate(rs):
        if r['borrowBinder'] == 'gateClosed':
            gate[nat] += 1; gate_seed[nat][sd] += 1
        lim = num(r['interestAtLimitFlag']); pr = num(r['payRateShare'])
        if lim == 1 and pr >= 1 and pos(num(r['borrowedPerMonth'])) + pos(num(r['devaluedPerMonth'])) > 0:
            zomb[nat] += 1
            u = num(r['unpaidBeforeDevaluationShare'])
            if u == u and u > 0: zomb_unpaid[nat] += 1
            elif u == 0: zomb_od[nat] += 1
            dv = num(r['devaluedToClaimShare'])
            if dv == dv: zomb_dev[nat].append(dv)
        dc = num(r['defaultCount'])
        if prev_dc is not None and dc == dc and dc > prev_dc:
            # 窓＝この月と、直前の insolvencyMonths＞0 が続いた月
            j = i - 1
            while j >= 0 and num(rs[j]['insolvencyMonths']) > 0: j -= 1
            win = rs[j + 1:i + 1]
            dev = sum(pos(num(x['devaluedPerMonth'])) for x in win)
            unp = sum(pos(num(x['claimTotalPerMonth'])) * pos(1 - num(x['payRateShare'])) for x in win)
            kind = 'インフレ' if dev > unp else '払えない'
            (d_infl if kind == 'インフレ' else d_unpaid)[nat] += int(dc - prev_dc)
            d_detail[nat].append((sd, int(r['monthIndex']), len(win), dev, unp, kind))
        if dc == dc: prev_dc = dc

# 自己検査：ゾンビの月数を nation_s の zombieMonthsCount の和と突き合わせる
zn = collections.Counter()
for sd, r in rows(D, 'nation'):
    if r['nation'] in nats:
        v = num(r.get('zombieMonthsCount', 'nan'))
        if v == v: zn[r['nation']] += int(v)

print(f'##### {LAB}（fiscal_s {len(seeds)}種：{seeds}・国 {nats}）')
print('① 門閉＋不足の月（borrowBinder＝gateClosed）・10種の和（種ごと）')
for n in nats:
    print(f'  {n}: {gate[n]}（' + ', '.join(f's{sd} {gate_seed[n][sd]}' for sd in seeds) + '）')
print('② ゾンビの月（限界に届いた・r＝1〔G10 に丸めた値〕・(借入＋切り下げ)＞0）：月数／うち払えない額＞0／うち払えない額＝0（借越だけ）／nation_s の zombieMonthsCount の和（自己検査・r≧1 の厳密な比較）／切り下げ÷請求の 25%・中央・75%')
for n in nats:
    v = zomb_dev[n]
    ok = '一致' if zomb[n] == zn[n] else '不一致＝r の丸め'
    print(f'  {n}: {zomb[n]} / {zomb_unpaid[n]} / {zomb_od[n]} / {zn[n]}（{ok}）/ {quant(v, 0.25):.4f}・{quant(v, 0.5):.4f}・{quant(v, 0.75):.4f}')
print('③ 不履行の2区分（窓＝不履行の月と直前の insolvencyMonths＞0 の連続）：インフレ区分（Σ切り下げ＞Σ(1−r)×請求）／払えない区分')
for n in nats:
    print(f'  {n}: インフレ {d_infl[n]}・払えない {d_unpaid[n]}')
    for sd, m, L, dev, unp, kind in d_detail[n]:
        print(f'    s{sd} 月{m} 窓{L}か月 切り下げ {dev:.1f} 払えない {unp:.1f} → {kind}')
