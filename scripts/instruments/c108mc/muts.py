# (1119) c108mc の突然変異（c105mc/muts.py の写し＋μ9・事前登録 measurements/m1119_20261114_prereg_mc.txt §突然変異）。使い捨ての木の EconomyStep.cs をバイトで書き換える。
#   python -X utf8 -I muts.py <木> <μ名>   ＝置き換えが 1 か所ずつ当たったら 0・当たらなければ 2
import sys
T = sys.argv[1].rstrip('/'); MU = sys.argv[2]
P = T + '/PriceOfBread.SimCore/EconomyStep.cs'
CAT = b'catMax = Catalog.IsMineral(s.GoodId) ? (coef.MineralScaleCapLift > 0 ? coef.ScaleMax : coef.MineralScaleCap) : coef.ScaleMax;'
SPD = b'catSpeed = coef.ScaleAdjustSpeed * coef.RgoScaleSpeedFactor;'
FLR = b's.BuiltScale * (1.0 - s.MothballFrac.Value)), catMax);'
HEAD = b'double catMax = coef.ScaleMax, catSpeed = coef.ScaleAdjustSpeed;'
R = {
    'mu0': [],   # 対照＝書き換えなし（緑のはず）
    'mu1': [(CAT, b'catMax = Catalog.IsMineral(s.GoodId) ? (coef.MineralScaleCapLift > 0 ? coef.MineralScaleCap : coef.MineralScaleCap) : coef.ScaleMax;')],
    'mu2': [(SPD, b'catSpeed = coef.MineralScaleCapLift > 0 && Catalog.IsMineral(s.GoodId) ? coef.ScaleAdjustSpeed : coef.ScaleAdjustSpeed * coef.RgoScaleSpeedFactor;')],
    'mu3': [(CAT, b'catMax = Catalog.IsMineral(s.GoodId) ? coef.MineralScaleCap : (coef.MineralScaleCapLift > 0 ? coef.ScaleMax : coef.ScaleMax);')],
    'mu4': [(FLR, b's.BuiltScale * (1.0 - s.MothballFrac.Value)), s.IsRgo && s.GoodId != Catalog.FishId && Catalog.IsMineral(s.GoodId) ? coef.MineralScaleCap : catMax);')],
    'mu5': [(CAT, b'catMax = Catalog.IsMineral(s.GoodId) ? (coef.MineralScaleCapLift > 0 ? double.PositiveInfinity : coef.MineralScaleCap) : coef.ScaleMax;')],
    'mu6': [(HEAD, b'double catMax = coef.ScaleMax, catSpeed = coef.MineralScaleCapLift > 0 ? coef.ScaleAdjustSpeed * 1.0000001 : coef.ScaleAdjustSpeed;')],
    'mu7': [(CAT, b'catMax = Catalog.IsMineral(s.GoodId) ? (coef.MineralScaleCapLift > 0 ? 2.0 : coef.MineralScaleCap) : coef.ScaleMax;')],
    'mu8': [(CAT, b'catMax = Catalog.IsMineral(s.GoodId) ? coef.MineralScaleCap : coef.ScaleMax;'),
            (FLR, b's.BuiltScale * (1.0 - s.MothballFrac.Value)), coef.MineralScaleCapLift > 0 ? coef.ScaleMax : catMax);')],
    'mu9': [(b', floor, catMax);', b', floor, s.IsRgo && s.GoodId != Catalog.FishId && Catalog.IsMineral(s.GoodId) ? Math.Max(floor, coef.MineralScaleCap) : catMax);')],   # Clamp の上側だけ max(floor, MineralScaleCap)
}
b = open(P, 'rb').read()
for old, new in R[MU]:
    if b.count(old) != 1: print(MU, '当たらない', old[:60]); sys.exit(2)
    b = b.replace(old, new)
open(P, 'wb').write(b)
print(MU, '適用')
