# (1414) c151wr 項 1：wexit_s の oldDecisiveFlag が、旧の崩壊の書き換えの後の戦勝点で数え直されたことと、newEndsFlag の値の検算。
# 呼び方：python wexit_check2.py <基準の表(m1411)> <腕の表>。0＝宣言どおり。
# 検算：(a) newEndsFlag が列 scoreIndex・kA・kB だけから再計算した値（s>=kB または -s>=kA）と一致する（表の桁の丸めの境は数える）。
#       (b) 崩壊の旗が無い行は oldDecisiveFlag が |scoreIndex|>=1 と一致する。
#       (c) 基準との差は oldDecisiveFlag の列だけで、崩壊の旗が立つ行だけ。他の列・行の数・見出しは同じ。
#       (d) 崩壊の旗が立つ行の旧の規則との食い違い（|scoreIndex|>=1 と oldDecisiveFlag が違う行）を数えて書く（記録）。
import sys
def rows(p):
    with open(p, encoding='utf-8') as f: return [l.rstrip('\n').split('\t') for l in f]
a, b = rows(sys.argv[1]), rows(sys.argv[2])
if len(a) != len(b): print(f'行の数が違う {len(a)} {len(b)}'); sys.exit(1)
if b[0][:len(a[0])] != a[0] or b[0][len(a[0]):] != ['powerIdA', 'powerIdB']: print('見出しが違う（基準＋powerIdA・powerIdB のはず）'); sys.exit(1)
b = [r[:len(a[0])] for r in b]   # 項 7 の足した 2 列は基準に無い＝比べる列は基準の列だけ
h = a[0]; ix = {n: h.index(n) for n in h}
bad = 0; edge = 0; chg = 0; cdiff = 0
for x, y in zip(a[1:], b[1:]):
    s, kA, kB = float(y[ix['scoreIndex']]), float(y[ix['kA']]), float(y[ix['kB']])
    ends = 1 if (s >= kB or -s >= kA) else 0
    if int(y[ix['newEndsFlag']]) != ends:
        # 表は有効桁を丸めて書く＝境の 1e-6 以内は丸めの差として数える
        if min(abs(s - kB), abs(-s - kA)) < 1e-6: edge += 1
        else: bad += 1; print('newEndsFlag が再計算と違う', y[:4])
    col = x[ix['oldCollapseAFlag']] == '1' or x[ix['oldCollapseBFlag']] == '1'
    if not col and int(y[ix['oldDecisiveFlag']]) != (1 if abs(s) >= 1.0 else 0): bad += 1; print('崩壊なしの行の oldDecisiveFlag が違う', y[:4])
    if col and int(y[ix['oldDecisiveFlag']]) != (1 if abs(s) >= 1.0 else 0): cdiff += 1
    if x != y:
        chg += 1
        if not col: bad += 1; print('崩壊の旗が無い行が違う', x[:4])
        d = [h[i] for i in range(len(x)) if x[i] != y[i] and h[i] != 'oldDecisiveFlag']
        if d: bad += 1; print('宣言の外の列が違う', d)
print(f'wexit_s 検算2 {"OK" if bad==0 else "NG"} 違う行 {chg}（oldDecisiveFlag だけ）・崩壊の行で scoreIndex の旧規則と食い違う行 {cdiff}・newEnds の丸めの境 {edge}・不一致 {bad}')
sys.exit(1 if bad else 0)
