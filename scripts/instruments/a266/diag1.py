import csv,glob,os,sys,collections
def load(d):
    res={}
    for sd in range(1,11):
        f=f'{d}/fiscal_s{sd}.tsv'
        if not os.path.exists(f): continue
        with open(f,encoding='utf-8') as fh:
            r=csv.DictReader(fh,delimiter='\t')
            rows=[x for x in r if x['nation']=='イギリス']
        res[sd]=rows
    return res
A=load('out_A_10/ins'); B=load('out_B_5/ins')
def fl(x):
    try: return float(x)
    except: return float('nan')
print('seed  borrow A / B      short A/B   claimWelfare A/B  interest A/B   debtEnd A/B   claimAdmin A/B')
for sd in sorted(B):
    a=A[sd]; b=B[sd]
    f=lambda rows,c: sum(fl(x[c]) for x in rows if fl(x[c])==fl(x[c]))
    sh=lambda rows: sum(1 for x in rows if fl(x['spareBeforeBorrowPerMonth'])<0)
    print(sd, f'{f(a,"borrowedPerMonth"):.0f}/{f(b,"borrowedPerMonth"):.0f}', f'{sh(a)}/{sh(b)}', f'{f(a,"claimWelfarePerMonth"):.0f}/{f(b,"claimWelfarePerMonth"):.0f}', f'{f(a,"claimInterestPerMonth"):.0f}/{f(b,"claimInterestPerMonth"):.0f}', f'{fl(a[-1]["govDebtStockEND"]) if "govDebtStockEND" in a[-1] else fl(a[-1]["govDebtStock"]):.0f}/{fl(b[-1]["govDebtStock"]):.0f}', f'{f(a,"claimAdminPerMonth"):.0f}/{f(b,"claimAdminPerMonth"):.0f}')
