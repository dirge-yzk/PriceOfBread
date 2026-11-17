"""(1186) c118nc 史実の桁の門 H：ng.sh の mil180 の ⑥〈大戦の兵士÷人口の頂点%〉の 1914-1918 の行を読み、
国ごとに 種の中央÷史実 を出す。基線で [0.1, 10] の内にあった国が腕で外へ出たら 赤（新しい桁外れ）。基線で既に外の国は記録。
使い方: python magn.py <基線の mil180_*.txt> <腕の mil180_*.txt>"""
import sys, re
def read(p):
    t = open(p, encoding='utf-8').read()
    i = t.find('## ⑥'); line = next(l for l in t[i:].splitlines() if l.strip().startswith('1914-1918:'))
    out = {}
    for m in re.finditer(r'([^\s・:]+) ([0-9.]+)〔[^〕]*〕（史実 約([0-9.]+)%', line):
        out[m.group(1)] = (float(m.group(2)), float(m.group(3)))
    return out
b, a = read(sys.argv[1]), read(sys.argv[2])
red = 0
for n in b:
    if n not in a: print(f'{n}: 腕に行なし'); continue
    rb = b[n][0] / b[n][1]; ra = a[n][0] / a[n][1]
    inb = 0.1 <= rb <= 10; ina = 0.1 <= ra <= 10
    tag = '赤（新しい桁外れ）' if inb and not ina else '記録（基線で既に外）' if not inb else '緑'
    red += tag.startswith('赤')
    print(f'{n}: 基線 {b[n][0]:.2f}%（÷史実 {rb:.3f}）→ 腕 {a[n][0]:.2f}%（÷史実 {ra:.3f}） 腕÷基線 {a[n][0]/b[n][0]:.3f} {tag}')
print(f'H: {"赤" if red else "緑"}（赤 {red} 国）')
