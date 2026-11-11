# (892) 合流：腕 Z の計器の生と線の ident の生を表ごとにバイトで比べる（予言 [4] の最後の行・記録）。
#   python -X utf8 cmpline892.py <Z の ins> <線の ins> <表の頭,...|all>
import filecmp, os, re, sys
z, l, heads = sys.argv[1:4]
fs = sorted(os.listdir(z))
if heads != 'all':
    hs = heads.split(',')
    fs = [f for f in fs if (m := re.match(r'^(.*?_s)(\d+)\.tsv$', f)) and m.group(1) in hs]
same = diff = miss = 0; dl = []
for f in fs:
    lp = os.path.join(l, f)
    if not os.path.exists(lp): miss += 1; continue
    if filecmp.cmp(os.path.join(z, f), lp, shallow=False): same += 1
    else: diff += 1; dl.append(f)
print(f'{l}  heads={heads}  files {len(fs)}  same {same}  diff {diff}  missing {miss}')
for f in dl[:40]: print('  diff', f)
