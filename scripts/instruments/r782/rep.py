# (784) 読み手 r782 の台本（使い捨ての写し・元は Temp）・入力のパスは当時の固定の値（keep_778）
import sys, csv, os
d=sys.argv[1]; s0=int(sys.argv[2]) if len(sys.argv)>2 else 1; Y1=int(sys.argv[3]) if len(sys.argv)>3 else 1830; nat='イギリス'; verbose='-v' in sys.argv
def F(x):
    try: return float(x)
    except: return 0.0
tot=dict(n=0,over=0,over_closed=0,def12=0,over_def12=0,seeds_over=0)
for s in range(s0,s0+10):
    fp=os.path.join(d,f'fiscal_s{s}.tsv')
    if not os.path.exists(fp): continue
    with open(fp,encoding='utf-8') as f:
        r=csv.reader(f,delimiter='\t'); h=next(r); ix={k:i for i,k in enumerate(h)}
        fr=[x for x in r if x[ix['nation']]==nat]
    dm=[int(x[0]) for x in fr if F(x[ix['defaultCount']])>0]; dm0=dm[0] if dm else None
    prevT=None; prevIR=None; sover=False
    for x in fr:
        y=int(x[ix['year']]); m=int(x[0])
        w=-F(x[ix['ledgerWarCostPerMonth']])
        if w>1 and y<=Y1 and (dm0 is None or m<=dm0):
            T=prevT if prevT is not None else 0
            over = w > max(0,T)
            d12 = dm0 is not None and 0<=dm0-m<=12
            tot['n']+=1; tot['over']+=over; tot['over_closed']+= over and (prevIR or 0)>=0.67; tot['def12']+=d12; tot['over_def12']+= over and d12; sover|=over
            if verbose: print(f's{s} m{m} y{y} rep={w:.0f} T_prev={T:.0f} over={int(over)} iR_prev={prevIR:.3f} rev={F(x[ix["revenueBasePerMonth"]]):.0f} def<=12={int(d12)}')
        prevT=F(x[ix['treasuryEndStock']]); R=F(x[ix['revenueBasePerMonth']]); prevIR=F(x[ix['claimInterestPerMonth']])/R if R>0 else 9
    tot['seeds_over']+=sover
print(d, tot)
