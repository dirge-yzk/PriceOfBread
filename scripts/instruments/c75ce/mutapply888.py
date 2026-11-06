# (888) c75ce 突然変異の置き換え（バイトで読み書き＝改行と BOM を保つ・ちょうど 1 か所でなければ失敗）。
#   python -X utf8 mutapply888.py <ファイル> <番号>
import sys

MUT = {
    '1': [("static string CedeKey(Power p) => p.BaseName.Length > 0 ? p.BaseName : p.Name;", "static string CedeKey(Power p) => p.Name;")],
    '2': [("bool added = r.ForeignRulers.Length > ev.RulersBefore.Length || sumA > sumB;", "bool added = false;")],
    '3': [("bool movedAgain = seen > 1 || (ev.To is not null && !ReferenceEquals(own, ev.To));", "bool movedAgain = ev.To is not null && !ReferenceEquals(own, ev.To);")],
    '4': [("st.Rows.Add(string.Join('\\t', I(q), I(ev.Year),", "st.Rows.Add(string.Join('\\t', I(q), I(own?.Year ?? ev.Year),")],
    '5': [("if (from.WarmupFrozen || from.Diplomacy is not { } dip) return;", "if (from.Diplomacy is not { } dip) return;")],
    '6': [("internal const double CedeIdentTol = 1e-12;", "internal const double CedeIdentTol = 1;")],
    '7': [("        if (Instruments.On) Instruments.RegionMoved(from, to, r, how);", "        // 突然変異 M7：呼び口を Rehome の後へ"),
          ("        Rehome(r, how, sameCore);\n", "        Rehome(r, how, sameCore);\n        if (Instruments.On) Instruments.RegionMoved(from, to, r, how);\n")],
    '8': [("            dm = CedeDiffMax(r.ForeignShares, ev.SharesBefore, dm);\n", "")],
}

def main():
    path, idx = sys.argv[1], sys.argv[2]
    with open(path, 'rb') as fh:
        b = fh.read()
    for a, c in MUT[idx]:
        ab, cb = a.encode('utf-8'), c.encode('utf-8')
        n = b.count(ab)
        if n != 1:
            print('mut', idx, '一致の数', n, a[:60]); sys.exit(1)
        b = b.replace(ab, cb)
    with open(path, 'wb') as fh:
        fh.write(b)
    print('mut', idx, '当てた')

main()
