# c106dc 担当向けの短い README：基準の木の README.md の行を、節ごとに抜き出す（新しい決まりは足さない）。
import sys, re
sys.path.insert(0, 'D:/PoB_work/wt_c106dc/scripts/instruments/c106dc')
from common import *

MAXLEN = int(sys.argv[1]) if len(sys.argv) > 1 else 105

# (節の題, 元の節, [行番号 or (始,終)])
SEC = [
 ('設計思想 10 則', '§1-1', [(32, 41), 43]),
 ('測定の作法（用語・回す前の確認・回し方）', '§1-2', [47, 56, (60, 63), (71, 81), 85, (90, 94)]),
 ('止める線の改め', '§1-2 止める線の改め', [98, (99, 179), 181, (185, 187)]),
 ('腕の節約', '§1-2 腕の節約', [(193, 196)]),
 ('計算量の予算', '§1-2 計算量の予算', [213, (217, 222)]),
 ('計器と読み手の道具', '§1-2 計器と読み手の道具', [228, (229, 236), 238]),
 ('ビット一致', '§1-3', [257, (260, 272)]),
 ('GUI・Python', '§1-4・§1-5', [276, 278, 286, (287, 288)]),
 ('作者の裁定が原因のとき・因果が読める方向', '§1-7・§1-8', [309, (311, 314), 318, 320, (321, 325)]),
 ('単位とスケール・財は名前で引く', '§1-9', [329, 332, 333, 335, 337, 358, 359, 361, 362]),
 ('ヘルパー', '§1-10', [(372, 374)]),
]

def excerpt(s):
    s = s.strip()
    s = re.sub(r'^[-*]\s+', '', s)
    s = s.replace('\r', '')
    if s.startswith('|'):
        cells = [c.strip() for c in s.strip('|').split('|')]
        s = '：'.join(cells[:2])
    if len(s) <= MAXLEN:
        return s
    cut = s[:MAXLEN]
    k = cut.rfind('。')
    if k >= 60:
        return cut[:k + 1]
    return cut + '…'

def main():
    L = base(F_RD).decode('utf-8').split('\n')
    out = []
    out.append('# 線の担当が毎回読む決まり（README.md の抜粋・短い版）\n')
    out.append('⚠ 新しい決まりは足していない。各行は `docs/README.md`（基準 ffb3838 の版）の行の頭の抜粋で、末尾の〈元〉が元の節と行。条件や例外の全文が要るときは元の行を読む。\n')
    out.append('★ 抜き出していない物：§0・§2〜§5・§1-6（フォークの決まり）・§1-2 の判定の線の表と所要の目安・表の長い列。\n')
    for title, src, items in SEC:
        out.append('\n## %s（%s）\n' % (title, src))
        for it in items:
            rng = range(it[0], it[1] + 1) if isinstance(it, tuple) else [it]
            for n in rng:
                raw = L[n - 1]
                if not raw.strip() or raw.startswith('|---') or raw.startswith('```'):
                    continue
                out.append('- %s 〔元 %s L%d〕\n' % (excerpt(raw), src.split(' ')[0], n))
    return ''.join(out).encode('utf-8')

if __name__ == '__main__':
    data = main()
    sys.stdout.buffer.write(data) if '--stdout' in sys.argv else open(REPO + '/docs/README_線の担当.md', 'wb').write(data)
    print(len(data.decode('utf-8')), file=sys.stderr)
