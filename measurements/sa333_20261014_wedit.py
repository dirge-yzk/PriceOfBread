# -*- coding: utf-8 -*-
"""(333) world.json を行単位で書き換える（pop227 の W を使う＝LF のみ・BOM なし・整形し直さない）。
使い方：python sa333_20261014_wedit.py <world.json の入力> <出力> <腕: B1|B2> [<変更表の TSV 出力>]
腕＝国の階級の人数と国庫・国債を f 倍（州の share・coercion〔人口あたり〕・識字率は据え置き）。植民地の行（英領ベンガルほか）は据え置き。"""
import sys, os, json
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import pop227_20261010_wedit as base
import sa333_20261014_spec as spec

if __name__ == '__main__':
    src, dst, arm = sys.argv[1], sys.argv[2], sys.argv[3]
    w = base.W(open(src, 'rb').read().decode('utf-8'))
    fs = spec.arm_f(arm)
    for nm in spec.INDEPENDENTS:
        f = fs[nm]
        if f == 1.0:
            w.log.append((arm, nm, '階級の和（千人）', w.total(w.nation(nm)), w.total(w.nation(nm)), '', '', 'R', '据え置き：' + spec.ANCHOR_SRC.get(nm, '')))
            continue
        old = w.total(w.nation(nm))
        w.scale_classes(w.nation(nm), f)
        w.log.append((arm, nm, '階級の和（千人）', old, w.total(w.nation(nm)), '', '', 'R', '×%.6f（BCG 1783 補間 %.0f 千人へ）' % (f, spec.TARGET_K)))
    open(dst, 'wb').write(w.text().encode('utf-8'))
    if len(sys.argv) > 4:
        with open(sys.argv[4], 'w', encoding='utf-8', newline='\n') as f:
            f.write('腕\t国\t項目\t旧\t新\t下限\t上限\t確からしさ\t根拠\n')
            for row in w.log:
                f.write('\t'.join(str(x) for x in row) + '\n')
    json.loads(w.text())
    print('ok', len(w.log), spec.factors())
