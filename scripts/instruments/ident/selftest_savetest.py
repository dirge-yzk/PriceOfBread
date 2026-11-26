# (1380・1387 で ident/ へ移し 4 つ目の例を足した) ident_check.cmp_saveleak の自己試験：不一致の行の savetest.txt で fail が出ること・一致の行で出ないこと。
import sys, os, tempfile
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import ident_check as ic
LEAK = '保存則違反なし 人口 0四半期 基礎貨幣 0四半期\n'
OKL = 'savetest 状態 0日の点 値の違い 0・順だけ 0・比べた対象 10\nsavetest OK：12四半期後も全61カ国が継続系列と一致（最大相対差 0.0E+000）\n'
BAD = 'savetest 状態 0日の点 値の違い 303・順だけ 0・比べた対象 10\nsavetest 不一致 0（2量）・状態の違いのある点 1/1（最大相対差 0.0E+000）\n'
MID = 'savetest OK：途中\n' + BAD   # 途中に OK が混ざっても最終行が不一致なら fail
LAST = 'savetest 状態 0日の点 値の違い 0・順だけ 0・比べた対象 10\nsavetest 不一致 0（2量）・状態の違いのある点 1/1（最大相対差 0.0E+000）\n'   # (1387) 値の違いは 0 でも最終行が不一致なら fail（最終行の判定だけが拾う例）
def run(txt):
    d = tempfile.mkdtemp()
    open(os.path.join(d, 'savetest.txt'), 'w', encoding='utf-8').write(txt)
    open(os.path.join(d, 'leakhunt.txt'), 'w', encoding='utf-8').write(LEAK)
    n0 = len(ic.FAILS); ic.cmp_saveleak(d); return len(ic.FAILS) - n0
r = (run(OKL), run(BAD), run(MID), run(LAST))
print('一致=%d 不一致=%d 途中OK+不一致=%d 値0+最終行不一致=%d' % r)
assert r[0] == 0 and r[1] > 0 and r[2] > 0 and r[3] > 0
print('自己試験 OK')
