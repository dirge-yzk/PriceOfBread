# (875) c73gt G5b の分布の行（記録だけ・事前登録 measurements/m875_20261106_prereg_gt.txt [3] (c)）。
#   門 G5b（gates_ss867.py）は符号つきの中央だけを見る＝対称の漏れと少数の行の漏れを拾わない（(867) の反証役 ③）。その分布を腕とヌル対ヌルで並べる。
#   行の選び方・dF・dB の式は gates_ss867.py の関数（load_arm・load_light・med）を importlib で読み込んで呼ぶ（式を写し書きしない）。
#   dB＝d−dF（d＝腕の s−基線の s・dF＝s−H×d0）。w＝比べる側の s の最大−最小。P5 の行＝門の G5 と同じ（売れ行きの割合が開幕と 1% 以内）。
#   使い方（c73ts と合流が記録に使える形）：
#     腕対    ：python -X utf8 -I g5bdist875.py arm   <名乗り> <腕 ins> <基線 ins> <ヌル1 ins> <ヌル2 ins> <出力> [種=1-10] [--check]
#               w は基線・ヌル 1・ヌル 2 の 3 本の幅（実の門と同じ）。--check は同じ入力で gates_ss867.run_gates を呼び、G5b の行の値と一致することを確かめる。
#     ヌル対ヌル：python -X utf8 -I g5bdist875.py null  <名乗り> <基線 ins> <ヌル1 ins> <ヌル2 ins> <出力> [種=1-10]
#               3 通り（腕に見立てる X・基準 Y・その他 Z）＝(N1,基線,N2)・(N2,基線,N1)・(基線,N1,N2)。w は Y と Z の 2 本の幅＝腕対より狭い（名乗る）。
#     種：`1-10`・`1`・`1,3,5`。種ごとの行も出す（--by-seed）。
#   分布の最近順位：昇順の n 個で添字 ceil(q×n)−1。出力の列：P5 行・G5b 行（dB が有限）・|dB| の中央・p90・p99・最大・w の中央（P5 の行）・|dB|>w の行の割合・符号つきの中央。
import sys, os, math, importlib.util
sys.dont_write_bytecode = True
sd = os.path.dirname(os.path.abspath(__file__))
G = os.path.join(sd, '..', 'c72gt', 'gates_ss867.py')
def load(path, name):
    sp = importlib.util.spec_from_file_location(name, path); m = importlib.util.module_from_spec(sp); sp.loader.exec_module(m); return m
gs = load(G, 'gs')

def seeds_of(spec):
    out = []
    for part in spec.split(','):
        if '-' in part: a, b = part.split('-'); out += list(range(int(a), int(b) + 1))
        else: out.append(int(part))
    return out

def rank(xs, q):
    n = len(xs)
    return float('nan') if n == 0 else xs[max(0, math.ceil(q * n) - 1)]

def collect(arm, ref, others, seeds, by_seed=None):
    """P5 の行ごとの (dB, w) の一覧。by_seed が辞書なら種ごとの一覧も入れる。"""
    out = []
    for seed in seeds:
        A, has2, _ = gs.load_arm(arm, seed, False)
        B = [gs.load_light(x, seed) for x in [ref] + list(others)]
        open0 = {reg: r for (q, reg), r in A.items() if q == 0}
        rows = []
        for (q, reg), r in A.items():
            key = (q, reg)
            s0s = [x[key][0] for x in B if key in x]
            if len(s0s) < len(B) or any(v != v for v in s0s) or r["s"] != r["s"]: continue
            if q == 0: continue
            w = max(s0s) - min(s0s); d = r["s"] - s0s[0]
            o = open0.get(reg)
            if o is None or r["d1"] != r["d1"] or o["d1"] != o["d1"]: continue
            if r["cap"] > 0 and o["cap"] > 0 and abs(r["sold"] / r["cap"] - o["sold"] / o["cap"]) <= 0.01:
                hH = r["hf"]
                dB = float("nan")
                if has2 and hH == hH: dB = d - (r["s"] - hH * r["s2D0Inst"])
                rows.append((dB, w))
        out += rows
        if by_seed is not None: by_seed[seed] = rows
        del A, B
    return out

def summarize(name, rows):
    fin = [(b, w) for b, w in rows if b == b]
    ab = sorted(abs(b) for b, _ in fin)
    wm = gs.med([w for _, w in rows])
    over = sum(1 for b, w in fin if abs(b) > w)
    return ("G5b 分布\t%s\tP5 %d 行\tdB 有限 %d 行\t|dB| 中央 %.5f\tp90 %.5f\tp99 %.5f\t最大 %.5f\tw 中央 %.4f\t|dB|>w の割合 %.5f\t符号つきの中央 %.5f"
            % (name, len(rows), len(fin), gs.med(ab), rank(ab, 0.90), rank(ab, 0.99), ab[-1] if ab else float("nan"), wm,
               over / len(fin) if fin else float("nan"), gs.med([b for b, _ in fin])))

def main():
    a = sys.argv[1:]
    by = "--by-seed" in a; chk = "--check" in a
    a = [x for x in a if not x.startswith("--")]
    mode = a[0]; L = []
    if mode == "arm":
        name, arm, b, n1, n2, out = a[1:7]; seeds = seeds_of(a[7] if len(a) > 7 else "1-10")
        bs = {} if by else None
        rows = collect(arm, b, [n1, n2], seeds, bs)
        L.append(summarize(name + "（腕対・w は 3 本）", rows))
        if by:
            for s in seeds: L.append(summarize("%s 種 %d" % (name, s), bs[s]))
        if chk:
            ok, C, OUT = gs.run_gates(arm, b, n1, n2, len(seeds) if seeds == list(range(1, len(seeds) + 1)) else 10, 1.0, {}, False)
            ln = [x for x in OUT if x.startswith("G5b")][0]
            fin = [(bb, w) for bb, w in rows if bb == bb]
            g5b = "|dB の中央| %.5f ≦ w の中央 %.4f" % (abs(gs.med([bb for bb, _ in fin])), gs.med([w for _, w in rows]))
            L.append("整合の確かめ（run_gates の G5b の行と値が一致）：%s\t門の行：%s" % ("一致" if g5b in ln else "不一致", ln[:150]))
    elif mode == "null":
        name, b, n1, n2, out = a[1:6]; seeds = seeds_of(a[6] if len(a) > 6 else "1-10")
        for lab, arm, ref, oth in (("N1", n1, b, n2), ("N2", n2, b, n1), ("基線", b, n1, n2)):
            bs = {} if by else None
            rows = collect(arm, ref, [oth], seeds, bs)
            L.append(summarize("%s（ヌル対ヌル・腕に見立てた %s・w は 2 本）" % (name, lab), rows))
            if by:
                for s in seeds: L.append(summarize("%s %s 種 %d" % (name, lab, s), bs[s]))
    else:
        raise SystemExit("arm か null")
    open(out, "w", encoding="utf-8", newline="\n").write("\n".join(L) + "\n"); print("\n".join(L))
    return 0

if __name__ == "__main__":
    sys.exit(main())
