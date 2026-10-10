"""(190) 新しい木用：inj_mil4（m180/inj_mil8 の連鎖）が書く首都の FleetStock の列を艦の強さの値（EconomyStep.FleetPowerValue＝国の和）へ。python fix_mil4.py <木の根>"""
import sys
p = sys.argv[1] + '/PriceOfBread.SimCore/SelfPlay.cs'
raw = open(p, 'rb').read(); bom = raw.startswith(b'\xef\xbb\xbf'); s = raw.decode('utf-8-sig')
a = '{yw.Region.FleetStock:F2}'
assert s.count(a) == 1, s.count(a)
s = s.replace(a, '{EconomyStep.FleetPowerValue(yw):F2}')
open(p, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + s.encode('utf-8'))
print('ok mil4', sys.argv[1])
