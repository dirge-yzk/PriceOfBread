# (1129) 合流：腕 Z の計器の生から keep_1129/base_ins を作る（m892/keep892.py の型・前の INSRAW keep_1122/base_ins とバイトで同じ表はハードリンク・違う表だけ Z の生を移す）。
#   python -X utf8 keep1129.py <Z の ins> <前の INSRAW> <新しい INSRAW> <出力の tsv>
import filecmp, os, shutil, sys

z, prev, new, out = sys.argv[1:5]
os.makedirs(new, exist_ok=True)
rows = []
nlink = nown = 0
for f in sorted(os.listdir(z)):
    zp = os.path.join(z, f)
    pp = os.path.join(prev, f)
    size = os.path.getsize(zp)
    if os.path.exists(pp) and filecmp.cmp(zp, pp, shallow=False):
        os.link(pp, os.path.join(new, f))
        kind = 'link'
        nlink += 1
    else:
        shutil.move(zp, os.path.join(new, f))
        kind = 'own'
        nown += 1
    rows.append(f'{f}\t{size}\t{kind}')
with open(out, 'w', encoding='utf-8', newline='\n') as w:
    w.write('# file\tbytes\tkind（link＝keep_1122/base_ins とバイトで同じ＝ハードリンク・own＝この木の表）\n')
    w.write('\n'.join(rows) + '\n')
print('link', nlink, 'own', nown, 'ownGB', round(sum(int(r.split('\t')[1]) for r in rows if r.endswith('own')) / 1e9, 2))
