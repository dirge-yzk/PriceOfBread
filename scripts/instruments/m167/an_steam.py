"""(167) 工場の蒸気動力の割合（SteamFill・an_gate の断面・種の中央値）を史実比較の桁の線で読む（読むだけ）。
史実＝工場（製造業）の動力のうち蒸気の割合【推・記憶の概数＝Kanefsky（英）・Atack（米）ほか】・床 0.05・比＝シミュ÷max(史実, 床)・両方が床の下は可。
断面の年で10年の窓を代表させる（計器の出力が断面だけ）。python an_steam.py <an_gate の出力> [...]"""
import sys, re
YEARS = [1793, 1813, 1833, 1853, 1863, 1873, 1883, 1893, 1913, 1933, 1949]
USE = [1793, 1813, 1833, 1853, 1873, 1893, 1913]
H = {  # 断面の年の史実【推】
 'イギリス':   [0.15, 0.30, 0.50, 0.70, 0.90, 0.92, 0.90],
 'フランス':   [0.01, 0.02, 0.05, 0.20, 0.40, 0.60, 0.70],
 'プロイセン': [0.00, 0.01, 0.02, 0.20, 0.60, 0.80, 0.80],
 'オーストリア': [0.00, 0.00, 0.02, 0.10, 0.40, 0.60, 0.70],
 'ロシア':     [0.00, 0.00, 0.01, 0.05, 0.30, 0.60, 0.70],
 'アメリカ':   [0.00, 0.02, 0.05, 0.30, 0.50, 0.75, 0.80],
 '日本':       [0.00, 0.00, 0.00, 0.00, 0.02, 0.50, 0.60],
 '清':         [0.00, 0.00, 0.00, 0.00, 0.00, 0.05, 0.10],
 'スペイン':   [0.00, 0.00, 0.01, 0.10, 0.30, 0.50, 0.60],
 'オスマン':   [0.00, 0.00, 0.00, 0.01, 0.05, 0.10, 0.20],
}
FLOOR = 0.05
for path in sys.argv[1:]:
    txt = open(path, encoding='utf-8').read()
    lab = txt.splitlines()[0]
    sec = txt.split('② 工場の蒸気動力の割合')[1].split('③')[0]
    sim = {}
    for ln in sec.splitlines()[1:]:
        m = re.match(r'\s+(\S+): (.*)', ln)
        if m: sim[m.group(1)] = [float(x) for x in m.group(2).split()]
    print(f'{lab}　工場の蒸気動力の割合（断面＝10年の窓の代表・種の中央）［史実【推】］比＝シミュ÷max(史実, 床 {FLOOR})')
    ng = rec = 0; ngc = []
    for c, hist in H.items():
        if c not in sim: continue
        cells = []
        for y, h in zip(USE, hist):
            s = sim[c][YEARS.index(y)]
            if s < FLOOR and h < FLOOR: cells.append(f'{y} {s:.2f}［{h:.2f}］可'); continue
            r = s / max(h, FLOOR)
            tag = 'NG' if (r >= 10 or r <= 0.1) else ('記録' if (r >= 3 or r <= 1/3) else '')
            if tag == 'NG': ng += 1; ngc.append(f'{c} {y}')
            elif tag == '記録': rec += 1
            cells.append(f'{y} {s:.2f}［{h:.2f}］{r:.3g}{tag}')
        print(f'  {c}: ' + '・'.join(cells))
    print(f'  NG のセル（10国×7断面）＝{ng}・記録 {rec}・NG＝{"・".join(ngc)}\n')
