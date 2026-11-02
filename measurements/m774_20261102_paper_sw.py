# (774) 波57 c7sw の紙の台本（既存の表だけで取れる数）。入力＝基線 m773 の計器つき 10 種の生（D:/PoB_work/keep_773/base_ins・n1・n2・a11）の nation_s・aib_s。
# 使い方：PYTHONUTF8=1 python measurements/m774_20261102_paper_sw.py [生の根=D:/PoB_work/keep_773]
# ⚠限界：既存の表に StateTarget は無い＝〈継いだ意志の型〉は数えられない。ここで数えるのは代用（proxy）＝後から生まれた国の、生まれて 12 四半期以内の国営の着工のうち、
#   AI の着工の決定の回（aib_s.startedCount＝SetStateTarget の呼び出しで着工した件）に入らなかった件（＝月次の再挑戦 StepStateTargets で着工した件）。AI 自身が拒まれて立てた意志の再挑戦も入る＝過大側。
#   正確な数は旗 0 の計器つき 10 種（ident の b 腕＝m773 の盤面）の inheritintent_s から取る（事前登録に〈紙の取り直し〉として追記する）。
import csv, sys, statistics, os
base = sys.argv[1] if len(sys.argv) > 1 else 'D:/PoB_work/keep_773'
def read(arm, name, seed, cols):
    out = []
    with open(f'{base}/{arm}/{name}_s{seed}.tsv', encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); idx = [h.index(c) for c in cols]
        for row in r: out.append([row[i] for i in idx])
    return out
def load(arm, seed):
    nat = {}
    for q, n, st, pl in read(arm, 'nation', seed, ['quarter', 'nation', 'stateFacilityStartedCount', 'priceLevel']):
        nat.setdefault(n, {})[int(q)] = (int(float(st)), float(pl) if pl not in ('nan', '') else float('nan'))
    aib = {}
    for q, n, st in read(arm, 'aib', seed, ['quarter', 'nation', 'startedCount']):
        aib.setdefault(n, {})[int(q)] = int(float(st))
    return nat, aib
print('# arm seed bornNations withStart12Q starts12Q aibStarts12Q retryProxy12Q openingNationStartsAllGame')
tot = {}
open_med = {}
for arm, seeds in (('base_ins', range(1, 11)), ('n1', range(1, 11)), ('n2', range(1, 11)), ('a11', range(11, 21))):
    sums = [0, 0, 0, 0]; opens = []
    for s in seeds:
        nat, aib = load(arm, s)
        nb = ws = st12 = ai12 = 0; op = 0
        for n, qs in nat.items():
            b = min(qs)
            if b == 0:
                op += sum(v[0] for v in qs.values()); continue
            nb += 1
            a = sum(v[0] for q, v in qs.items() if q - b < 12)
            ai = sum(v for q, v in aib.get(n, {}).items() if q - b < 12)
            st12 += a; ai12 += ai; ws += a > 0
        print(f'{arm} {s} {nb} {ws} {st12} {ai12} {st12 - ai12} {op}')
        sums[0] += ws; sums[1] += st12; sums[2] += ai12; sums[3] += st12 - ai12; opens.append(op)
    tot[arm] = sums; open_med[arm] = statistics.median(opens)
print('# 10 種の和（withStart12Q・starts12Q・aibStarts12Q・retryProxy12Q）')
for a, v in tot.items(): print(f'#   {a}: {v}')
print('# 開幕からの国の国営の着工（全期間）の種ごとの中央値（U2 の基準）')
for a, v in open_med.items(): print(f'#   {a}: {v}')
print('# 名指し（P 最大＝nation_s.priceLevel の全期間の最大・開幕の値を 1 としない絶対値）')
for arm, seed, nm in (('n2', 3, '小アンティル王国'), ('a11', 11, '小アンティル王国'), ('n2', 4, 'ジャマイカ王国'), ('base_ins', 6, 'ジャマイカ王国')):
    nat, _ = load(arm, seed)
    if nm not in nat: print(f'#   {arm} s{seed} {nm}: nation_s に無い'); continue
    qs = nat[nm]; b = min(qs); mx = max(qs, key=lambda q: qs[q][1] if qs[q][1] == qs[q][1] else -1)
    st = {q: v[0] for q, v in qs.items() if v[0] > 0}
    print(f'#   {arm} s{seed} {nm}: bornQ {b} Pmax {qs[mx][1]:.4g} (q{mx}) 国営の着工の四半期 {sorted(st.items())[:6]}')
# ---- U2・U2r の床（基線・N1・N2 の 30 個の種ごとの値の、下から 4 番目＝約 10 パーセンタイル・AI の置いた幅の定義）
pooled_tot = []; pooled_retry = []
for arm in ('base_ins', 'n1', 'n2'):
    for s in range(1, 11):
        nat, aib = load(arm, s)
        op = [n for n, qs in nat.items() if min(qs) == 0]
        t = sum(sum(v[0] for v in nat[n].values()) for n in op); a = sum(sum(aib.get(n, {}).values()) for n in op)
        pooled_tot.append(t); pooled_retry.append(t - a)
pooled_tot.sort(); pooled_retry.sort()
print('# U2 の床（開幕からの国の国営の着工・全期間・30 個の下から 4 番目）', pooled_tot[3], ' 下位 6 個', pooled_tot[:6])
print('# U2r の床（開幕からの国の再挑戦の着工＝nation_s−aib・30 個の下から 4 番目）', pooled_retry[3], ' 下位 6 個', pooled_retry[:6], ' 中央値', statistics.median(pooled_retry))
print('# L1p の上限（代用 retryProxy ではなく nation_s の starts12Q の和・基線の 1/3 の切り捨て）', tot['base_ins'][1] // 3)
