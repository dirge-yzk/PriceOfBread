"""(273) 線P 後半：在庫に比例する流れ（窃盗・港の工賃・外資の逃避と配当ほか）が置き直しでどれだけ縮んだかの表（段T を重ねる回の入力・読むだけ）。
  python an_p273_flowshrink.py <出力.md> B=<flow の出力フォルダ> R=<…> S=<…> S2=<…> [窓=Q5〜Q40] [国=イギリス]
  入力＝`python scripts/instruments/flow262/an_flow262.py <ins> <flow の出力フォルダ>` が書く t2_routes.tsv（国の所得に対する割合・種の中央）。
  出力＝全階級・大衆・富裕の各グループで、基線 B の |値| の大きい順に経路を並べ、各腕の値と 腕÷B を出す。
  限界の自己申告：
    ・値は flow_s の四半期の月平均を窓で平均して国の所得（Earned の和）で割った割合の、種の中央（t2_routes.tsv の median 列）。種の中央どうしの比＝種ごとの比の中央ではない。
    ・「在庫に比例する」の印は経路名のキーワード（窃盗・逃避・配当・利子・工賃・保有）による分類で、機構の読みではない（実際に在庫に比例するかは読み手の外）。
    ・B に無い経路（腕でだけ出る経路）は B の値を 0 として並べる。窓に入った四半期の数は腕で同じ（同じ Q）。"""
import sys, os, csv, collections
OUT = sys.argv[1]
ARMS = collections.OrderedDict(); WIN = 'Q5〜Q40'; NAT = 'イギリス'
for a in sys.argv[2:]:
    k, v = a.split('=', 1)
    if k == '窓': WIN = v
    elif k == '国': NAT = v
    else: ARMS[k] = v
KEYS = ('窃盗', '逃避', '配当', '利子', '工賃', '保有')
D = {}
for arm, d in ARMS.items():
    with open(os.path.join(d, 't2_routes.tsv'), encoding='utf-8') as fh:
        for r in csv.DictReader(fh, delimiter='\t'):
            if r['nation'] != NAT or r['window'] != WIN: continue
            D.setdefault((r['group'], r['kind'], r['file'], r['line'], r['name'], r['cp']), {})[arm] = float(r['median'])
groups = []
for k in D:
    if k[0] not in groups: groups.append(k[0])
out = [f'# (273) 在庫に比例する流れの縮み（{NAT}・{WIN}・国の所得比・種の中央）\n\n']
armnames = list(ARMS)
for g in groups:
    rows = [(k, v) for k, v in D.items() if k[0] == g]
    rows.sort(key=lambda kv: -abs(kv[1].get('B', 0.0)))
    out.append(f'## {g}\n\n| 種類 | 経路 | 相手 | 印 | ' + ' | '.join(armnames) + ' | ' + ' | '.join(f'{a}÷B' for a in armnames if a != 'B') + ' |\n|---|---|---|---|' + '---|' * (len(armnames) + len(armnames) - 1) + '\n')
    n = 0
    for k, v in rows:
        b = v.get('B', 0.0)
        if max(abs(x) for x in v.values()) < 0.002: continue
        if n >= 14: break
        n += 1
        mark = '在庫比例?' if any(w in k[4] for w in KEYS) else ''
        ratios = ' | '.join((f'{v.get(a, 0.0) / b:.3f}' if abs(b) > 1e-9 else '—') for a in armnames if a != 'B')
        out.append(f'| {k[1]} | {k[4]} ({k[2]}:{k[3]}) | {k[5]} | {mark} | ' + ' | '.join(f'{v.get(a, 0.0):.4f}' for a in armnames) + f' | {ratios} |\n')
    out.append('\n')
open(OUT, 'w', encoding='utf-8', newline='\n').write(''.join(out))
print('done', OUT)
