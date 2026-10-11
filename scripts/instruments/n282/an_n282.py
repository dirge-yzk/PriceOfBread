"""(282) 線N：分母を揃える線の読み手（読むだけ）。python an_n282.py <計器の出力ディレクトリ> [出力ファイル]
入力＝nation_s<種>.tsv（INS=1 の標準の計器）。A（r279）と B（(282)）の両方に同じ読みを当てる。
  N＝政治予算の係数の分母。B は pushBudgetFrontsCount、A（その列が無い）は openFrontsCount（A では記録した開いた戦線の総数＝N）。
  Q1 段の法 L の資本固定の割合＝openFrontStates の 'L.…:2' ÷ 'L.…:*'（A も B も L の戦線を記録する）。
  記録 S頭／b の分布・払いの合計・連続の軸の払い。
"""
import csv, collections, statistics, sys, math

D = sys.argv[1]
OUT = sys.argv[2] if len(sys.argv) > 2 else None
lines = []
def P(s=''):
    lines.append(s)

def fl(x):
    try:
        return float(x)
    except Exception:
        return float('nan')

def mean(xs):
    xs = [x for x in xs if not math.isnan(x)]
    return sum(xs) / len(xs) if xs else float('nan')

def quant(xs, q):
    xs = sorted(x for x in xs if not math.isnan(x))
    return xs[min(len(xs) - 1, int(q * len(xs)))] if xs else float('nan')

rows = 0
cntN = []; factor = []; cont = []; lvl = []; sh = []; shfull = 0; shn = 0
L = collections.Counter(); K = collections.Counter()
Nhist = collections.Counter()
hasBud = False
for s in range(1, 11):
    try:
        f = open(f'{D}/nation_s{s}.tsv', encoding='utf-8', errors='replace')
    except FileNotFoundError:
        continue
    with f:
        for r in csv.DictReader(f, delimiter='\t'):
            rows += 1
            if 'pushBudgetFrontsCount' in r:
                hasBud = True
                n = fl(r['pushBudgetFrontsCount'])
            else:
                n = fl(r['openFrontsCount'])
            if n >= 0:
                cntN.append(n); Nhist[int(n)] += 1
            factor.append(fl(r['pushBudgetFactor']))
            cont.append(fl(r['continuousPaidInQuarter'])); lvl.append(fl(r['levelLawPaidInQuarter']))
            sd, mx = fl(r['politicalCapitalSpentAtLevelHead']), fl(r['politicalCapitalMaxAtLevelHead'])
            if mx > 0 and not math.isnan(sd):
                sh.append(sd / mx); shn += 1
                if sd >= mx * (1 - 1e-9):
                    shfull += 1
            for st in r['openFrontStates'].split(';'):
                if not st:
                    continue
                kind = st[0]; code = st.rsplit(':', 1)[1].rstrip('x')
                K[kind] += 1
                if kind == 'L':
                    L['all'] += 1
                    if code == '2':
                        L['fixed'] += 1
P('(282) 線N の読み  入力=%s  行=%d  N の列=%s' % (D, rows, 'pushBudgetFrontsCount（B）' if hasBud else 'openFrontsCount（A＝N）'))
P('N（政治予算の分母）：平均 %.3f・中央 %.1f・p90 %.1f・N≦1（係数 1.0）の割合 %.1f%%' % (mean(cntN), statistics.median(cntN), quant(cntN, 0.9), 100.0 * sum(1 for x in cntN if x <= 1) / len(cntN)))
P('政治予算の係数 pushBudgetFactor：平均 %.4f・中央 %.4f' % (mean(factor), statistics.median([x for x in factor if not math.isnan(x)])))
P('記録した戦線の種類（件数／国×四半期）：' + '・'.join('%s %.3f' % (k, v / rows) for k, v in sorted(K.items())))
P('Q1 段の法 L の政治資本固定（符号 2）：%d／%d ＝ %.2f%%' % (L['fixed'], L['all'], 100.0 * L['fixed'] / L['all'] if L['all'] else float('nan')))
P('連続の軸の払い（1国×四半期）：%.5f・段の法の払い：%.5f・合計 %.5f' % (mean(cont), mean(lvl), mean(cont) + mean(lvl)))
P('S頭／b（段の法のループの頭の目減り÷最大量）：平均 %.3f・中央 %.3f・p90 %.3f・p99 %.3f・壁（S頭≧b）の割合 %.2f%%' % (
    mean(sh), statistics.median(sh), quant(sh, 0.9), quant(sh, 0.99), 100.0 * shfull / shn if shn else float('nan')))
P('N の分布（上位）：' + '・'.join('%d:%.1f%%' % (k, 100.0 * v / len(cntN)) for k, v in sorted(Nhist.items())[:12]))
text = '\n'.join(lines) + '\n'
if OUT:
    with open(OUT, 'w', encoding='utf-8', newline='\n') as f:
        f.write(text)
sys.stdout.reconfigure(encoding='utf-8')
print(text)
