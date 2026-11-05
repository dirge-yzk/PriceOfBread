# (855) c70ss 点火の編集（旗 SubsistenceStage2 の既定を 1 にし、DormantIdentityTests の旗の表から外す）＝ignite849.py と同じ中身（注の字句だけ今の main の行に合わせた）。
#   呼び方：python ignite855.py <木の根>（バイトで読み書き＝改行と BOM を保つ）
import sys, os
root = sys.argv[1]
def edit(rel, a, b):
    p = os.path.join(root, rel)
    s = open(p, "rb").read().decode("utf-8")
    assert s.count(a) == 1, (rel, a)
    open(p, "wb").write(s.replace(a, b).encode("utf-8"))
edit("PriceOfBread.SimCore/Coefficients.cs",
     "    public double SubsistenceStage2 = 0;          // ★(849) 自給の段 2（S1-a・AI の案・確証なし）。0＝今の形",
     "    public double SubsistenceStage2 = 1;          // ★(849) 自給の段 2（S1-a・AI の案・確証なし・(855) 2026-11-05 点火）。0＝旧の形")
edit("PriceOfBread.Tests/DormantIdentityTests.cs",
     "        \"SubsistenceStage2\",           // ★(849) c69ss 自給の段 2（S1-a）の旗（0＝食べる割合は欄のデータのまま・1 ビットも変わらない）\n",
     "        // ★SubsistenceStage2（(849) c69ss 自給の段 2）は (855) c70ss が 2026-11-05 に 1 で**点火**＝この表から外した。\n")
print("IGNITE ok")
