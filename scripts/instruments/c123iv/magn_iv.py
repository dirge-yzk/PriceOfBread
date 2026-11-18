# 使い方: python -X utf8 -I magn_iv.py <rawdir> <seeds 1-10> [<基準の rawdir>]
# (1223) c123iv の史実の桁の線 H1・H2 と狙いの W1 の読み手（読むだけ）。
#  H1＝6 大国（イギリス・フランス・プロイセン〔ドイツ帝国があればその行〕・アメリカ・ロシア・オーストリア）の 1913 年の工場の産出
#      （factory_s の outputRefValuePerMonth・1913 年の四半期の平均・6 国の和）の種の中央。factory_s が無い生（刈られた腕）は H1 だけ飛ばす。
#  H2＝資本家の貯蓄÷名目 GDP（年）：class_s の classId 3 の savingsStock ÷ nation_s の nominalGdpPerYear（同じ年の四半期の平均）・
#      全ての国×種の中央（1850 年と 1900 年・GDP＞0 の行）。
#  W1＝資本家の消滅（class_s の資本家の sizeK が峰の 1/1000 未満に初めて落ちた国×種）の数と年の中央（shadow.py と同じ定義）。
#  基準の rawdir を渡すと 腕÷基準 を出し、H1・H2 は [0.1, 10] の外を「赤」、W1 は 腕の数 ≦ 0.85×基準 を「通る」と書く。
#  種の数が基準と違うか読めた種が 0 なら「判定不能」。
import sys, os, statistics as st
SIX = ["イギリス", "フランス", "プロイセン", "ドイツ帝国", "アメリカ", "ロシア", "オーストリア"]
NAN = float("nan")
def fl(x):
    try: return float(x)
    except: return NAN
def read(raw, sr):
    s0, s1 = int(sr.split('-')[0]), int(sr.split('-')[1])
    h1 = []; h2 = {1850: [], 1900: []}; ext = 0; tot = 0; years = []; ns = 0
    for s in range(s0, s1 + 1):
        if not (os.path.exists(f"{raw}/class_s{s}.tsv") and os.path.exists(f"{raw}/nation_s{s}.tsv")): continue
        ns += 1
        if os.path.exists(f"{raw}/factory_s{s}.tsv"):
            acc = {}
            with open(f"{raw}/factory_s{s}.tsv", encoding="utf-8") as f:
                h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
                for l in f:
                    c = l.rstrip("\n").split("\t")
                    if c[ix["year"]] != "1913" or c[ix["nation"]] not in SIX: continue
                    a = acc.setdefault(c[ix["nation"]], [0, 0.0]); a[0] += 1; a[1] += fl(c[ix["outputRefValuePerMonth"]])
            if "ドイツ帝国" in acc: acc.pop("プロイセン", None)
            h1.append(sum(v[1] / v[0] for v in acc.values() if v[0]))
        gdp = {}
        with open(f"{raw}/nation_s{s}.tsv", encoding="utf-8") as f:
            h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
            for l in f:
                c = l.rstrip("\n").split("\t"); y = int(c[ix["year"]])
                if y not in h2: continue
                a = gdp.setdefault((c[ix["nation"]], y), [0, 0.0]); a[0] += 1; a[1] += fl(c[ix["nominalGdpPerYear"]])
        cap = {}
        with open(f"{raw}/class_s{s}.tsv", encoding="utf-8") as f:
            h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
            for l in f:
                c = l.rstrip("\n").split("\t")
                if c[0] in ("open", "warm") or int(c[3]) != 3: continue
                n, y = c[1], int(c[2]); z = fl(c[ix["sizeK"]]); S = fl(c[ix["savingsStock"]])
                cap.setdefault(n, []).append((y, z))
                if y in h2:
                    g = gdp.get((n, y))
                    if g and g[0] and g[1] / g[0] > 0 and S == S: h2[y].append(S / (g[1] / g[0]))
        for n, ser in cap.items():
            ser.sort(); tot += 1
            pk = max(ser, key=lambda t: t[1] if t[1] == t[1] else -1)
            for (y, z) in ser:
                if y > pk[0] and z == z and z < pk[1] / 1000: ext += 1; years.append(y); break
    return (st.median(h1) if h1 else NAN, {y: (st.median(v) if v else NAN) for y, v in h2.items()}, ext, tot, (st.median(years) if years else NAN), ns, len(h1))
a = read(sys.argv[1], sys.argv[2])
print(f"腕 {sys.argv[1]} 種 {a[5]}（H1 の種 {a[6]}）：H1 6 大国 1913 工場の産出の中央 {a[0]:.6g}・H2 資本家の貯蓄÷GDP 1850 {a[1][1850]:.4g} 1900 {a[1][1900]:.4g}・W1 消滅 {a[2]}／{a[3]} 年の中央 {a[4]}")
if len(sys.argv) > 3:
    b = read(sys.argv[3], sys.argv[2])
    print(f"基準 {sys.argv[3]} 種 {b[5]}（H1 の種 {b[6]}）：H1 {b[0]:.6g}・H2 1850 {b[1][1850]:.4g} 1900 {b[1][1900]:.4g}・W1 消滅 {b[2]}／{b[3]} 年の中央 {b[4]}")
    bad = a[5] == 0 or a[5] != b[5]
    def g(x, y, nseed, nbase):
        if bad or nseed == 0 or nseed != nbase: return NAN, "判定不能"
        r = x / y if y else NAN
        return r, ("通る" if 0.1 <= r <= 10 else "赤")
    r1 = g(a[0], b[0], a[6], b[6]); r2 = g(a[1][1850], b[1][1850], a[5], b[5]); r3 = g(a[1][1900], b[1][1900], a[5], b[5])
    print(f"H1 腕÷基準 {r1[0]:.4g} → {r1[1]}")
    print(f"H2 1850 腕÷基準 {r2[0]:.4g} → {r2[1]}・1900 腕÷基準 {r3[0]:.4g} → {r3[1]}")
    w = "判定不能" if bad else ("通る" if a[2] <= 0.85 * b[2] else "外れ")
    print(f"W1 腕 {a[2]} ≦ 0.85×基準 {0.85*b[2]:.1f} → {w}（年の中央 腕 {a[4]}・基準 {b[4]}＝記録）")
