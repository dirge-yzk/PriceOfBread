# (1066) 秒＝回数×単価（.cost の b5_20261112_m1053 の単価）。年の点の値を台形で時間平均（1784〜1950・助走 20 四半期は 1784 の値）。読むだけ。
# 2026-11-12 改め（反証の指摘）：下界は事前登録どおり〈約 0〉（中身の無い物の 1 回の重さが飛ばしの判定 1 回分まで小さい場合）。
#   (186) の重さの比 0.216 は生産の行だけの〈目安〉（眠った生産者の重さ＝POP の段の根拠にはしない）。
#   POP の回数は下界だけ（world_zero.py）＝秒は〈下界の回数×平均の単価〉を〈目安〉として出す（上界ではない・上界は回数の上界が無く出せない）。
def tavg(pts):
    ys = sorted(pts); tot = 0; span = 0
    for a, b in zip(ys, ys[1:]):
        tot += (pts[a] + pts[b]) / 2 * (b - a); span += b - a
    tot += pts[ys[0]] * 5; span += 5
    return tot / span
def read(path, cols):
    out = {}
    with open(path, encoding="utf-8") as f:
        lines = [l for l in f if not l.startswith("#")]
    h = lines[0].rstrip("\n").split("\t")
    for line in lines[1:]:
        c = line.rstrip("\n").split("\t")
        out[int(c[0])] = {k: float(c[h.index(k)]) for k in cols}
    return out
pg = read("out_pc_gate.txt", ["run", "run_empty_gateopen", "run_empty_svc_site"])
wz = read("out_world_zero.txt", ["slots", "lb"])
MONTHS = (668 + 20) * 3
U = {"prod": 0.48553, "cons": 0.02801, "mon": 1.73157, "qs": 3.36578, "qe": 1.05863}
N = {"prod": 94748964, "cons": 362726070, "mon": 8751616, "qs": 2917205, "qe": 2917205}
SEC = {k: N[k] * U[k] / 1e6 for k in N}
ANCHOR = 0.041 / 0.19
emp = tavg({y: v["run_empty_gateopen"] + v["run_empty_svc_site"] for y, v in pg.items()})
run = tavg({y: v["run"] for y, v in pg.items()})
led = N["prod"] / MONTHS
print(f"生産：回された中身の無い生産者（月あたり・時間平均・近似）{emp:.0f}・回された生産者（近似）{run:.0f}・台帳の月平均 {led:.0f}・割合 {emp/run:.4f}")
print(f"  近似の残り（台帳−近似）{led-run:.0f}/月＝中身の無い物の {abs(led-run)/emp:.2f} 倍にあたる（近似の確かさの幅）")
cnt = emp * MONTHS
print(f"  回数/試合 {cnt/1e6:.2f} 百万・上界 {cnt*U['prod']/1e6:.2f} 秒（平均の単価）・下界 約 0 秒（事前登録）・目安 {cnt*U['prod']*ANCHOR/1e6:.2f} 秒（(186) の重さの比 {ANCHOR:.3f}・生産の行だけ）")
print(f"  近似の残りを足した上界：{(emp + abs(led - run)) * MONTHS * U['prod'] / 1e6:.2f} 秒")
zs = tavg({y: v["lb"] / v["slots"] for y, v in wz.items()})
print(f"POP：人口 0 の割合の下界の時間平均 {zs:.3f}（兵士を除く 12 階級・全 61 国）")
for k, nm in [("cons", "消費：POP×財（月）"), ("mon", "月次：POP×月（決算の残り）"), ("qs", "四半期首：POP×国"), ("qe", "四半期末：POP×国")]:
    print(f"  {nm}\t段の秒 {SEC[k]:.2f}\t人口 0 の回数/試合の下界 {N[k]*zs/1e6:.2f} 百万\t目安（下界の回数×平均の単価・上界ではない）{SEC[k]*zs:.2f} 秒\t下界 約 0 秒")
