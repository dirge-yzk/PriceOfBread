# (1150) 合流：合わせた木の M の生と線の腕 B の生を表ごとにバイトで比べる（予言 [3]＝cm の表〔rule_s・trrail_s・corncf_s・rponset_s〕と既知の揺れ〔burscan_s・clock_s〕を除いて一致）。
#   python -X utf8 -I rawcmp.py <M の ins> <腕 B の ins> <出力 tsv>
import os, sys, filecmp, re
A, B, OUT = sys.argv[1:4]
fa = set(os.listdir(A)); fb = set(os.listdir(B))
rows = []; cnt = {}
for f in sorted(fa | fb):
    m = re.match(r'^(.+_s)\d+\.tsv$', f); t = m.group(1) if m else f
    if f not in fa: k = 'Bだけ'
    elif f not in fb: k = 'Mだけ'
    else: k = '一致' if filecmp.cmp(os.path.join(A, f), os.path.join(B, f), shallow=False) else '違う'
    rows.append((f, t, k)); cnt.setdefault((t, k), 0); cnt[(t, k)] += 1
with open(OUT, 'w', encoding='utf-8', newline='\n') as w:
    w.write('file\ttable\tkind\n')
    for r in rows: w.write('\t'.join(r) + '\n')
tot = {}
for (t, k), n in cnt.items(): tot[k] = tot.get(k, 0) + n
print('合計', ' '.join(f'{k}={v}' for k, v in sorted(tot.items())))
for (t, k), n in sorted(cnt.items()):
    if k != '一致': print(t, k, n)
