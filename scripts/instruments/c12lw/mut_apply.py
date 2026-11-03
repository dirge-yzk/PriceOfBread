# (803) c12lw 突然変異：焼き込み後のソースに当てる（使い捨ての木で）。使い方: python mut_apply.py <木> <M1..M6>
# 置換は 1 回だけ一致を要求する（CRLF の Politics.cs も扱う）。
import sys, os
tree, m = sys.argv[1], sys.argv[2]
MUT = {
 'M1': ('PriceOfBread.SimCore/PolicyPreferences.cs',   # 減らす向きも損の式で読む（得の式を外す）
        '=> !increasing ? LordIncomeGain(w, a, e.LordRentToIncome) : LordIncomeLoss(w, a, e.LordRentToIncome);',
        '=> LordIncomeLoss(w, a, e.LordRentToIncome);'),
 'M2': ('PriceOfBread.SimCore/PolicyPreferences.cs',   # 旧の式（貯蓄の割合 W0）へ戻す
        '=> !increasing ? LordIncomeGain(w, a, e.LordRentToIncome) : LordIncomeLoss(w, a, e.LordRentToIncome);',
        '=> e.Wealth[0];'),
 'M3': ('PriceOfBread.SimCore/PolicyPreferences.cs',   # 要求の順位が減らす向きを取り直さない
        '=> b == 0 && (a == PolicyAxis.SerfEmancipation || a == PolicyAxis.LandReform);',
        '=> false;'),
 'M4': ('PriceOfBread.SimCore/Government.cs',          # 壁の一歩の止めを外す（S へ代価の全額）
        'if (!(w.MandateSpent + amount <= w.PoliticalCapitalMax))',
        'if (false)'),
 'M5': ('PriceOfBread.SimCore/Politics.cs',            # 好機のブレーキを外す（体制は常に弱み）
        '                         || externalWar;\n',
        '                         || externalWar || true;\n'),
 'M6': ('PriceOfBread.SimCore/Politics.cs',            # 外征を弱みに数えない
        'externalWar = true; break; }',
        'break; }'),
}
rel, old, new = MUT[m]
p = os.path.join(tree, rel)
b = open(p, 'rb').read()
o, n = old.encode('utf-8'), new.encode('utf-8')
if b'\r\n' in b:
    o, n = o.replace(b'\n', b'\r\n'), n.replace(b'\n', b'\r\n')
c = b.count(o)
assert c == 1, f'{m}: {rel} で {c} 件'
open(p, 'wb').write(b.replace(o, n))
print(f'{m}: applied to {rel}')
