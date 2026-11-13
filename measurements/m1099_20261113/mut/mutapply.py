"""(1099) c105gp 突然変異を木に当てる（使い捨ての木・batch.sh prep の編集の台本としても呼べる）。
使い方: python mutapply.py <M1|M2|M3|M4|M5> <木の根>
バイトで読み書きする（改行と BOM を保つ）。当てる文字列がちょうど 1 回現れなければ失敗（2）。
"""
import sys, os

MUT = {
    # 開院の分岐の条件を壊す（空の議席で発火しない）
    "M1": [(b"if (seatsEmpty && w.QuartersSinceElection >= 1", b"if (!seatsEmpty && w.QuartersSinceElection >= 1")],
    # 選挙の政体で毎期 得票の第一党を与党にする
    "M2": [(b"w.QuartersWithElections++;", b"w.QuartersWithElections++; w.GoverningParty = (int)Winner(w);")],
    # 非選挙でも得票の第一党を与党にする
    "M3": [(b"w.GoverningParty = (int)RulingParty(w.Polity);", b"w.GoverningParty = (int)Winner(w);")],
    # 開院で Winner を呼ばない
    "M4": [(b"            w.GoverningParty = (int)Winner(w);   // \xe9\x96\x8b\xe9\x99\xa2", b"            // M4   // \xe9\x96\x8b\xe9\x99\xa2")],
    # 写さないだけで空にしない
    "M5": [(b"if (c.NonElectiveSeatsEmpty > 0) { w.PartySeats[0] = 0; w.PartySeats[1] = 0; w.PartySeats[2] = 0; }", b"if (c.NonElectiveSeatsEmpty > 0) { }")],
}


def main():
    name, root = sys.argv[1], sys.argv[2]
    p = os.path.join(root, "PriceOfBread.SimCore", "Parliament.cs")
    b = open(p, "rb").read()
    for old, new in MUT[name]:
        if b.count(old) != 1:
            print(f"{name}: 当てる文字列が {b.count(old)} 回", file=sys.stderr); sys.exit(2)
        b = b.replace(old, new)
    open(p, "wb").write(b)
    print(f"{name} 当てた")


main()
