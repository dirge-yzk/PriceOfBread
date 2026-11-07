# (932) c81ld：r898/addon898.py の写し（盤面の名 m873→m919 と import の名だけ替えた・式は同じ）
# (898) c76ds 読むだけの台本（paper932.py の続き・同じ読み方を import して使う）：
#  A1 G1〈①′ と ③′ の旧支配層の穀物法の選好の差〉を旗 0（今の式 cornPrefNow0）で数え、ヌル腕どうしの幅を並べる（限定以上の行・1783〜1846）
#  A2 G2 を旗 0 に当てる＝〈③ かつ限定以上〉で |cornPrefNow0|＜0.05 の割合（旗 0 で落ちるか＝★(879) の恒真の確かめ）
#  A3 G3 に替わる母集合の候補＝〈限定以上で穀物が国境を越えない ②′〉と〈限定以上の ①′〉の件数（反射の罠の誤りの量を読む行）
# 使い方：python -I -X utf8 addon898.py <dir> [<dir> ...]
import sys, os, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import paper932 as P

def run(name, d):
    rows = P.load(d)
    for r in rows:
        r.update(P.derive(r)); r["ft"] = P.ftype(r)
    has = [r for r in rows if r["ft"] is not None and r["gd0"] > 0]
    print(f"## {name}  行={len(rows)}")
    for a, b in [(1783, 1846), (1783, 1814), (1815, 1846), (1783, 1950)]:
        xs = [r for r in has if a <= r["year"] <= b and r["code"] >= 1]
        t1 = [r["now0"] for r in xs if r["ft"] == 1]; t3 = [r["now0"] for r in xs if r["ft"] == 3]
        n1 = [r["prefdn"] for r in xs if r["ft"] == 1]; n3 = [r["prefdn"] for r in xs if r["ft"] == 3]
        print(f"- A1 {a}〜{b}・限定以上：①′ {len(t1)} 行・③′ {len(t3)} 行／旗 0（今の式）の中央 ①′ {P.fmt(P.q(t1,.5))}・③′ {P.fmt(P.q(t3,.5))}・差 {P.fmt(P.q(t1,.5)-P.q(t3,.5),4)}"
              f"／C1′ 下げの上限（値段の型を流れで近似＝③′ も 0 にならない）の中央 ①′ {P.fmt(P.q(n1,.5),4)}・③′ {P.fmt(P.q(n3,.5),4)}")
    for lab, pred in [("③＝grainExportValue＞0", lambda r: r["gexp"] > 0), ("③′＝純輸出", lambda r: r["ft"] == 3)]:
        pop = [r for r in has if pred(r) and r["code"] >= 1]
        s = sum(1 for r in pop if abs(r["now0"]) < 0.05) / len(pop) if pop else float("nan")
        print(f"- A2 {lab}・限定以上 {len(pop)} 行：旗 0 で |選好|＜0.05 の割合 {P.fmt(s,4)}")
    for a, b in [(1783, 1846), (1847, 1950), (1783, 1950)]:
        c2 = sum(1 for r in has if a <= r["year"] <= b and r["code"] >= 1 and r["ft"] == 2)
        c1 = sum(1 for r in has if a <= r["year"] <= b and r["code"] >= 1 and r["ft"] == 1)
        print(f"- A3 {a}〜{b}：限定以上の ②′ {c2} 行・限定以上の ①′ {c1} 行")
    print()

if __name__ == "__main__":
    names = ["m919 基線", "m919 N1", "m919 N2"]
    for i, d in enumerate(sys.argv[1:]):
        run(names[i] if i < len(names) else d, d)
