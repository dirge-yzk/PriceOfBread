# 英仏普西蘭露の本国（Core）の州から出る量：国内の移民（入る州の格ごと）・入植・越境（1783〜1950 の和・千人）
import sys, os, collections, statistics as st
NAT=['イギリス','フランス','プロイセン','スペイン','オランダ','ロシア','ポルトガル']
def reg(d,sd):
    own={}; agg=collections.defaultdict(float)
    with open(f'{d}/region_s{sd}.tsv',encoding='utf-8') as fh:
        h=next(fh).rstrip('\n').split('\t'); ix={k:i for i,k in enumerate(h)}
        for ln in fh:
            c=ln.rstrip('\n').split('\t'); q=int(c[0]); n=c[3]; r=c[4]; t=c[5]
            own[(q,r)]=(n,t)
            if n in NAT and t=='Core':
                agg[(n,'settler')]+=float(c[ix['settlerOutKPerQuarter']])
                agg[(n,'xb')]+=float(c[ix['crossBorderOutKPerQuarter']])
            if n in NAT: agg[(n,'settlerIn_'+t)]+=float(c[ix['settlerInKPerQuarter']])
    return own,agg
def mig(d,sd,own,agg):
    with open(f'{d}/migration_s{sd}.tsv',encoding='utf-8') as fh:
        for ln in fh:
            c=ln.rstrip('\n').split('\t')
            if len(c)!=4 or not c[0].isdigit(): continue
            q=int(c[0]); s=own.get((q,c[1])); dd=own.get((q,c[2]))
            if not s or not dd: continue
            if s[0] in NAT and s[1]=='Core':
                key='int_'+('same' if dd[0]==s[0] else 'other')+'_'+dd[1]
                agg[(s[0],key)]+=float(c[3])
            if s[0] in NAT and s[1]=='Core' and dd[0]==s[0] and dd[1]=='Colonial': pass
if __name__=='__main__':
    for arm in sys.argv[1:]:
        res=collections.defaultdict(list)
        for sd in range(1,11):
            own,agg=reg(arm,sd); mig(arm,sd,own,agg)
            keys=set(k for k in agg)
            for n in NAT:
                for k in ['settler','xb','int_same_Core','int_same_Colonial','int_same_Integrated','settlerIn_Colonial','settlerIn_Core']:
                    res[(n,k)].append(agg.get((n,k),0.0))
        print('##',arm)
        for n in NAT:
            print(n,'\t'.join(f'{k} {st.median(res[(n,k)]):.0f}' for k in ['settler','xb','int_same_Core','int_same_Colonial','int_same_Integrated','settlerIn_Colonial','settlerIn_Core']))
