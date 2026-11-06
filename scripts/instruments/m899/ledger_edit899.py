# (899) 合流：台帳の写し（reanchor899.py の出力に §4-2・§4-3 を足し、§0 に錨の注、§1 の S17・S20 の状態、S27-P01 の最後に確かめたコミットを直す）
#   python -X utf8 ledger_edit899.py <入力 md> <足す md> <出力 md>
import sys
src, add, dst = sys.argv[1:4]
t = open(src, 'rb').read().decode('utf-8')
a = open(add, 'rb').read().decode('utf-8')
def rep1(s, old, new):
    assert s.count(old) == 1, (old, s.count(old))
    return s.replace(old, new)
note = "- **錨の座標**（(899) の合流）：(893) c76lg の台帳の錨は 731d3de1 の行だった。(899) の合わせた木で c76cs の Coefficients.cs の 1 行（:2962 SummonByInterestShare）と c76ls・c76cs の SimCore の行が動き、137 本の錨が引けなくなった＝`scripts/instruments/m899/reanchor899.py` で行番号だけを写した（名と他の欄は変えない・一致の塊で 136・近くで 1・写せない 0・measurements/m899_20261106/reanchor899.tsv）。〈最後に確かめた〉の列は読んだ木のまま（行番号は今の main の木）。\n"
t = rep1(t, "- 生の名：R1＝", note + "- 生の名：R1＝")
t = rep1(t, "この波の線が埋める (889) c75cp・(892) が §4 に門の行を写した（見出しの他の門は未確かめ） |\n| S18",
         "この波の線が埋める (889) c75cp・(892) が §4 に門の行を写した・(899) が §4-2 に (896) c76cs の召集の旗の行を写した（見出しの他の門は未確かめ） |\n| S18")
t = rep1(t, "| 未確かめ（土地税の行は (891) c75ds） |", "| 未確かめ（土地税の行は (891) c75ds・(899) が §4-2 に (894) c76lb の計器 landbase_s の読みを写した） |")
old = "| Instruments.cs:1220:CedeBefore | Territory.cs Diplomacy.cs | 両側の門 | 11db605f |"
t = rep1(t, old, old.replace("11db605f", "731d3de1"))
if not t.endswith('\n'): t += '\n'
t += a
open(dst, 'wb').write(t.encode('utf-8'))
print('ok', t.count('\n'))
