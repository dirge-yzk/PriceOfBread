# (910) c78ep 腕 B の G1′ の破れの内訳（読むだけ・結果の後の診断＝門は変えない）
#   python -X utf8 diag910.py <ins フォルダ> [--seeds 1 10]
#   G1′ の行（空の商人・debt − inCredit ＞1e-9×max(1, debt)）を、その四半期の inMarket と debt で分ける：
#     a＝inMarket ≧ debt（四半期の所得で返し切れたはず＝実装か時点の食い違いの疑い）／b＝inMarket ＜ debt（所得が債務に届かない＝紙の C と同じ型）
#   年代（50 年ごと）・同じ鍵の続きの長さ・inMarket＝0 の行の数も出す。
import sys, os
from collections import defaultdict
args = sys.argv[1:]
s0, s1 = 1, 10
if '--seeds' in args:
    i = args.index('--seeds'); s0, s1 = int(args[i + 1]), int(args[i + 2]); del args[i:i + 3]
d = args[0]; DEAD = 1e-12
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
a = b = z = an = asv = 0; era = defaultdict(lambda: [0, 0]); runs = []; emp = 0
for s in range(s0, s1 + 1):
    p = os.path.join(d, f'cpop_s{s}.tsv')
    if not os.path.exists(p): continue
    ser = defaultdict(list)
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            x = line.rstrip('\n').split('\t')
            if x[ix['className']] != '商人' or fl(x[ix['popK']]) > DEAD: continue
            emp += 1
            db, cr, mk, yr = fl(x[ix['debt']]), fl(x[ix['inCreditPerQuarter']]), fl(x[ix['inMarketPerQuarter']]), int(x[ix['year']])
            carry = db - cr > 1e-9 * max(1.0, db)
            ser[(x[ix['region']], x[ix['popIndex']])].append((int(x[ix['quarter']]), carry))
            e = era[(yr - 1783) // 50]; e[1] += 1
            if carry:
                e[0] += 1
                if mk >= db:
                    a += 1
                    if mk - fl(x[ix['outMarketPerQuarter']]) < db: an += 1   # a のうち純の所得（inMarket − outMarket）が債務に届かない行
                    if fl(x[ix['savings']]) >= db: asv += 1   # a のうち四半期の末の貯蓄が債務以上（返す原資は手元にある＝次の相続の段で返る時点の型）
                else: b += 1
                if not (mk > 0): z += 1
    for v in ser.values():
        run = 0; last = None
        for q, c in v:
            if c and (last is None or q == last + 1) and run: run += 1
            elif c:
                if run: runs.append(run)
                run = 1
            else:
                if run: runs.append(run)
                run = 0
            last = q
        if run: runs.append(run)
runs.sort()
med = runs[len(runs) // 2] if runs else 0
print(f'空の商人の行 {emp}・G1′ の行 {a + b}（a inMarket≧debt {a}〔その中で純の所得 inMarket−outMarket ＜ debt {an}・末の貯蓄 ≧ debt {asv}〕・b inMarket＜debt {b}・その中で inMarket＝0 {z}）')
print('年代（1783 からの 50 年ごと）：' + '・'.join(f'{1783 + 50 * k}〜 {c}/{n}（{c / n:.3f}）' for k, (c, n) in sorted(era.items())))
print(f'持ち越しの続き {len(runs)} 本・中央 {med}・90 点 {runs[int(0.9 * (len(runs) - 1))] if runs else 0}・最大 {runs[-1] if runs else 0}・1 四半期だけ {sum(1 for r in runs if r == 1)}')
