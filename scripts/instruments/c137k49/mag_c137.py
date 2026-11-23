# 使い方: python -X utf8 -I mag_c137.py <腕の rawdir> <基準の rawdir> <種の起点> <種の数>
# (1319) c137k49 史実の桁の線（この線が一番動かす量）の読み手（読むだけ・エンジンの関数を呼ばない・prodcap_s の capacity を自分で足す）。
#  量 F＝工場の能力＝prodcap_s の capacity の和（isRgo=0・財が役務でない〔役務＝Catalog.cs と goods.json の Service の 11 財〕）・国（ownerNation）ごと。
#  量 I＝国が調達する基盤の 3 生産者（recipe＝水道局・ガス灯・電灯網）の capacity の和・国ごと。
#  年＝開幕（prodcap_s の最初の年＝1784）・1850・1913。国＝6 大国（イギリス・フランス・プロイセン〔ドイツ帝国があればその行〕・アメリカ・ロシア・オーストリア）＋清＋世界（全ての国の和）。
#  門 H（同じ盤面の旧の規則を物差しに）：腕÷基準（種の中央どうし）が 10 以上か 0.1 以下なら赤。基準の中央が 1（能力の単位）未満の行は記録（開幕の基盤は 0.0004〜0.18・1850 の米は 2.6e-12＝芽の値の比は桁で揺れる）。
import sys, os, statistics as st
SERV = {"労働力", "教育", "医療", "行政", "兵力", "高度労働力", "農業労働力", "上下水道", "電化インフラ", "家事奉仕", "高等教育"}
INFRA = {"水道局", "ガス灯", "電灯網"}
NAT = ["イギリス", "フランス", "プロイセン", "アメリカ", "ロシア", "オーストリア", "清", "世界"]
NAN = float("nan")
def fl(x):
    try: return float(x)
    except: return NAN
def read(raw, s0, n):
    out = {}; ns = 0; y0s = set()
    for s in range(s0, s0 + n):
        p = f"{raw}/prodcap_s{s}.tsv"
        if not os.path.exists(p): continue
        ns += 1; acc = {}; y0 = None
        with open(p, encoding="utf-8") as f:
            h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
            iy, io, ig, ir, irg, ic = ix["year"], ix["ownerNation"], ix["good"], ix["recipe"], ix["isRgo"], ix["capacity"]
            for l in f:
                c = l.rstrip("\n").split("\t")
                y = c[iy]
                if y0 is None: y0 = y
                if y not in (y0, "1850", "1913"): continue
                yk = "開幕" if y == y0 else y
                nm = c[io]; cap = fl(c[ic])
                if c[irg] == "0" and c[ig] not in SERV: q = "F"
                elif c[ir] in INFRA: q = "I"
                else: continue
                for k in (nm, "世界"):
                    acc[(yk, k, q)] = acc.get((yk, k, q), 0.0) + cap
        y0s.add(y0)
        for yk in ("開幕", "1850", "1913"):
            for q in ("F", "I"):
                if (yk, "ドイツ帝国", q) in acc: acc[(yk, "プロイセン", q)] = acc.pop((yk, "ドイツ帝国", q))
        for k in NAT:
            for yk in ("開幕", "1850", "1913"):
                for q in ("F", "I"):
                    out.setdefault((k, q, yk), []).append(acc.get((yk, k, q), 0.0))
    med = {key: st.median(v) for key, v in out.items()}
    return med, ns, y0s
def main():
    arm, base, s0, n = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4])
    A, na, ya = read(arm, s0, n); B, nb, yb = read(base, s0, n)
    print(f"# mag_c137 腕={arm} 基準={base} 種 {s0}..{s0+n-1} 読めた種 腕 {na} 基準 {nb} 開幕の年 腕 {sorted(ya)} 基準 {sorted(yb)}")
    if na == 0 or na != nb: print("判定不能（種の数）"); return
    red = 0
    print("国\t量\t年\t腕（種の中央）\t基準（種の中央）\t腕÷基準\t門 H")
    for k in NAT:
        for q in ("F", "I"):
            for yk in ("開幕", "1850", "1913"):
                a, b = A[(k, q, yk)], B[(k, q, yk)]
                r = a / b if b > 0 else NAN
                v = "記録（基準＜1）" if not (b >= 1.0) else ("赤" if not (0.1 < r < 10) else "緑")
                if v == "赤": red += 1
                print(f"{k}\t{q}\t{yk}\t{a:.6g}\t{b:.6g}\t{r:.4f}\t{v}")
    print(f"門 H 赤 {red}")
main()
