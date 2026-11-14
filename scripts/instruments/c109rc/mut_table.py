# (1123) 突然変異ごとの門の表：<作業フォルダ>/gates_<M>.txt を読み、門ごとに 緑／赤／未 と、対照 CTL と比べた〈赤↑〉（CTL で緑か未の門が赤）を並べる。
# 使い方: python -X utf8 -I mut_table.py <作業フォルダ> <出力> CTL M1 M2 ...
import sys, os
W, out, ms = sys.argv[1], sys.argv[2], sys.argv[3:]
def read(m):
    p = os.path.join(W, f'gates_{m}.txt'); r = {}
    if not os.path.exists(p): return None
    for ln in open(p, encoding='utf-8'):
        c = ln.rstrip('\n').split('\t')
        if len(c) >= 3 and c[1] in ('緑', '赤', '未'): r[c[0]] = (c[1], c[2])
    return r
tabs = {m: read(m) for m in ms}
gates = []
for m in ms:
    for g in (tabs[m] or {}):
        if g not in gates: gates.append(g)
ctl = tabs.get('CTL') or {}
with open(out, 'w', encoding='utf-8', newline='\n') as o:
    o.write('# (1123) 突然変異ごとの門（2 種・80 四半期・対照 CTL＝旗 1 の突然変異なし）。〈赤↑〉＝CTL で緑か未なのに突然変異で赤。下限の件数は短い走りなので CTL でも赤になりうる＝下限は読まず〈食い違い〉と大きさを読む\n')
    o.write('突然変異\t' + '\t'.join(gates) + '\n')
    for m in ms:
        t = tabs[m]
        if t is None: o.write(f'{m}\t(出力なし)\n'); continue
        row = []
        for g in gates:
            st, det = t.get(g, ('-', ''))
            mark = st + ('↑' if m != 'CTL' and st == '赤' and ctl.get(g, ('-',))[0] in ('緑', '未') else '')
            row.append(f'{mark}:{det[:90]}')
        o.write(m + '\t' + '\t'.join(row) + '\n')
print(open(out, encoding='utf-8').read()[:200])
