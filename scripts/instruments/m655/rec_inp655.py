"""(655) 合流の記録⑥（読むだけ）：工場の投入の充足（factory_s の inputFillShare・作りたい量の金額加重＝国×四半期の 1 行）と労働以外の投入の失い（otherBoundLoss÷desired）を時代×国で並べる（inp_648/s0_anchor.py を腕の名で受ける形にした写し）。
使い方：python -X utf8 -I rec_inp655.py <腕名>=<計器の出力フォルダ> ...（種は 1〜10）
⚠限界（自己申告）：factory_s は国×四半期の金額加重＝〈極端に低い工場の件数の割合〉は読めない（件数の表 inpf_s は (648) で作らなかった）。
  代わりに〈国×四半期の行のうち inputFillShare＜0.25 の割合〉を出す（行の割合であって工場の割合ではない）。"""
import csv, collections, statistics, sys, os
sys.stdout.reconfigure(encoding='utf-8')
N9 = ['清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本', 'アメリカ']
eras = [(1783, 1829), (1830, 1869), (1870, 1909), (1910, 1950)]
def load(root):
    out = collections.defaultdict(lambda: collections.defaultdict(list))
    for s in range(1, 11):
        p = f'{root}/factory_s{s}.tsv'
        if not os.path.exists(p): continue
        tmp = collections.defaultdict(lambda: collections.defaultdict(list))
        with open(p, encoding='utf-8') as f:
            for row in csv.DictReader(f, delimiter='\t'):
                nat = row['nation']; y = int(row['year'])
                if nat not in N9: continue
                des = float(row['desiredRefValuePerMonth'])
                if des <= 0: continue
                e = [i for i, (a, b) in enumerate(eras) if a <= y <= b]
                if not e: continue
                e = e[0]; fill = float(row['inputFillShare'])
                tmp[(e, nat)]['fill'].append(fill)
                tmp[(e, nat)]['low'].append(1.0 if fill < 0.25 else 0.0)
                tmp[(e, nat)]['oth'].append(float(row['otherBoundLossRefValuePerMonth']) / des)
        for k, v in tmp.items():
            for m, l in v.items(): out[k][m].append(sum(l) / len(l))
    return out
arms = [a.split('=', 1) for a in sys.argv[1:]]
res = {n: load(d) for n, d in arms}
for metric, lab in (('fill', '入力の充足 inputFillShare'), ('low', 'inputFillShare＜0.25 の国×四半期の行の割合'), ('oth', '労働以外の投入の失い÷作りたい量')):
    print(f'## {lab}（種の中央値・腕の並び {" / ".join(n for n, _ in arms)}）')
    for e, (a, b) in enumerate(eras):
        cells = []
        for nat in N9:
            vals = []
            for n, _ in arms:
                v = res[n][(e, nat)][metric]
                vals.append(f'{statistics.median(v):.2f}' if v else '-')
            cells.append(f'{nat} {"/".join(vals)}')
        print(f'  {a}-{b}: ' + '　'.join(cells))
