import re,glob
R='D:/PoB_work/wt_c144wr/PriceOfBread.SimCore'
def strip(t):
    t=re.sub(r'"(?:[^"\\n]|\.)*"','""',t)
    t=re.sub(r'//[^\n]*','',t)
    t=re.sub(r'/\*.*?\*/',lambda m:'\n'*m.group(0).count('\n'),t,flags=re.S)
    return t
def old(files,m):
    rx=re.compile(r'\.%s(\[[^\]]*\])?\s*(\+=|-=|\*=|/=|=(?![=>]))'%m);n=0
    for f in files:
        for l in open(f,encoding='utf-8'):
            c=l.find('//'); 
            if c>=0:l=l[:c]
            n+=len(rx.findall(l))
    return n
def new(files,m):
    rx=re.compile(r'\.%s(\[[^\]]*\])?\s*(\+=|-=|\*=|/=|=(?![=>]))'%m);n=0
    for f in files:
        n+=len(rx.findall(strip(open(f,encoding='utf-8').read())))
    return n
top=glob.glob(R+'/*.cs');ins=glob.glob(R+'/Instruments/*.cs')
for m in "Militancy Satisfaction Legitimacy Consciousness Savings StateSpent FiscalFundingRatio CumulativeTradeFlow CumulativeFiatIssued CumulativeGoldMinted Loans HouseholdLoans PaidPerMonth EarnedPerMonth Treasury LandShares WarExhaustion DynasticMandate".split():
    print(m,old(top+ins,m),new(top,m))
