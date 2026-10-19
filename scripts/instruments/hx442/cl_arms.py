#!/usr/bin/env python
"""(442) 合成後のアブレーション cl の腕（(ii) だけ・全部切る・全部入れる）の港の指標の表（読むだけ・シミュを回さない）。
入力：各腕の計器の生の port_s<seed>.tsv（rd434/port434.py を呼ぶ＝(434) の判定と同じ定義）。清・ロシア・イギリスの和・四半期あたり（gate は 1 試合あたり）。
雑音の目盛り＝〈全部入れる〉の腕とそのヌル腕（N1・N2）の差の最大（ヌル腕は全部入れる世界の SupplySlope を ±1e-8 動かすだけ）。
  ⚠(434) の judge434.py は雑音を |ヌル腕−A| で測る（A＝全部切る・ヌル腕は m428 の全部切る世界）。m436 のヌル腕は全部入れる世界なので、全部切る腕を A にすると雑音が実効果を含んでしまう＝この表は雑音を〈全部入れる〉側で測る。
使い方：PYTHONUTF8=1 python cl_arms.py <名前=フォルダ>... --on <全部入れる腕の名前> --nulls N1,N2 [--ref <名前>] [--seeds 1,...,10] [--seedsR 1,...,5]
"""
import argparse, json, os, subprocess, sys

HERE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "rd434")
ERA = ["1783-1850", "1851-1913", "1914-1949"]
ap = argparse.ArgumentParser()
ap.add_argument("arms", nargs="+")
ap.add_argument("--on", required=True)
ap.add_argument("--nulls", default="N1,N2")
ap.add_argument("--ref", default="R")
ap.add_argument("--seeds", default="1,2,3,4,5,6,7,8,9,10")
ap.add_argument("--seedsR", default="1,2,3,4,5")
g = ap.parse_args()


def port(folder, seeds):
    r = subprocess.run([sys.executable, "-X", "utf8", os.path.join(HERE, "port434.py"), folder, "--seeds", seeds, "--json"],
                       capture_output=True, text=True, encoding="utf-8", env={**os.environ, "PYTHONUTF8": "1"})
    if r.returncode != 0:
        raise SystemExit(r.stderr)
    return json.loads(r.stdout)


folders = dict(a.split("=", 1) for a in g.arms)
P = {n: port(f, g.seeds) for n, f in folders.items()}
PR = {n: port(f, g.seedsR) for n, f in folders.items()}   # 参照腕 R と同じ種だけで読んだ物
nulls = [n for n in g.nulls.split(",") if n in P]


def cell(d, e, k):
    return d["cell"][f"SUM|{e}"][k]


print(f"腕={list(folders)}　種={g.seeds}（R と比べる項は {g.seedsR}）　〈全部入れる〉={g.on}　ヌル腕={nulls}\n")
for key, label, scale in [("pot", "港の州の潜在量（四半期あたりの和）", 1), ("cap", "港の容量（同）", 1), ("gate", "建設の門を通った州×四半期（1 試合あたり）", 1), ("thr", "律速の割合", 1), ("flipRate", "律速の状態の反転率", 1)]:
    print(f"== {label}（清・ロシア・イギリスの和）==")
    for e in range(3):
        on = cell(P[g.on], e, key)
        noise = max(abs(cell(P[n], e, key) - on) for n in nulls) if nulls else float("nan")
        cols = "　".join(f"{n}={cell(P[n], e, key):.4g}" for n in folders if n != g.ref)
        rr = f"　{g.ref}（5 種）={cell(PR[g.ref], e, key):.4g}" if g.ref in PR else ""
        print(f"  {ERA[e]}: {cols}{rr} | 雑音（|ヌル腕−{g.on}| の最大）={noise:.3g}")
print("== 潜在量の四半期間の変動（cv_all）==")
print("  " + "　".join(f"{n}={P[n]['cv_all']:.4f}" for n in folders if n != g.ref))
print("\n== 〈全部入れる〉に対する各腕の比（潜在量・容量・門。1.0＝同じ）==")
for n in folders:
    if n in (g.on, g.ref) or n in nulls:
        continue
    for key in ("pot", "cap", "gate"):
        print(f"  {n} {key}: " + "　".join(f"{ERA[e]} {cell(P[n], e, key) / cell(P[g.on], e, key):.2f}" for e in range(3)))
if g.ref in PR:
    print(f"\n== 参照腕 {g.ref}（番の廃止＝潜在量の真値に近い世界・m428 の木・5 種）との潜在量の比（同じ 5 種で読み直し）==")
    for n in folders:
        if n == g.ref:
            continue
        print(f"  {n}: " + "　".join(f"{ERA[e]} {cell(PR[n], e, 'pot') / cell(PR[g.ref], e, 'pot'):.2f}" for e in range(3)))
