# (899) 合流：keep892.py の写し（前の INSRAW を keep_892/base_ins・比べる線の生を c76lb の keep と c76ls の keep_ident の 2 本にした）。
#   python -X utf8 keep899.py <Z の ins> <前の INSRAW> <新しい INSRAW> <出力の tsv> <線の生 1> [<線の生 2> ...]
#   前の INSRAW とバイトで同じ表はハードリンク・違う表だけ Z の生を移す。線の生に同じ名の表があれば、Z の表とバイトで同じかを数える（記録）。
import filecmp, os, shutil, sys

z, prev, new, out = sys.argv[1:5]
lines = sys.argv[5:]
assert not os.path.exists(new), new
os.makedirs(new)
names = sorted(os.listdir(z))
rows = []
nlink = nown = 0
cmpn = {l: [0, 0] for l in lines}   # [同じ, 違う]
for f in names:
    zp = os.path.join(z, f)
    pp = os.path.join(prev, f)
    tags = []
    for l in lines:
        cp = os.path.join(l, f)
        if os.path.exists(cp):
            s = filecmp.cmp(zp, cp, shallow=False)
            cmpn[l][0 if s else 1] += 1
            tags.append(('same' if s else 'diff') + ':' + os.path.basename(os.path.dirname(l.rstrip('/'))) + '/' + os.path.basename(l.rstrip('/')))
    size = os.path.getsize(zp)
    if os.path.exists(pp) and filecmp.cmp(zp, pp, shallow=False):
        os.link(pp, os.path.join(new, f)); kind = 'link'; nlink += 1
    else:
        shutil.move(zp, os.path.join(new, f)); kind = 'own'; nown += 1
    rows.append(f'{f}\t{size}\t{kind}\t{",".join(tags) or "-"}')
with open(out, 'w', encoding='utf-8', newline='\n') as w:
    w.write('# file\tbytes\tkind（link＝keep_892/base_ins とバイトで同じ＝ハードリンク・own＝この木の表）\t線の生と（same／diff）\n')
    w.write('\n'.join(rows) + '\n')
print(f'files {len(names)} link {nlink} own {nown}', {l: tuple(v) for l, v in cmpn.items()})
