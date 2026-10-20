import sys,collections
sys.stdout.reconfigure(encoding='utf-8')
fn,nat=sys.argv[1],sys.argv[2]; cols=sys.argv[3].split(','); step=int(sys.argv[4]) if len(sys.argv)>4 else 10; lo=int(sys.argv[5]) if len(sys.argv)>5 else 0; hi=int(sys.argv[6]) if len(sys.argv)>6 else 400
hdr=None
print('q\t'+'\t'.join(cols))
for line in open('D:/PoB_work/bk473/'+fn+'.tsv',encoding='utf-8'):
    if line.startswith('#'): continue
    x=line.rstrip('\n').split('\t')
    if hdr is None: hdr=x; ix={h:i for i,h in enumerate(hdr)}; continue
    if x[1]!=nat: continue
    q=int(x[0])
    if q<lo or q>hi or (q-lo)%step: continue
    print(q,'\t'.join(('%.4g'%float(x[ix[c]])) for c in cols),sep='\t')
