# (875) c73gt (a) の両側の門の小さな盤面と突然変異（紙の台本・事前登録 [4] (a)・[6] M1 M4）。実の生は読まない。
#   python -X utf8 -I fix_down875.py <作業フォルダ> <出力の表>
import sys, os, shutil, subprocess, importlib.util
sys.dont_write_bytecode = True
sd = os.path.dirname(os.path.abspath(__file__))
def load(path, name):
    sp = importlib.util.spec_from_file_location(name, path); m = importlib.util.module_from_spec(sp); sp.loader.exec_module(m); return m
gd = load(os.path.join(sd, "gdp_down875.py"), "gd")

H = ['quarter', 'year', 'nationIndex', 'nation', 'totalPopulationK', 'populationK', 'nominalGdpPerYear', 'nationUid']
def series(rows, uid, name, start, gdps, pops):
    for k, (g, p) in enumerate(zip(gdps, pops)):
        q = start + k; rows.append([q, 1783 + q // 4, 0, name, p, p, g, uid])

def write(path, rows):
    rows.sort(key=lambda r: (r[0], r[7]))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, 'wb') as f:
        f.write(('\t'.join(H) + '\n').encode())
        for r in rows: f.write(('\t'.join(str(v) for v in r) + '\n').encode())

def build(root):
    shutil.rmtree(root, ignore_errors=True)
    S = [1000.0] * 14
    rows = []
    # A アワド型：1/129 の落ち（人口 27957→9782）→ 次の四半期に 92.5 倍で戻る（段の比 <100＝上向きの台本は数えない）
    series(rows, 'A', 'aアワド型', 0, [100.0] * 6 + [100.0 / 129, 100.0 / 129 * 92.5, 72, 72], [27957.0] * 6 + [9782.0, 9782.0, 27000.0, 27000.0])
    # B 1/129 の落ち→段の比 129 で戻る（上向きの台本が数えた物＝印あり）
    series(rows, 'B', 'b上向き数え済み', 0, [100.0] * 6 + [100.0 / 129, 100.0, 100.0], [1000.0] * 9)
    # C 滑らか（落ちなし）
    series(rows, 'C', 'c滑らか', 0, [100.0 - k for k in range(9)], [1000.0] * 9)
    # D 1/20 の落ち（既定では拾わず・--down 10 で拾う）
    series(rows, 'D', 'd二十分の一', 0, [100.0] * 6 + [5.0, 100.0, 100.0], [1000.0] * 9)
    # E 0 への落ち（落ちと数えず・ゼロへの落ち）
    series(rows, 'E', 'eゼロ', 0, [100.0] * 6 + [0.0, 0.0, 0.0], [1000.0] * 9)
    # F 建国の直後（最初の行から 3 四半期）
    series(rows, 'F', 'f建国3', 10, [100.0] * 3 + [0.5, 100.0], [1000.0] * 5)
    # G 分母の痩せ（人口の比 1/13）
    series(rows, 'G', 'g痩せ', 0, [100.0] * 6 + [0.5, 100.0, 100.0], [1000.0] * 6 + [76.0, 1000.0, 1000.0])
    # H 落ちて戻らない
    series(rows, 'H', 'h戻らない', 0, [100.0] * 6 + [0.5] * 12, [1000.0] * 18)
    # I 窓の外（9 四半期後）に戻る
    series(rows, 'I', 'i窓の外', 0, [100.0] * 6 + [0.5] * 9 + [100.0], [1000.0] * 16)
    # J 1/100 ちょうど（拾う）と 1/99.9（拾わない）
    series(rows, 'J', 'j百分の一', 0, [100.0] * 6 + [1.0, 100.0], [1000.0] * 8)
    series(rows, 'K', 'k百分の一の手前', 0, [100.0] * 6 + [1.001, 100.0], [1000.0] * 8)
    write(os.path.join(root, "with", "nation_s1.tsv"), rows)
    # 落ちを入れない盤面（C だけ）
    rows2 = []; series(rows2, 'C', 'c滑らか', 0, [100.0 - k for k in range(9)], [1000.0] * 9)
    series(rows2, 'D', 'd滑らか', 0, [100.0] * 9, [1000.0] * 9)
    write(os.path.join(root, "without", "nation_s1.tsv"), rows2)
    os.makedirs(os.path.join(root, "empty"), exist_ok=True)

def table(path, down=100.0):
    fl, zero, nf = gd.scan_seed(path, down)
    by = {}
    for cls, row in fl: by[row[1]] = (cls, row)
    return by, zero

def main():
    work, out = sys.argv[1], sys.argv[2]
    build(work)
    p_with = os.path.join(work, "with", "nation_s1.tsv"); p_wo = os.path.join(work, "without", "nation_s1.tsv")
    L = ["(875) 下向きの 100 倍の固定の盤面（合成）の表・gdp_down875.py", "確かめ\t予言\t実際\t結果"]; allok = True
    def chk(name, want, got):
        nonlocal allok
        ok = want == got; allok &= ok
        L.append("%s\t%s\t%s\t%s" % (name, want, got, "予言どおり" if ok else "予言の外れ"))
    by, zero = table(p_with)
    chk("拾う国（既定 100）", "aアワド型,b上向き数え済み,f建国3,g痩せ,h戻らない,i窓の外,j百分の一", ",".join(sorted(by, key=lambda n: n)))
    chk("区分 A アワド型（人口 0.35＝それ以外）", 3, by["aアワド型"][0])
    chk("区分 F 建国の直後", 1, by["f建国3"][0]); chk("区分 G 分母の痩せ", 2, by["g痩せ"][0])
    chk("戻り A（次の四半期）", "あり:7:なし", "%s:%s:%s" % ("あり" if by["aアワド型"][1][8] != "-" else "-", by["aアワド型"][1][8], by["aアワド型"][1][10]))
    chk("上向きの印 B", "あり", by["b上向き数え済み"][1][10])
    chk("戻らない H・窓の外 I", "-,-", "%s,%s" % (by["h戻らない"][1][8], by["i窓の外"][1][8]))
    chk("ゼロへの落ち（落ちと数えない・E の 1 回だけ）", 1, zero)
    chk("1/100 の手前は拾わない", False, "k百分の一の手前" in by)
    byw, zw = table(p_wo)
    chk("入れない盤面は拾わない", 0, len(byw))
    by10, _ = table(p_with, 10.0)
    chk("M1 DOWN=10 で D（1/20）が増える", True, "d二十分の一" in by10 and "d二十分の一" not in by)
    old = gd.RET; gd.RET = 0
    by0, _ = table(p_with); gd.RET = old
    chk("M4 戻りの窓 0 で A の戻りが消える", "-", by0["aアワド型"][1][8])
    r = subprocess.run([sys.executable, "-X", "utf8", "-I", os.path.join(sd, "gdp_down875.py"), "x=" + os.path.join(work, "empty")], capture_output=True, text=True, encoding="utf-8")
    chk("空の盤面は終了コード 2（★(808)）", 2, r.returncode)
    L.append("判定：" + ("予言どおり" if allok else "予言と違う所あり"))
    open(out, "w", encoding="utf-8", newline="\n").write("\n".join(L) + "\n"); print("\n".join(L))
    return 0 if allok else 1

if __name__ == "__main__":
    sys.exit(main())
