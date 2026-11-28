# (1433) c154rx 突然変異：python mut.py <木> <M1|M2|M3>（バイトで読み書き）
import sys
t,m=sys.argv[1],sys.argv[2]
p=t+'/PriceOfBread.SimCore/Diplomacy.cs'
b=open(p,'rb').read().decode('utf-8')
def sub(o,n):
    global b
    assert b.count(o)==1,o
    b=b.replace(o,n)
if m=='M1':   # 違う宗教の組に 0.03×dist を Ethnic の札で積み戻す
    sub('                AddTensionKeyed(k, c.LawTensionPerMonth * Math.Abs(na.Freedom - nb.Freedom), TensionReason.Law);',
        '                AddTensionKeyed(k, c.LawTensionPerMonth * Math.Abs(na.Freedom - nb.Freedom), TensionReason.Law);\r\n                if (a.Religion.Length != 0 && b.Religion.Length != 0 && a.Religion != b.Religion) AddTensionKeyed(k, 0.03 * ReligionDistance(a, b, c), TensionReason.Ethnic);')
elif m=='M2': # GovCost の ReligionDistance を 0 に
    sub('0.5 * ReligionDistance(w, l, c);','0.5 * 0.0;')
elif m=='M3': # 保護の項を消す
    sub('bool protector = c.ProtectorTensionPerMonth > 1e-12;','bool protector = false;')
open(p,'wb').write(b.encode('utf-8'))
