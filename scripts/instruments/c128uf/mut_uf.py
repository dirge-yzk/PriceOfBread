"""(1258) c128uf 突然変異を使い捨ての木へ入れる（バイトで読み書き）。使い方: python mut_uf.py <木> <μ0|μ1|μ2|μ3|μ4>"""
import sys
root, m = sys.argv[1], sys.argv[2]
F = root + '/PriceOfBread.SimCore/Formations.cs'; D = root + '/PriceOfBread.SimCore/Diplomacy.cs'
CALL = '            if (c.UnificationSkipFormed > 0 && FormedElsewhere(d, p, r)) continue;'
LF, CRLF = bytes([10]), bytes([13, 10])
def rep(path, a, b):
    s = open(path, 'rb').read(); a, b = a.encode(), b.encode()
    if CRLF in s: a, b = a.replace(LF, CRLF), b.replace(LF, CRLF)   # Diplomacy.cs は CRLF
    assert s.count(a) == 1, (path, a[:60], s.count(a)); open(path, 'wb').write(s.replace(a, b))
if m == 'μ0': rep(F, CALL, CALL.replace('c.UnificationSkipFormed > 0', 'false'))   # 旗を見ない＝何もしない実装
elif m == 'μ1': rep(F, 'if (q == p || q.Nation is null || q.FormationTier < r.Tier)', 'if (q != p || q.Nation is null || q.FormationTier < r.Tier)')   # 自分の名と比べる
elif m == 'μ2': rep(F, '                if (f.NewName == q.Name && f.Tier >= r.Tier && (f.CultureIs ?? f.NewName) == key) return true;', '                return true;')   # 札を見ず段だけ
elif m == 'μ3':   # AI の呼び手だけに判定を入れる
    rep(F, CALL, '')
    rep(F, 'private static bool FormedElsewhere', 'internal static bool FormedElsewhere')
    rep(D, '            var rule = Formations.UnificationCandidate(this, p, pw, c);\n            if (rule is null) continue;',
        '            var rule = Formations.UnificationCandidate(this, p, pw, c);\n            if (rule is null || (c.UnificationSkipFormed > 0 && Formations.FormedElsewhere(this, p, rule))) continue;')
elif m == 'μ4':   # 札でなく規則の名だけ（指示の文の形）
    rep(F, 'string key = r.CultureIs ?? r.NewName;', 'string key = r.NewName;')
    rep(F, '(f.CultureIs ?? f.NewName) == key', 'f.NewName == key')
else: raise SystemExit('μ?')
print('applied', m)
