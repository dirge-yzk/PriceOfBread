# (1072) 線 c102em の門 G1・G2 の読み手：.cost の行から 飛ばした件数・独立の件数・消費：POP×財（月）を読み、一致と割合を出す（読むだけ）。
#   使い方: python -I g1.py <.cost のパス>...    出力: 件数・G1（飛ばした＝独立）・G2（独立÷POP×財 が 0.08 以上 0.16 未満）
import sys

def rows(p):
    d = {}
    on = False
    for ln in open(p, encoding='utf-8').read().split('\n'):
        ln = ln.rstrip('\r')
        if ln.startswith('  名前\t件数'):
            on = True; continue
        if ln.startswith('推定の計算量'):
            on = False
        if on and ln.startswith('  '):
            f = ln.strip().split('\t')
            d[f[0]] = int(f[1])
    return d

for p in sys.argv[1:]:
    d = rows(p)
    sk = next(v for k, v in d.items() if k.startswith('消費：飛ばした'))
    ind = next(v for k, v in d.items() if k.startswith('消費：人口 0'))
    tot = d['消費：POP×財（月）']
    r = ind / tot
    print(f'{p}\n  飛ばした {sk}  独立 {ind}  消費：POP×財 {tot}  独立÷POP×財 {r:.4f}')
    print('  G1 ' + ('一致' if sk == ind and sk > 0 else ('計器つき（飛ばさない＝0）' if sk == 0 and ind > 0 else '不一致')) + '  G2 ' + ('範囲内' if 0.08 <= r < 0.16 else '範囲外'))
