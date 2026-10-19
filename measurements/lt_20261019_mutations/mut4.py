p='D:/PoB_work/lt/mut/PriceOfBread.SimCore/SelfPlay.cs'
b=open(p,'rb').read().decode('utf-8')
for a,c in (('return w.Antitrust >= AntitrustLaw.CartelBan ? null : AntitrustLaw.CartelBan;','return AntitrustLaw.CartelBan;'),
            ('return w.Guild >= GuildLaw.Limited ? null : GuildLaw.Limited;','return GuildLaw.Limited;')):
    assert b.count(a)==1
    b=b.replace(a,c)
open(p,'wb').write(b.encode('utf-8'))
