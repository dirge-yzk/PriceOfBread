"""(244) fullS の注入の中の曖昧な旧名（同じ名前の別の量がある）を、受け手の変数名で型を見分けて新しい名前へ直す。
python renamb.py <注入.py> ...   w.／yw.＝World・sp.＝Power・p.／ypp.・[SoldierPopIndex]・[InvestorPopIndex]＝Pop。生産者の s.LastRevenue は直さない。
受け手の名前は注入ごとに違う＝新しい注入に当てる前に grep で受け手を確かめること。"""
import re, sys, os

RULES = [
    (r'(\b(?:w|yw)\.)LastRevenue(?![A-Za-z0-9_])', 'LastRevenuePerMonth'),
    (r'(\b(?:w|yw)\.)NominalGdp(?![A-Za-z0-9_])', 'NominalGdpPerMonth'),
    (r'(\b(?:w|sp)\.)Gdp(?![A-Za-z0-9_])', 'GdpPerMonth'),
    (r'(\b(?:p|ypp)\.)Earned(?![A-Za-z0-9_])', 'EarnedPerMonth'),
    (r'(SoldierPopIndex\]\.)Earned(?![A-Za-z0-9_])', 'EarnedPerMonth'),
    (r'(InvestorPopIndex\]\.)LandRent(?![A-Za-z0-9_])', 'LandRentPerMonth'),
]

for p in sys.argv[1:]:
    b = open(p, 'rb').read()
    bom = b[:3] == b'\xef\xbb\xbf'
    s = (b[3:] if bom else b).decode('utf-8')
    tot = 0
    for rx, new in RULES:
        n = [0]

        def f(m, new=new):
            n[0] += 1
            return m.group(1) + new
        s = re.sub(rx, f, s)
        if n[0]:
            tot += n[0]
            print(os.path.relpath(p), new, n[0])
    if tot:
        open(p, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + s.encode('utf-8'))
