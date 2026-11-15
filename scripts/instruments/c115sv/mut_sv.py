# (1166) c115sv 突然変異（使い捨ての写しの木だけに当てる）。形は環境変数 SV_MUT（M1〜M5）。引数＝木の根。
#   M1 空きの条件を大きさ 0 の兵でだけ外す／M2 単価を 0／M3 国の兵の受け取りの比を掛けない／M4 大きさ 0 の枝を全ての兵に当てる／M5 兵の空きの上限を外す。
import os, sys

ROOT = sys.argv[1]
P = os.path.join(ROOT, "PriceOfBread.SimCore", "Meritocracy.cs")
MUT = os.environ.get("SV_MUT", "")
EDITS = {
    "M1": [("if (t.IsSoldier && !(soldierVac > Eps)) continue;", "if (t.IsSoldier && t.Size > Eps && !(soldierVac > Eps)) continue;", 2)],
    "M2": [("double unit = Military.RecruitWage(r, c, last: true, keep: w.ConscriptionLaw >= ConscriptionLaw.Selective) * c.SoldierPayMult;", "double unit = 0;", 1)],
    "M3": [("double perWorker = unit * (ReferenceEquals(_seatWorld, w) ? _seatPayRate : SeatPayRate(w, c));", "double perWorker = unit;", 1)],
    "M4": [("double inc = t.Size > Eps ? JobIncome(t, src) : EmptySeatIncome(w, r, t, src, c);", "double inc = t.IsSoldier ? EmptySeatIncome(w, r, t, src, c) : JobIncome(t, src);", 3)],
    "M5": [("if (t.IsSoldier) f = Math.Min(f, soldierVac);", "", 1), ("if (dst.IsSoldier) f = Math.Min(f, soldierVac);", "", 1)],
}
if MUT not in EDITS:
    sys.exit(f"SV_MUT が不正: {MUT}")
b = open(P, "rb").read()
for old, new, n in EDITS[MUT]:
    ob, nb = old.encode("utf-8"), new.encode("utf-8")
    c = b.count(ob)
    if c != n:
        sys.exit(f"{MUT}: 「{old}」が {c} 回（期待 {n}）")
    b = b.replace(ob, nb)
open(P, "wb").write(b)
print(f"{MUT} 当てた")
