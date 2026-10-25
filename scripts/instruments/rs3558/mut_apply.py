import sys
which = sys.argv[1]
ROOT = 'D:/PoB_work/wt_rs3/PriceOfBread.SimCore/'
def edit(fn, old, new, count=1):
    p = ROOT + fn
    b = open(p, 'rb').read().decode('utf-8')
    assert b.count(old) == count, (fn, old, b.count(old))
    open(p, 'wb').write(b.replace(old, new).encode('utf-8'))
if which == '1':   # 減らす向きで損を読む
    edit('PolicyPreferences.cs', '? (!increasing && w.Coef.LordOppositionReverseGain > 0 ? LordIncomeGain(w, a, e.LordRentToIncome) : LordIncomeLoss(w, a, e.LordRentToIncome))',
         '? (!increasing && w.Coef.LordOppositionReverseGain > 0 ? LordIncomeLoss(w, a, e.LordRentToIncome) : LordIncomeLoss(w, a, e.LordRentToIncome))')
elif which == '2': # 戻した段＝今の段（得 0）
    edit('PolicyPreferences.cs', 'lv == ReformLevel.None ? ReformLevel.None : lv - 1;', 'lv;')
elif which == '3': # 分母を戻した段にする
    edit('PolicyPreferences.cs', 'return now > 1e-9 ? (prev - now) / now * lordRentToIncome : 0.0;', 'return prev > 1e-9 ? (prev - now) / prev * lordRentToIncome : 0.0;')
elif which == '4': # 進める向きにも得を使う
    edit('PolicyPreferences.cs', '(!increasing && w.Coef.LordOppositionReverseGain > 0 ?', '(w.Coef.LordOppositionReverseGain > 0 ?')
elif which == '5': # ComputeDemands が強さを取り直さない
    edit('FactionSnapshot.cs', 'if (!increase && PolicyPreferences.DirectionAware(w, axis, bloc)) strength = System.Math.Abs(PolicyPreferences.Pref(axis, bloc, e, w, false));', '')
elif which == '6': # 旗 rs3 を無視する（旗 0 でも減らす向きは得）
    edit('PolicyPreferences.cs', '(!increasing && w.Coef.LordOppositionReverseGain > 0 ?', '(!increasing ?')
elif which == '7': # DirectionAware が土地改革を落とす
    edit('PolicyPreferences.cs', '(a == PolicyAxis.SerfEmancipation || a == PolicyAxis.LandReform);', '(a == PolicyAxis.SerfEmancipation);')
elif which == '8': # ParliamentNet が向きを渡さず true
    edit('PolicyPreferences.cs', 'sum += w.PartySeats[party] * Pref(a, party, e, w, sign > 0) * sign;', 'sum += w.PartySeats[party] * Pref(a, party, e, w, true) * sign;')
else:
    raise SystemExit('bad')
