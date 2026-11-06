# (880) c74id 旗 0 の ident の生で新しい列 importBudgetExcessMaxPerClearingSubLive の予言 p1〜p4 を確かめる（読むだけ・事前登録 [3]）。
#   python -X utf8 -I live880.py <ins（cearn_s<種>.tsv のあるフォルダ）> [種の数 10]
import sys, os
d = sys.argv[1]; NS = int(sys.argv[2]) if len(sys.argv) > 2 else 10
def fl(s):
    try: return float(s)
    except Exception: return float('nan')
def thr(x, ix):
    b = fl(x[ix['importBudgetStartPerQuarter']]); p = fl(x[ix['importPaidPerQuarter']])
    ref = b if (b == b and b != float('inf')) else (p if p == p else 0.0)
    return 1e-9 * (1 + ref) + 1e-9
n = p1 = p2n = p2 = p3n = p3 = p4 = live_def = sub_def = sub_only = 0; out = []
for sd in range(1, NS + 1):
    with open(os.path.join(d, 'cearn_s%d.tsv' % sd), encoding='utf-8') as f:
        h = f.readline().rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        assert h[-1] == 'importBudgetExcessMaxPerClearingSubLive', h[-1]
        for line in f:
            x = line.rstrip('\r\n').split('\t'); n += 1
            s, l = x[ix['importBudgetExcessMaxPerClearingSub']], x[ix['importBudgetExcessMaxPerClearingSubLive']]
            sv, lv = fl(s), fl(l); t = thr(x, ix)
            if lv == lv: live_def += 1
            if sv == sv: sub_def += 1
            if sv == sv and lv != lv: sub_only += 1
            if (lv == lv and sv == sv and lv > sv) or (lv == lv and sv != sv): p1 += 1; out.append('p1 外れ\t%d\t%s' % (sd, '\t'.join(x[:5] + [s, l])))
            if fl(x[ix['importPaidPayerSizeLe1e12PerQuarter']]) == 0:
                p2n += 1
                if s != l: p2 += 1; out.append('p2 外れ\t%d\t%s' % (sd, '\t'.join(x[:5] + [s, l])))
            if sv == sv and sv > t:
                p3n += 1
                if lv == lv and lv > t: p3 += 1; out.append('p3 外れ\t%d\t%s' % (sd, '\t'.join(x[:5] + [s, l])))
                else: out.append('7 行\t%d\t%s\tLive=%s' % (sd, '\t'.join(x[:5] + [s]), l))
            if lv == lv and lv > t: p4 += 1; out.append('p4 外れ\t%d\t%s' % (sd, '\t'.join(x[:5] + [s, l])))
print('\n'.join(out))
print('行 %d・新しい列が定義 %d・Sub が定義 %d・Sub だけ定義（人の居ない払いの清算だけの四半期）%d' % (n, live_def, sub_def, sub_only))
print('p1 新しい列 ≦ Sub か nan：外れ %d' % p1)
print('p2 帯 ≦1e-12 が 0 の行 %d で文字列の一致：外れ %d' % (p2n, p2))
print('p3 U2s の行 %d で新しい列が nan か閾値以下：外れ %d' % (p3n, p3))
print('p4 新しい列が閾値を超える行（U2L の旗 0）：%d' % p4)
print('VERDICT live880 %s' % ('pass' if p1 == p2 == p3 == p4 == 0 else 'fail'))
