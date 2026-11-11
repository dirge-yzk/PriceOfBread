# (954) 合流：INSRAW を合わせた木の ident の計器つき 10 種の生へ移す（★(926) の型）。
#   python -X utf8 keep954.py <ident の ins> <古い keep の base_ins> <新しい base_ins>
#   古い keep とバイトで同じ表はハードリンク（古い keep は読むだけ・書かない）・違う表だけ新しい生を移す。
import sys, os, filecmp, shutil
src, old, dst = sys.argv[1:4]
os.makedirs(dst, exist_ok=False)
same = moved = 0; diff = []
for n in sorted(os.listdir(src)):
    s = os.path.join(src, n); o = os.path.join(old, n); d = os.path.join(dst, n)
    if os.path.isfile(o) and filecmp.cmp(s, o, shallow=False):
        os.link(o, d); same += 1
    else:
        shutil.move(s, d); moved += 1; diff.append(n)
print(f'表 {same + moved}・ハードリンク {same}・移した {moved}')
for n in diff: print('  移した', n)
