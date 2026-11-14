# (1138) c111tp 突然変異の編集（使い捨ての木だけ・バイトで読み書き）：python mut_edit.py <型> <木>
#   型：flag1＝旗の既定を 1 に／Ma＝旗 1＋Core も外す／Mb＝旗 1＋TreatyPortInForce だけ直す（ForeignPortShare は旧）／Mc＝旗 1＋旗を読まない／Md＝旗 1＋Integrated も外す
import sys, os
mode, tree = sys.argv[1], sys.argv[2]
def rep(rel, old, new, count=1):
    p = os.path.join(tree, rel); d = open(p, 'rb').read()
    n = d.count(old.encode('utf-8'))
    if n != count: sys.exit(f'EDITFAIL {rel}: {n} 箇所（期待 {count}）')
    open(p, 'wb').write(d.replace(old.encode('utf-8'), new.encode('utf-8')))
P = 'PriceOfBread.SimCore/Politics.cs'
FP = 'if (w.Coef.TreatyPortHomeOnly > 0 && r.Tier == RegionTier.Colonial) continue;'
TP = '!(c.TreatyPortHomeOnly > 0 && r.Tier == RegionTier.Colonial)'
rep('PriceOfBread.SimCore/Coefficients.p08_audit_soldiers.cs', 'public double TreatyPortHomeOnly = 0;', 'public double TreatyPortHomeOnly = 1;')
if mode == 'Ma':
    rep(P, FP, FP.replace('r.Tier == RegionTier.Colonial', 'r.Tier != RegionTier.Integrated'))
    rep(P, TP, TP.replace('r.Tier == RegionTier.Colonial', 'r.Tier != RegionTier.Integrated'))
elif mode == 'Mb':
    rep(P, FP, '')
elif mode == 'Mc':
    rep(P, FP, FP.replace('w.Coef.TreatyPortHomeOnly > 0', 'false'))
    rep(P, TP, TP.replace('c.TreatyPortHomeOnly > 0', 'false'))
elif mode == 'Md':
    rep(P, FP, FP.replace('r.Tier == RegionTier.Colonial', 'r.Tier != RegionTier.Core'))
    rep(P, TP, TP.replace('r.Tier == RegionTier.Colonial', 'r.Tier != RegionTier.Core'))
elif mode != 'flag1':
    sys.exit('EDITFAIL 型が無い ' + mode)
print('編集', mode, tree)
