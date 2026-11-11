# (1015) c94rh 案 B の向きの紙（推）：board.tsv の持ち州の産地の数（種の中央値）から、国×財の B の下での向きを記号で並べる。
#   ＋＝産地の州を持つ（B で単位費用 0.297〜0.285・輸出か自給の側）／−＝持たない（非産地 0.54 のまま＝産地の楔 1.82 倍の差で輸入の側）／
#   ±＝中央値は 0 だが 1 種以上で持つ（種で割れる）。年は 1870 と 1913。力学は読まない（越境が実際に起きるかは輸送の楔と量しだい＝紙）。
# 使い方: python -X utf8 -I rh_dirB.py <board.tsv> <財,財,...> <出力のファイル>
import sys
BOARD, GL, OUT = sys.argv[1:4]
GL = GL.split(',')
NINE = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'アメリカ', '清', 'オーストリア', 'オスマン', '日本', 'スペイン', 'スウェーデン', 'ライン諸邦']
B = {}
with open(BOARD, encoding='utf-8') as f:
    for line in f:
        if line.startswith('#'): continue
        c = line.rstrip('\n').split('\t')
        if c[0] == 'nation': h = {k: i for i, k in enumerate(c)}; continue
        B[(c[0], c[1])] = c
def sym(c, y):
    if c is None: return '−'
    m = float(c[h[f'held{y}_med']]); mx = int(c[h[f'held{y}_rng']].split('-')[1])
    return '＋' if m > 0 else ('±' if mx > 0 else '−')
o = open(OUT, 'w', encoding='utf-8', newline='\n')
o.write(f'# rh_dirB board={BOARD}（各セル＝1870/1913 の記号・＋産地あり ±種で割れる −産地なし）\nnation\t' + '\t'.join(GL) + '\n')
for n in NINE:
    o.write(n + '\t' + '\t'.join(sym(B.get((n, g)), 1870) + '/' + sym(B.get((n, g)), 1913) for g in GL) + '\n')
o.close(); print('ok', OUT)
