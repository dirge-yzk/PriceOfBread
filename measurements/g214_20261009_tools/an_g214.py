import sys,glob,statistics as st,os
"""(214) 計器の読み手：python an_g214.py <out_dir> ...  W 行＝世界×四半期（比・産出・金の国の数）・O 行＝産地の開いた年"""
def f(x):
    try: return float(x)
    except: return float('nan')
def load(d):
    res={}
    for p in sorted(glob.glob(d+'/s*.txt')):
        seed=int(os.path.basename(p)[1:-4]); W={};O=[];N=[]
        for ln in open(p,encoding='utf-8'):
            if ln[0]=='W': 
                c=ln.rstrip('\n').split('|'); W[int(c[1])]=c
            elif ln[0]=='O': O.append(ln.rstrip('\n').split('|'))
            elif ln[0]=='N': N.append(ln.rstrip('\n').split('|'))
        res[seed]=(W,O,N)
    return res
def yearq(W,yr):
    # last quarter of year yr
    qs=[q for q,c in W.items() if int(c[2])==yr]
    return max(qs) if qs else None
if __name__=='__main__':
    for d in sys.argv[1:]:
        print('==',d)
        R=load(d)
        for seed,(W,O,N) in R.items():
            print('seed',seed,'opens:',[(o[2],o[3],o[4],o[5]) for o in O if o[5] in ('銀','金')])
            row=[]
            for yr in (1783,1813,1843,1858,1873,1883,1893,1903,1913,1933,1950):
                q=yearq(W,yr)
                if q is None: continue
                c=W[q]
                # idx: 0 W,1 q,2 yr,3 R金,4 n,5 Ag,6 Au,7 R銀,8 n,9,10,11 R不換,12,13,14,15 R全,16 n,17,18,19 prodAg,20 prodAu,21 demAg,22 demAu,23 stAg,24 stAu,25 mintAg,26 mintAu,27 sellAg,28 sellAu,29 resAg,30 resAu,31 statRel,32 nGold,33 nSilver,34 nFiat
                row.append(f"{yr}:R{f(c[15]):.2f} Ag{f(c[19]):.3g} G{c[32]}/S{c[33]}")
            print('  ','  '.join(row))
