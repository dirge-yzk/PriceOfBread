# (973) c87mw 点火の変更を木へ当てる（旗の既定 1・κ の根拠の注・番人の双子・注）。使い方: python ign973.py <木>
#   バイトで読み書き（CR の数を保つ）。ff1bf106 の cherry-pick の後に当てる（旗の行は ff1bf106 が 1 にする）。
import sys, os
T = sys.argv[1]
def rep(rel, old, new):
    p = os.path.join(T, rel); b = open(p, "rb").read(); o = old.encode(); n = new.encode()
    if b.count(o) != 1: sys.exit(f"{rel} の一致 {b.count(o)} 件（1 件であること）")
    open(p, "wb").write(b.replace(o, n))
rep("PriceOfBread.SimCore/Coefficients.cs",
    "42＝m947 の生で今の戦時の軍需の総量 Σ兵×(warMult−1) を保つ値（紙・戦死÷Rate を接触の代理にした近似・AI の値・確証なし）",
    "42＝今の戦時の軍需の総量 Σ兵×(warMult−1) を保つ値（(973) m968 の生で取り直した紙＝戦死÷Rate を接触の代理にした近似で base・n1・n2 の幅 41.8〜43.9・measurements/m973_20261108/paper973_m968.txt・同じ盤面の計器の値は ident c87mwz の門の記録・AI の値・確証なし）")
rep("PriceOfBread.SimCore/EconomyStep.cs",
    "〔今は対外戦と同じ MilitaryWarMult×(1+WarMobilizationDepth×厭戦)〕",
    "〔今は対外戦と同じ 1＋WarArmsDemandPerContact×接触の割合（(973) 旗 MilitaryWarMultFromContact 1）〕")
rep("PriceOfBread.Tests/MilitaryTests.cs",
    "        (double ceil, double pay, double warMult) Twin(double warMultCoef)",
    "        (double ceil, double pay, double warMult) Twin(double kappa)")
rep("PriceOfBread.Tests/MilitaryTests.cs",
    "            c.MilitaryWarMult = warMultCoef;    // 月の決算",
    "            c.WarArmsDemandPerContact = kappa;    // (973) 旗 MilitaryWarMultFromContact 1＝戦時の倍率は 1＋κ×接触の割合＝双子は κ を替える。月の決算")
rep("PriceOfBread.Tests/MilitaryTests.cs",
    "        var hi = Twin(new Coefficients().MilitaryWarMult); var lo = Twin(1.0);\n        Assert.True(hi.warMult > lo.warMult && hi.warMult > 1, $\"前提が崩れた：戦時の倍率 {hi.warMult}／{lo.warMult}\");",
    "        var hi = Twin(new Coefficients().WarArmsDemandPerContact); var lo = Twin(0.0);\n        Assert.True(hi.warMult > lo.warMult && hi.warMult > 1 && Math.Abs(lo.warMult - 1.0) <= 1e-12, $\"前提が崩れた：戦時の倍率 {hi.warMult}／{lo.warMult}（κ＝0 の側は 1）\");")
print("当てた")
