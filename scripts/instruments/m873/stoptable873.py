# (873) 止める線の件数の表（stop_table_866.tsv と同じ列）を stoplines866.sh の出力から作る（読むだけ）。
#   python -X utf8 -I stoptable873.py <出力 tsv> <名>=<stop の出力> ...
import sys, re
out = sys.argv[1]
cols = ["arm", "ukDefaultSeeds", "gdp100_other(797)", "gdp100_833(建国/痩せ/それ以外)", "1e10", "gdpZero4q", "plvlMax", "plvl1000open", "hol1/10", "hol1/4", "hol1/10capital"]
def one(name, p):
    t = open(p, encoding="utf-8", errors="replace").read()
    g = lambda pat, d="?": (re.search(pat, t).group(1) if re.search(pat, t) else d)
    uk = g(r"不履行のあった種 (\d+/\d+)")
    s797 = t.split("== gdpjump（旧の台本 gdp_jump797）")[1].split("==")[0]
    o797 = re.search(r"それ以外 (\d+)", s797).group(1)
    s833 = t.split("== gdpjump833（3 区分・行つき）")[1].split("== gdp_zero10")[0]
    m = re.search(r"建国の直後〔≦4 四半期〕(\d+)・分母の痩せ〔[^〕]*〕(\d+)・それ以外 (\d+)", s833)
    c833 = "%s/%s/%s" % m.groups()
    e10 = max(int(x) for x in re.findall(r"1e10 倍以上 (\d+)", s797 + s833))
    z = g(r"4 四半期以上続く区間 (\d+)")
    pmax = g(r"価格水準の最大 \(([0-9.eE+-]+)")
    p1000 = g(r"開幕の1000倍以上（国×種で最初の四半期） (\d+)")
    h10 = g(r"## ピークの1/10 未満: (\d+) 件")
    h4 = g(r"## ピークの1/4 未満: (\d+) 件")
    hc = g(r"## 1/10 の首都: (\d+)")
    return [name, uk, o797, c833, str(e10), z, pmax, p1000, h10, h4, hc]
with open(out, "w", encoding="utf-8", newline="\n") as w:
    w.write("\t".join(cols) + "\n")
    for a in sys.argv[2:]:
        n, p = a.split("=", 1); w.write("\t".join(one(n, p)) + "\n")
print(open(out, encoding="utf-8").read())
