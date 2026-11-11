# (1005) c92tf：latch_1001.txt §3 から形と列を抜く。使い方：python -X utf8 -I ext.py <latch_1001.txt> <書き先> <形の頭,...> <列名,...>
import sys
L=open(sys.argv[1],'rb').read().decode('utf-8').split('\n')
want=sys.argv[3].split(',')
out=[]; cur=None; hdr=None
for l in L:
    if l.startswith('### '): cur=l[4:]; continue
    if l.startswith('## 4'): cur=None
    if cur is None: continue
    c=l.split('\t')
    if c[0]=='国': hdr=c; continue
    if len(c)<5: continue
    if not any(cur.startswith(w) for w in want): continue
    idx=[hdr.index(k) for k in sys.argv[4].split(',')]
    out.append(cur.split('（')[0]+'\t'+c[0]+'\t'+'\t'.join(c[i] for i in idx))
open(sys.argv[2],'wb').write(('\n'.join(out)+'\n').encode('utf-8'))
