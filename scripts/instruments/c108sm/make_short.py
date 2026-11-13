# (1120) c108sm：短い README を作り直す。c106dc/make_short.py の写し＋次の直し。
#  - 行の頭 105 字で切るのをやめる＝各行は〈元の全文〉か〈→ README L行〉の参照のどちらか（途中で切った文を置かない）。
#  - 項目は c106dc の基準の版（ffb3838）の行番号で選び、今の docs/README.md の同じ行（全文一致→先頭 30 字一致）へ引き直す。
#  - 全文で入れる物：止める線の改め（:98〜:115）・★(711)・★(743)・腕の節約・計算量の予算・L89。それ以外は FULLMAX 字以下なら全文・超えれば参照。
import sys, os, re, subprocess
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'c106dc'))
from common import base, REPO, F_RD
FULLMAX = int(sys.argv[1]) if len(sys.argv) > 1 and sys.argv[1].isdigit() else 250
LIMIT = 20000

SEC = [
 ('設計思想 10 則', '§1-1', [(32, 41), 43]),
 ('測定の作法（用語・回す前の確認・回し方）', '§1-2', [47, 56, (60, 63), (71, 81), 85, 'c89', (90, 94)]),
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

def main():
    old = base(F_RD).decode('utf-8').replace('\r', '').split('\n')
    cur = open(REPO + '/' + F_RD, 'rb').read().decode('utf-8').replace('\r', '').split('\n')
    idx = {}
    for i, t in enumerate(cur): idx.setdefault(t, []).append(i + 1)
    def remap(n):
        t = old[n - 1]
        if t in idx and len(idx[t]) == 1: return idx[t][0]
        c = [i + 1 for i, x in enumerate(cur) if x[:30] == t[:30] and t[:30].strip()]
        if len(c) == 1: return c[0]
        return None
    out = ['# 線の担当が毎回読む決まり（README.md の抜粋・短い版）\n',
           '⚠ 新しい決まりは足していない。各行は `docs/README.md` の行の全文か、`→ README L行` の参照（長い行）のどちらか。途中で切った文は置かない。\n',
           '★ 抜き出していない物：§0・§2〜§5・§1-6（フォークの決まり）・§1-2 の判定の線の表と所要の目安・表の長い列。\n']
    miss = []
    full_lines = set()
    for title, src, items in SEC:
        out.append('\n## %s（%s）\n' % (title, src))
        for it in items:
            rng = range(it[0], it[1] + 1) if isinstance(it, tuple) else [it]
            for n in rng:
                if isinstance(n, str):   # 今の README の行番号で直に指す（L89＝規律の表の見出し・全文）
                    m = int(n[1:]); out.append(('- %s 〔README L%d〕'+chr(10)) % (cur[m - 1].strip(), m)); continue
                raw = old[n - 1]
                if not raw.strip() or raw.startswith('|---') or raw.startswith('```'): continue
                m = remap(n)
                if m is None: miss.append(n); continue
                t = cur[m - 1]
                forced = (98 <= m <= 116 or m == 125 or src.startswith('§1-2 腕の節約') or src.startswith('§1-2 計算量の予算'))
                s = re.sub(r'^[-*]\s+', '', t.strip())
                if forced or len(s) <= FULLMAX:
                    out.append('- %s 〔README L%d〕\n' % (s, m))
                else:
                    tag = re.split(r'[：:]', s, 1)[0][:40]
                    out.append('- → README L%d（%s）\n' % (m, tag))
    data = ''.join(out)
    return data, miss

if __name__ == '__main__':
    data, miss = main()
    open(REPO + '/docs/README_線の担当.md', 'wb').write(data.encode('utf-8'))
    print(len(data), 'miss', miss, file=sys.stderr)
