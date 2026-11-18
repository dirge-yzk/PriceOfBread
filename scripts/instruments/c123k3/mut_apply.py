# (1222) c123k3 突然変異を木に 1 本入れる（バイトで読み書き・ちょうど 1 回の置き換え）。使い方: python mut_apply.py <木> <M1..M3>
import sys
tree, m = sys.argv[1], sys.argv[2]
F = 'PriceOfBread.SimCore/Simulation.cs'
MUT = {
    'M1': ("        double healthcare = c.HealthcareLiteracyGateOff > 0\n", "        double healthcare = true\n"),
    'M2': ("        if (c.HealthcareRealGate > 0) healthcare = Math.Min(healthcare, doctorCare / c.HealthcareRealGate);",
           "        if (c.HealthcareRealGate > 0 && !(c.HealthcareLiteracyGateOff > 0)) healthcare = Math.Min(healthcare, doctorCare / c.HealthcareRealGate);"),
    'M3': ("        if (c.DoctorHealthcareSubstitute > 0) healthcare = Math.Max(healthcare, c.DoctorHealthcareSubstitute * doctorCare);",
           "        if (false) healthcare = Math.Max(healthcare, c.DoctorHealthcareSubstitute * doctorCare);"),
}
o, n = MUT[m]; p = f'{tree}/{F}'
b = open(p, 'rb').read(); ob, nb = o.encode('utf-8'), n.encode('utf-8')
if b.count(ob) != 1: print(f'{m} 置き換えの旧が {b.count(ob)} 回'); sys.exit(1)
open(p, 'wb').write(b.replace(ob, nb)); print(f'{m} 入れた：{p}')
