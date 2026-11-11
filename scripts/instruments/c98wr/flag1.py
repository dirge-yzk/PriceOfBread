# (1038) 旗 3 本の既定を 1 にする（木の根を引数に）
import sys, os
p = os.path.join(sys.argv[1], "PriceOfBread.SimCore", "Coefficients.cs")
b = open(p, "rb").read().decode("utf-8")
for k in ["WarSurplusFromFill", "WarTensionDiscountOpen", "WarSubmitUnified"]:
    o = f"    public double {k} = 0;"; assert b.count(o) == 1, k
    b = b.replace(o, f"    public double {k} = 1;")
open(p, "wb").write(b.encode("utf-8")); print("flag1 ok")
