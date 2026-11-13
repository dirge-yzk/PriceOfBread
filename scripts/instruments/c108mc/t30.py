import sys,statistics as st
D=sys.argv[1]
Y8=(1850,1860,1870,1880,1890,1900,1910,1913)
for s in range(1,11):
    O=T15=T30=0.0
    with open(f'{D}/minecap_s{s}.tsv',encoding='utf-8') as f:
        h=f.readline().rstrip('\n').split('\t');ix={k:i for i,k in enumerate(h)}
        for l in f:
            p=l.rstrip('\n').split('\t')
            if int(p[ix['year']]) not in Y8: continue
            o=float(p[ix['outputSum']])
            if not o>0: continue
            O+=o;T15+=o*float(p[ix['target15Share']]);T30+=o*float(p[ix['target30Share']])
    print(s,round(T15/O,4),round(T30/O,4))
