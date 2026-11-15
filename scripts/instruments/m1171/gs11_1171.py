# (1171) 合流：種 11〜20 の sv の門 G1 の閾値を、同じ定義〈旗 0 の 10 種の和の 1/10 の切り捨て〉で旗 0 の側（S11z）の paper_sv.py の出力から取る（★(1157) の型）。
#   G3 の閾値 0.9・床（分母≧100）と G2 は c115sv/gates_sv.py のまま（その出力 gates_sv.txt を読む）。G1 の数え方は paper_sv.py のまま。
# 引数：S11 の paper_sv.py の出力  S11z の paper_sv.py の出力
import sys
sys.stdout.reconfigure(newline="\n")
KEY = "G1 の内訳：届き＞0×定員＞0（空きが立ちうる）"


def g1(p):
    for line in open(p, encoding="utf-8"):
        t = line.rstrip("\n").split("\t")
        if t[0] == KEY:
            return int(t[1])
    raise SystemExit(f"G1 が無い {p}")


a, z = g1(sys.argv[1]), g1(sys.argv[2])
thr = z // 10
print(f"# (1171) 種 11〜20 の G1：S11 {a}・S11z（旗 0）{z}・閾値 ⌊{z}/10⌋＝{thr}")
print(f"G1 働いた（種 11〜20）\t{a}\t≦{thr}\t{'緑' if a <= thr else '赤'}")
