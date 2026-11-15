# (1157) 合流：合わせた木の M の生を、線の腕 B の生の表ごとの sha1（縮める前に取った trim1157.py の tsv）と比べる（予言 [3]＝cearn_s・cpop_s〔経路の名の行〕と既知の揺れ〔burscan_s・clock_s・dep_s〕を除いて一致）。
#   python -X utf8 -I rawcmp_sha.py <M の ins> <腕 B の sha1 の tsv> <出力 tsv>
import hashlib, os, re, sys
A, TSV, OUT = sys.argv[1:4]
ref = {}
for ln in open(TSV, encoding='utf-8').read().rstrip('\n').split('\n')[1:]:
    c = ln.split('\t'); ref[c[1]] = c[3]
fa = sorted(f for f in os.listdir(A) if os.path.isfile(os.path.join(A, f)))
rows = []; cnt = {}
for f in sorted(set(fa) | set(ref)):
    m = re.match(r'^(.+_s)\d+\.tsv$', f); t = m.group(1) if m else f
    if f not in ref: k = 'Mだけ'
    elif f not in fa: k = 'Bだけ'
    else:
        h = hashlib.sha1()
        with open(os.path.join(A, f), 'rb') as r:
            for b in iter(lambda: r.read(1 << 22), b''): h.update(b)
        k = '一致' if h.hexdigest() == ref[f] else '違う'
    rows.append((f, t, k)); cnt[(t, k)] = cnt.get((t, k), 0) + 1
with open(OUT, 'w', encoding='utf-8', newline='\n') as w:
    w.write('file\ttable\tkind\n')
    for r in rows: w.write('\t'.join(r) + '\n')
tot = {}
for (t, k), n in cnt.items(): tot[k] = tot.get(k, 0) + n
print('合計', ' '.join(f'{k}={v}' for k, v in sorted(tot.items())))
for (t, k), n in sorted(cnt.items()):
    if k != '一致': print(t, k, n)
