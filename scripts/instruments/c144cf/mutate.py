import sys
p='PriceOfBread.SimCore/Instruments/InstrumentsCwend.cs'
s=open(p,'rb').read().decode('utf-8')
M={
1:('war.ForcedPeace ? "forced" : System.Math.Abs','war.ForcedPeace ? "forced" : collapse ? "collapse" : System.Math.Abs'),
2:('Reserved = parent.Nation?.SecedingRegion is { } seat && rebel.Nation is { } rn && rn.Regions.Contains(seat),','Reserved = born is not null && born.CapitalRising,'),
3:('bool entryExit = war.CivilWarSecession && war.ForcedPeace;','bool entryExit = false;'),
4:('Collapse = collapse,','Collapse = false,'),
}
a,b=M[int(sys.argv[1])]
assert s.count(a)==1
open(p,'wb').write(s.replace(a,b).encode('utf-8'))
