import sys
m=sys.argv[1]; root='D:/PoB_work/c104cp_work/mut/PriceOfBread.SimCore/'
def sub(f,a,b):
    p=root+f; t=open(p,'rb').read().decode('utf8'); assert a in t,(f,a); open(p,'wb').write(t.replace(a,b,1).encode('utf8'))
if m=='M2': sub('PowerCenter.cs','return (l / total, a / total, v / total);','return (l / total, a / total, v / total * 0.5);')
if m=='M3': sub('InstrumentsCenter.cs','double g = PowerCenter.GOf(lp, wUse), h = PowerCenter.HOf(lp, wUse, popK * qv);','double h = PowerCenter.HOf(lp, wUse, popK * qv), g = h;')
if m=='M5': sub('PowerCenter.cs','double sum = g + q * mExt;','double sum = g + mExt;')
