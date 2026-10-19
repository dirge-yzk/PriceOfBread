p='D:/PoB_work/lt/mut/PriceOfBread.SimCore/SelfPlay.cs'
b=open(p,'rb').read().decode('utf-8')
a='internal static bool GoldWishFromBoard(World w) => w.MonetaryLaw == MonetaryStandardLaw.Gold || PoliticalQuantities'
assert b.count(a)==1
b=b.replace(a,'internal static bool GoldWishFromBoard(World w) => PoliticalQuantities')
open(p,'wb').write(b.encode('utf-8'))
