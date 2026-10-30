# (687) 線 urb2 後半：種 11〜20 の追試（M2・M3'）。基線＝a11（旗 0）・幅は種 1〜10 のヌル 3 本から借りる。閾値＝a11 の値＋max(2×借りた幅, F)。読むだけ。
import sys, importlib.util, os
spec = importlib.util.spec_from_file_location("g3", os.path.join(os.path.dirname(__file__), "gates3.py"))
sys.argv = ["x"]  # gates3 の主処理を空打ちにする（引数なし）
src = open(os.path.join(os.path.dirname(__file__), "gates3.py"), encoding="utf-8").read().split("nulls = [x.rstrip")[0]
ns = {}; exec(compile(src, "g3", "exec"), ns)
m2, m3, widths = ns["m2"], ns["m3"], ns["widths"]
K, A11, B11 = "D:/PoB_work/keep_685", "D:/PoB_work/keep_685/a11", "D:/PoB_work/urb2_687/out_B_10_s11/ins"
for name, fn in (("M2", m2), ("M3'", m3)):
    nul = [fn(d, range(1, 11)) for d in (K + "/m685_ins", K + "/n1", K + "/n2")]
    w = widths(nul); F = max(wd for _, wd in w.values())
    a = fn(A11, range(11, 21)); b = fn(B11, range(11, 21))
    out = [k for k in a if k in b and k in w and b[k] > a[k] + max(2 * w[k][1], F)]
    print(f"{name} 追試（種 11〜20）：外 {len(out)} / {len([k for k in a if k in b and k in w])}", out)
