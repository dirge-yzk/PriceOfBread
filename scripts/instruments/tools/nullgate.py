# 門 A′（ほとんど影響がない）：腕の成績表を、基線とヌル腕 2 本（同じ分布から来た 3 本）と同じ条件で比べる。
# 使い方：python nullgate.py <腕の成績表> <基線> <ヌル腕1> <ヌル腕2>
#   腕なしで基線の 3 本だけを渡すと（<腕> に - ）、3 本それぞれの「残り 2 本の幅の外」の列の数（較正の値）だけを出す。
# 比べ方：
#   - 行は数字を # にした型で突き合わせる（difflib・国の表は並びが変わるので位置では比べない）。4 本全てで合った行の数字だけを使う。
#   - 較正：3 本それぞれについて、残り 2 本の最小〜最大の外にある列の数 L_k（3 本の間で値が揺れる列だけ）。
#   - 腕：3 本から 2 本の組 3 通りそれぞれについて、腕が組の幅の外にある列の数を数え、平均する＝O。
#     どちらも「1 本を、同じ分布の 2 本の幅と比べる」なので、腕が雑音と同じなら O と L_k は同じ分布になる。
#   - 判定：O ≦ max(L_k) なら〈雑音の内〉。超えたら〈雑音の外〉＝門 A′ 不合格。
#   - 3 本で値が全く同じ列（破綻 0/10 など）で腕だけが違う列は、件数と行を別に出す（判定には入れない・止める線の列なら止める線で読む）。
# 限界（自己申告）：3 本からの較正は粗い（L_k の幅が大きい）。列どうしは独立でない（同じ国の数字は一緒に動く）。
import difflib
import re
import sys

NUM = re.compile(r'-?\d+(?:\.\d+)?')


def load(path):
    rows = []
    for line in open(path, encoding='utf-8').read().split('\n'):
        if line.startswith('★係数の上書き') or line.startswith('SOURCE-HASH'):
            continue
        rows.append((NUM.sub('#', line), [float(x) for x in NUM.findall(line)], line))
    return rows


def align(a, b):
    sm = difflib.SequenceMatcher(None, [x[0] for x in a], [x[0] for x in b], autojunk=False)
    mp = {}
    for bl in sm.get_matching_blocks():
        for k in range(bl.size):
            mp[bl.a + k] = bl.b + k
    return mp


def columns(base, others):
    maps = [align(base, o) for o in others]
    cols = []
    for i, (_, vals, text) in enumerate(base):
        if all(i in mp for mp in maps):
            for j, v in enumerate(vals):
                cols.append(([v] + [o[mp[i]][1][j] for o, mp in zip(others, maps)], text.strip()[:80]))
    return cols


def outside(x, a, b):
    return not (min(a, b) <= x <= max(a, b))


def main():
    sys.stdout.reconfigure(encoding='utf-8')
    arm, base, n1, n2 = sys.argv[1:5]
    B, N1, N2 = load(base), load(n1), load(n2)
    others = [N1, N2] + ([] if arm == '-' else [load(arm)])
    cols = columns(B, others)
    trio = [(c[:3], t) for c, t in cols]
    varied = [i for i, (c, _) in enumerate(trio) if len(set(c)) > 1]
    L = []
    for k in range(3):
        a, b = [m for m in range(3) if m != k]
        L.append(sum(1 for i in varied if outside(trio[i][0][k], trio[i][0][a], trio[i][0][b])))
    print(f'合った数字の列 {len(cols)}・3 本で揺れる列 {len(varied)}')
    print(f'較正 L（基線・ヌル1・ヌル2 がそれぞれ残り 2 本の幅の外）: {L[0]}・{L[1]}・{L[2]}＝上限 {max(L)}')
    if arm == '-':
        return 0
    pairs = [(0, 1), (0, 2), (1, 2)]
    cnt = [sum(1 for i in varied if outside(cols[i][0][3], cols[i][0][a], cols[i][0][b])) for a, b in pairs]
    O = sum(cnt) / 3.0
    fixed = [(cols[i][0], cols[i][1]) for i in range(len(cols)) if i not in set(varied) and cols[i][0][3] != cols[i][0][0]]
    print(f'腕 O（組ごと {cnt[0]}・{cnt[1]}・{cnt[2]} の平均）: {O:.1f}')
    print(f'3 本で同じ値なのに腕だけが違う列: {len(fixed)}')
    for c, t in fixed[:40]:
        print(f'   {c[0]:g} → {c[3]:g}  | {t}')
    ok = O <= max(L)
    print('判定: 雑音の内' if ok else '判定: 雑音の外')
    return 0 if ok else 1


if __name__ == '__main__':
    sys.exit(main())
