import csv,sys
def rows(d,sd,kind,nat='イギリス'):
    with open(f'{d}/{kind}_s{sd}.tsv',encoding='utf-8') as fh:
        return [x for x in csv.DictReader(fh,delimiter='\t') if x['nation']==nat]
def fl(x):
    try: return float(x)
    except: return float('nan')
for sd in (1,3,4):
    for lab,d in (('A','out_A_10/ins'),('B','out_B_5/ins')):
        f=rows(d,sd,'fiscal'); n=rows(d,sd,'nation')
        w=[(int(x['monthIndex'])//3, fl(x['claimWelfarePerMonth'])) for x in f if fl(x['claimWelfarePerMonth'])>0]
        first=w[0][0] if w else None
        ss=[(int(x['quarter']),fl(x['socialSecurityEndShare']),fl(x['socialSecurityTargetEndShare']),int(fl(x['socialSecurityLawEndStage']))) for x in n]
        mid=[s for s in ss if s[0] in (60,120,200,300,400,500)]
        tc=sum(fl(x['claimTotalPerMonth']) for x in f); wc=sum(v for _,v in w)
        print(sd,lab,'welfare first quarter',first,'months>0',len(w),'welfare/total claims %.3f'%(wc/tc), 'socialSecurity(q,end,target,stage):',mid)
