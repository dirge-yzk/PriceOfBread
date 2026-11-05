# (873) アブレーションの腕 ≡ 線の腕（事前登録 prereg_873.txt [4]・読むだけ・生を書き換えない）。
#   python -X utf8 -I cmpabl873.py <アブレーションの腕の出力フォルダ（ins と b10_*.txt を持つ）> <線の腕の keep（ins と b10_*.txt を持つ）> <出力>
#   門と止める線の 8 表（種 1〜10）をバイトで比べる（成績表は diff で別に比べる）。
import sys, os, filecmp
arm, line, out = sys.argv[1:4]
T = ("region_s", "migration_s", "nation_s", "pxmonth_s", "cessp_s", "secession_s", "subst_s", "rule_s")
rows = []; bad = 0; n = 0
for t in T:
    for s in range(1, 11):
        f = "%s%d.tsv" % (t, s); a = os.path.join(arm, "ins", f); b = os.path.join(line, "ins", f)
        if not os.path.exists(a) and not os.path.exists(b): continue
        n += 1
        if not (os.path.exists(a) and os.path.exists(b)): k = "片方だけ"
        elif os.path.getsize(a) == os.path.getsize(b) and filecmp.cmp(a, b, shallow=False): k = "同じ"
        else: k = "違う"
        if k != "同じ": bad += 1
        rows.append((f, k))
with open(out, "w", encoding="utf-8", newline="\n") as w:
    w.write("# (873) %s 対 %s（8 表・種 1〜10）\n" % (arm, line))
    for r in rows: w.write("%s\t%s\n" % r)
    w.write("# 表 %d 本・同じでない %d 本\n" % (n, bad))
    w.write("# 判定：" + ("8 表は全てバイトで同じ" if bad == 0 and n > 0 else "違う表あり＝帰属を読むまで採らない") + "\n")
print(open(out, encoding="utf-8").read().splitlines()[-2:])
