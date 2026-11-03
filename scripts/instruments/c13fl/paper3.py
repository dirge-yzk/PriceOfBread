# (810) c13fl 紙の台本 3（読むだけ）：形 (a) yH の利札・(b) 未払いと落とす請求権・O の一次の予言・(c) の項の大きさを、旗 0 の盤面の生で数える。
# 使い方：python paper3.py <ins フォルダ> <種 1-10 か 14,15,16,17> <inv_income の tsv（O の分母）|-> [国 種 年0 年1]
# 一次の予言＝盤面の量（fci・sav・rent）をそのまま使い、軌道の変化（落とした請求権で次の四半期の fci が縮む等）は入れない。
# K_ind≈meanIndustrialization（score_s）×12×earnedTotalPerMonth（nation_s）＝国の近似（paper2.py と同じ限界）。
# 利札 claim＝rent_q×fci÷(fci＋sav＋K)・払い＝min(claim, sav)・未払い＝claim−払い・落とす請求権＝min(未払い, fci)。
# (c) の項＝PolicyRateCapitalDraw(1.0)×4×払い÷fci×信用（信用は divhost に無い＝1 と置いた上側）。
import sys, csv, os, collections
d = sys.argv[1]; spec = sys.argv[2]; invf = sys.argv[3]
seeds = list(range(int(spec.split('-')[0]), int(spec.split('-')[1]) + 1)) if '-' in spec else [int(x) for x in spec.split(',')]
N = len(seeds)
focus = sys.argv[4:8] if len(sys.argv) >= 8 else None
NINE = {'清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本', 'アメリカ'}
dec = collections.defaultdict(lambda: [0.0] * 6)   # 年代 → 旧の払い・新の払い・新の未払い・落とす請求権・fci の和（全て金建て・種の平均）・旧の請求
nine_new = 0.0; nine_old = 0.0
rows = 0; unpaid_rows = 0; wipe_rows = 0; term = []
frows = []
for s in seeds:
    ind = {}; earn = {}; last = {}
    for row in csv.DictReader(open(os.path.join(d, f'score_s{s}.tsv'), encoding='utf-8'), delimiter='\t'): ind[(int(row['year']), row['nation'])] = float(row['meanIndustrialization'])
    for row in csv.DictReader(open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8'), delimiter='\t'): earn[(row['quarter'], row['nation'])] = float(row['earnedTotalPerMonth'])
    for row in csv.DictReader(open(os.path.join(d, f'divhost_s{s}.tsv'), encoding='utf-8'), delimiter='\t'): last[(row['quarter'], row['nation'])] = row
    for (q, nat), row in last.items():
        if int(float(row['rentMonths'])) != 3: continue
        e = float(row['fxRate']); rent = 3 * float(row['rentMonth3']); fci = float(row['fci']); sav = float(row['invSav']); yr = int(row['year'])
        paid_old = float(row['paidIndex']); req_old = float(row['divReqIndex'])
        if not (fci > 0): continue
        mi = ind.get((yr, nat)); ea = earn.get((q, nat))
        K = mi * 12 * ea if (mi is not None and ea is not None) else 0.0
        claim = rent * fci / (fci + sav + K) if rent > 0 else 0.0
        paid = min(claim, max(0.0, sav)); unpaid = claim - paid; wo = min(unpaid, fci)
        rows += 1
        if unpaid > 1e-9 * max(1.0, fci): unpaid_rows += 1
        if wo >= 0.999 * fci: wipe_rows += 1
        if paid > 0: term.append(4 * paid / fci)
        a = dec[yr // 10 * 10]; a[0] += paid_old * e / N; a[1] += paid * e / N; a[2] += unpaid * e / N; a[3] += wo * e / N; a[4] += fci * e / N; a[5] += req_old * e / N
        if nat in NINE and 1850 <= yr <= 1949: nine_new += paid * e / N; nine_old += paid_old * e / N
        if focus and nat == focus[0] and s == int(focus[1]) and int(focus[2]) <= yr <= int(focus[3]):
            frows.append((int(q), yr, fci, sav, rent, K, claim, paid, wo))
print(f'盤面 {d}・種 {seeds}：fci>0 の行 {rows}・新の形で未払いが出る行 {unpaid_rows}（{unpaid_rows / max(1, rows):.4f}）・1 四半期で fci を消す行 {wipe_rows}')
print('年代\t旧の払い\t新の払い（一次）\t新の未払い\t落とす請求権\tfci の和\t落とす÷fci\t旧の請求')
for y in sorted(dec):
    a = dec[y]
    print(f'{y}\t{a[0]:.4g}\t{a[1]:.4g}\t{a[2]:.4g}\t{a[3]:.4g}\t{a[4]:.4g}\t{a[3] / a[4] if a[4] > 0 else 0:.4f}\t{a[5]:.4g}')
if term:
    term.sort(); m = len(term)
    print(f'(c) の項の上側（年の率・信用 1 と置く）：p50 {term[m // 2]:.4g}・p90 {term[m * 9 // 10]:.4g}・p99 {term[m * 99 // 100]:.4g}・最大 {term[-1]:.4g}・払いのある行 {m}')
if invf != '-':
    op = 0.0; dv = 0.0
    for line in open(invf, encoding='utf-8'):
        c = line.rstrip('\n').split('\t')
        if c[0] == '9国計' and c[1].isdigit() and 1850 <= int(c[1]) <= 1940: op += float(c[2]); dv += -float(c[7])
    print(f'O（9 国・1850〜1949）：旗 0 の実際 {dv / op:.4f}（flow_s の div {dv:.6g}・受け取り {op:.6g}）／新の一次の予言 {nine_new / op:.4f}（divhost の新の払い {nine_new:.6g}）／divhost の旧の払い {nine_old:.6g}（flow_s の div との比 {nine_old / dv if dv else 0:.3f}）')
if focus:
    print(f'■{focus[0]} 種 {focus[1]} {focus[2]}〜{focus[3]}（四半期・年・fci・sav・rent_q・K・新の請求・新の払い・落とす請求権）')
    for t in sorted(frows): print('  ' + '\t'.join(f'{v:.4g}' if isinstance(v, float) else str(v) for v in t))
