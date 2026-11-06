# (892) 合流：keep885.py の写し（前の INSRAW を keep_885/base_ins・比べる線の生を c75ce と c75cp の ident の生にした）。使い方は同じ：python -X utf8 keep892.py <Z の ins> <前の INSRAW> <新しい INSRAW> <線の ins> <出力の tsv>
# (885) 合流：腕 Z の計器の生から keep_885/base_ins を作る（keep_879/base_ins とバイトで同じ表はハードリンク・違う表だけ Z の生を移す）。
# 併せて、c74id の旗 0 の生（ident_c74id/out_ib_10/ins）と Z の生がバイトで同じかを数える（予言 [4] の最後の行・記録）。
#   python -X utf8 keep885.py <Z の ins> <前の INSRAW> <新しい INSRAW> <c74id の ins> <出力の tsv>
import filecmp, os, shutil, sys

z, prev, new, c74, out = sys.argv[1:6]
os.makedirs(new, exist_ok=True)
names = sorted(os.listdir(z))
rows = []
nlink = nown = nsame74 = 0
for f in names:
    zp = os.path.join(z, f)
    pp = os.path.join(prev, f)
    cp = os.path.join(c74, f)
    same74 = os.path.exists(cp) and filecmp.cmp(zp, cp, shallow=False)
    nsame74 += same74
    size = os.path.getsize(zp)
    if os.path.exists(pp) and filecmp.cmp(zp, pp, shallow=False):
        os.link(pp, os.path.join(new, f))
        kind = 'link'
        nlink += 1
    else:
        shutil.move(zp, os.path.join(new, f))
        kind = 'own'
        nown += 1
    rows.append(f'{f}\t{size}\t{kind}\t{"same" if same74 else "diff"}')
with open(out, 'w', encoding='utf-8', newline='\n') as w:
    w.write('# file\tbytes\tkind（link＝keep_879/base_ins とバイトで同じ＝ハードリンク・own＝この木の表）\tc74id の旗 0 の生と（same／diff）\n')
    w.write('\n'.join(rows) + '\n')
print(f'files {len(names)} link {nlink} own {nown} same_as_c74id {nsame74}')
