# -*- coding: utf-8 -*-
"""(333) world.json を行単位で書き換える（pop227 の W を使う＝LF のみ・BOM なし・整形し直さない）。
使い方：python sa333_20261014_wedit.py <world.json の入力> <出力> <腕: B1|B2> [<変更表の TSV 出力>]
腕＝国の階級の人数と国庫・国債を f 倍（州の share・coercion〔人口あたり〕・識字率は据え置き）。植民地の行（英領ベンガルほか）は据え置き。"""
import sys, os, json
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pop227_20261010_wedit as base
import sa333_20261014_spec as spec

DEFAULT_CLASSES = spec.DEFAULT_CLASSES


def with_defaults(w, nm):
    """world.json に行の無い階級は NationConfig の既定値（商人 400・職人 800）を読む＝一様に掛けるには明示の行にして掛ける。
    返す値＝既定値を含む階級の和（千人）。"""
    r = w.nation(nm)
    have = w.class_lines(r)
    return w.total(r) + sum(v for k, v in DEFAULT_CLASSES.items() if k not in have)


def scale_nation(w, nm, f):
    """階級（行の無い物は既定値を明示してから）と国庫・国債を f 倍。"""
    r = w.nation(nm)
    have = w.class_lines(r)
    miss = [k for k in DEFAULT_CLASSES if k not in have]
    if miss:
        # professional の行の直後（無ければ最後の階級の行の直後）に既定値の行を足す＝値は旧の既定値のまま（A と同じ挙動）
        anchor = max(i for i, _ in have.values())
        w.L[anchor + 1:anchor + 1] = ['   "%s": %d,' % (k, DEFAULT_CLASSES[k]) for k in miss]
    r = w.nation(nm)
    w.scale_classes(r, f)
    return miss


if __name__ == '__main__':
    src, dst, arm = sys.argv[1], sys.argv[2], sys.argv[3]
    w = base.W(open(src, 'rb').read().decode('utf-8'))
    fs = spec.arm_f(arm)
    for nm in spec.INDEPENDENTS:
        f = fs[nm]
        if f == 1.0:
            w.log.append((arm, nm, '階級の和（千人・既定値込み）', with_defaults(w, nm), with_defaults(w, nm), '', '', 'R', '据え置き：' + spec.ANCHOR_SRC.get(nm, '')))
            continue
        old = with_defaults(w, nm)
        miss = scale_nation(w, nm, f)
        w.log.append((arm, nm, '階級の和（千人・既定値込み）', old, w.total(w.nation(nm)), '', '', 'R', '×%.6f（BCG 1783 補間 %.0f 千人へ）%s' % (f, spec.TARGET_K, ('・行の無い階級 %s は既定値を明示して掛けた' % '/'.join(miss)) if miss else '')))
    open(dst, 'wb').write(w.text().encode('utf-8'))
    if len(sys.argv) > 4:
        with open(sys.argv[4], 'w', encoding='utf-8', newline='\n') as f:
            f.write('腕\t国\t項目\t旧\t新\t下限\t上限\t確からしさ\t根拠\n')
            for row in w.log:
                f.write('\t'.join(str(x) for x in row) + '\n')
    json.loads(w.text())
    print('ok', len(w.log), spec.factors())
