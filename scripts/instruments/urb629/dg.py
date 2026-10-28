import sys
f=sys.argv[1]; step=int(sys.argv[2])
print('month rural urban incR incU rawTarget target cur flow uFill rFill rSub')
for line in open(f,encoding='utf-8'):
    c=line.rstrip('\n').split('\t')
    n=int(c[1])
    if n%step==0:
        v=[float(x) for x in c[2:]]
        print('%4d(%d) rur%7.1f urb%7.1f incR%.3f incU%.3f raw%.3f tgt%.3f cur%.3f flow%+.2f uF%.2f rF%.2f sub%.2f'%(n,1783+n//12,v[0],v[1],v[2],v[3],v[4],v[5],v[6],v[7],v[8],v[9],v[10]))
