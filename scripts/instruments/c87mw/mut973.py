# (973) c87mw 突然変異を使い捨ての木へ当てる（事前登録 measurements/m973_20261108_prereg_mw.txt §5・(959) mut959.py の写し＝当てる字は今の木でも 1 件ずつ一致）。使い方: python mut973.py <木> <M0|M1|M2|M3|M4|M5|M6>
#   バイトで読み書き（CR の数と BOM を保つ）。当てる前の木は点火の状態（旗の既定 1・番人を直した後）の素の状態であること（git checkout で戻してから呼ぶ）。
import sys, os
T, M = sys.argv[1], sys.argv[2]
SIM = os.path.join(T, "PriceOfBread.SimCore", "Simulation.cs")
COEF = os.path.join(T, "PriceOfBread.SimCore", "Coefficients.cs")
DIP = os.path.join(T, "PriceOfBread.SimCore", "Diplomacy.cs")

def rep(p, old, new):
    b = open(p, "rb").read(); o = old.encode(); n = new.encode()
    if b.count(o) != 1: sys.exit(f"{M}: {p} の一致 {b.count(o)} 件（1 件であること）")
    open(p, "wb").write(b.replace(o, n))

ENG = "double warEngageNat = World.Coef.MilitaryWarMultFromContact > 0 && atWarNation && World.Diplomacy is { } dgE && World.SelfPower is { } spE ? dgE.Engagement(spE, World.Coef) : 0;"
if M == "M0":   # 何もしない＝旗を読まない（いつも今の式）
    rep(SIM, "double warMult = World.Coef.MilitaryWarMultFromContact > 0 ?", "double warMult = false ?")
elif M == "M1":   # κ＝0
    rep(COEF, "public double WarArmsDemandPerContact = 42.0;", "public double WarArmsDemandPerContact = 0.0;")
elif M == "M2":   # 接触を内戦だけ 2 倍
    rep(SIM, ENG, ENG.replace("double warEngageNat = ", "double warEngageNat = (")[:-1] + ") * (World.InCivilWar ? 2.0 : 1.0);")   # 1 回目は括弧なし＝3 項の else の 0 に掛かり何もしない形だった（M2_first_noop.txt）＝括り直した
elif M == "M3":   # 接触を前の月の値で持つ（新しい状態＝Power の欄）
    rep(SIM, ENG, "double warEngageNat = 0; if (World.Coef.MilitaryWarMultFromContact > 0 && World.SelfPower is { } spH) { warEngageNat = spH.EngageHeld959; spH.EngageHeld959 = atWarNation && World.Diplomacy is { } dgE ? dgE.Engagement(spH, World.Coef) : 0; }")
    rep(DIP, "    public double Military;", "    public double EngageHeld959;\r\n    public double Military;")
elif M == "M4":   # κ を 10 倍
    rep(COEF, "public double WarArmsDemandPerContact = 42.0;", "public double WarArmsDemandPerContact = 420.0;")
elif M == "M5":   # Clamp01 を掛ける
    rep(DIP, "        double e = contact / p.Military;\r\n        return e > 0 ? e : 0;", "        double e = contact / p.Military;\r\n        return e > 0 ? (e < 1 ? e : 1) : 0;")
elif M == "M6":   # 内戦の 2.0 の枝を残す
    rep(SIM, "(atWar ? 1.0 + World.Coef.WarArmsDemandPerContact * warEngageNat : 1.0)", "(atWar ? (World.InCivilWar ? 2.0 : 1.0) * (1.0 + World.Coef.WarArmsDemandPerContact * warEngageNat) : 1.0)")
else:
    sys.exit("未知の突然変異 " + M)
print(f"{M} を当てた")
