# (960) c85tx 読むだけの台本：旗 0 の生（keep_947/base_ins の money_s*.tsv）から、
# AI の規則 A の〈比例→定額〉の側の〈置くはずの目標〉（期待インフレ < 0 なら定額）の反転の回数を国×種で数える。
# 凍結の額は main の生に無い＝定額→比例の側（規則 A と B の違い）は数えられない（限界）。keep_947 は読むだけ。
import sys, os, statistics
RAW = sys.argv[1]
OUT = sys.argv[2]
rows = []
for s in range(1, 11):
    p = os.path.join(RAW, 'money_s%d.tsv' % s)
    with open(p, 'rb') as f:
        lines = f.read().decode('utf-8').split('\n')
    head = lines[0].rstrip('\r').split('\t')
    iN, iY, iT, iP = head.index('nation'), head.index('year'), head.index('time'), head.index('expectedInflationPerYear')
    ser = {}
    for ln in lines[1:]:
        if not ln.strip():
            continue
        c = ln.rstrip('\r').split('\t')
        ser.setdefault(c[iN], []).append((c[iT], int(c[iY]), float(c[iP])))
    for nat, v in ser.items():
        neg = [x[2] < 0 for x in v]
        flips = sum(1 for a, b in zip(neg, neg[1:]) if a != b)
        negq = sum(neg)
        zero = sum(1 for x in v if x[2] == 0)
        nan = sum(1 for x in v if x[2] != x[2])
        rows.append((nat, s, len(v), flips, negq, zero, nan))
out = ['# r960read：規則 A の比例→定額の側の〈置くはずの目標〉（期待インフレ<0＝定額）の反転の回数（国×種・行は四半期・助走の行を含む）',
       '# 列: nation seed rows flips negQuarters zeroQuarters nanQuarters']
for r in sorted(rows):
    out.append('\t'.join(str(x) for x in r))
out.append('# 国ごとの反転の回数（10 種の中央〔最小,最大〕）・期待インフレ<0 の四半期の割合の中央')
nats = sorted(set(r[0] for r in rows))
allmax = 0
for n in nats:
    fl = [r[3] for r in rows if r[0] == n]
    sh = [r[4] / r[2] for r in rows if r[0] == n]
    allmax = max(allmax, max(fl))
    out.append('%s\t%s〔%d,%d〕\t%.2f' % (n, statistics.median(fl), min(fl), max(fl), statistics.median(sh)))
out.append('# 国×種の最大 %d・国×種の数 %d' % (allmax, len(rows)))
with open(OUT, 'wb') as f:
    f.write(('\n'.join(out) + '\n').encode('utf-8'))
