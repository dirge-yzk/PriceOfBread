# (895) c76ls：(892) shrink892.py の写し（中身は同じ）。ident の生を landshare_s と keepsum へ縮める。
# (892) 合流：採らなかった腕の生を keep_892_rej へ縮めて移す（先に keepsum＝全ての表の sha256・バイト数・行数・見出し・を書く）。
#   python -X utf8 shrink892.py <腕の ins> <移す先> <keepsum の出力> <残す表の頭,...>
#   残す表の頭（例 region_s）に種の番号と .tsv が続くファイルだけを移す。他は消す。移す先が既にあれば止める。
import os, re, shutil, subprocess, sys

src, dst, ks, keep = sys.argv[1:5]
keep = set(keep.split(','))
assert not os.path.exists(dst), dst
here = os.path.dirname(os.path.abspath(__file__))
rc = subprocess.run([sys.executable, '-I', os.path.join(here, '..', 'tools', 'keepsum.py'), src, ks]).returncode
assert rc == 0, rc
os.makedirs(dst)
moved = dropped = 0; mb = db = 0
for f in sorted(os.listdir(src)):
    p = os.path.join(src, f)
    m = re.match(r'^(.*?_s)(\d+)\.tsv$', f)
    sz = os.path.getsize(p)
    if m and m.group(1) in keep:
        shutil.move(p, os.path.join(dst, f)); moved += 1; mb += sz
    else:
        os.remove(p); dropped += 1; db += sz
print(f'moved {moved} ({mb/1e9:.2f}GB) dropped {dropped} ({db/1e9:.2f}GB)')
