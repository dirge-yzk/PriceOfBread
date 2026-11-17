# (1195) c119us 突然変異の編集（使い捨ての木に当てる・バイトで読み書き）。python -X utf8 -I mut.py <木> <M1..M5>
import sys
root, m = sys.argv[1], sys.argv[2]
D = root + '/PriceOfBread.SimCore/Diplomacy.cs'; F = root + '/PriceOfBread.SimCore/Formations.cs'
E = {
 'M1': [(D, 'if (Formations.UnificationWarBlock(this, p, targets, t) == Formations.UnificationBlock.None && DeclareWar(p, t, c))', 'if (DeclareWar(p, t, c))')],
 'M2': [(F, "         : d.HasAlliance(p, t) ? UnificationBlock.Allied\n", '')],
 'M3': [(F, "         : d.IsAtWar(p, t) ? UnificationBlock.AtWar\n         : d.HasAlliance(p, t) ? UnificationBlock.Allied\n",
            "         : d.HasAlliance(p, t) ? UnificationBlock.Allied\n         : d.IsAtWar(p, t) ? UnificationBlock.AtWar\n")],
 'M4': [(F, "=> !targets.Contains(t) ? UnificationBlock.NotTarget\n         : d.IsAtWar", "=> d.IsAtWar")],
 'M5': [(D, 'targets.Sort((x, y) => x.Military != y.Military ? x.Military.CompareTo(y.Military) : x.Id.CompareTo(y.Id));   // 最弱から',
            'targets.Sort((x, y) => x.Military != y.Military ? y.Military.CompareTo(x.Military) : x.Id.CompareTo(y.Id));   // 最弱から')],
}
for p, o, n in E[m]:
    b = open(p, 'rb').read(); crlf = b'\r\n' in b
    ob, nb = o.encode(), n.encode()
    if crlf: ob, nb = ob.replace(b'\n', b'\r\n'), nb.replace(b'\n', b'\r\n')
    assert b.count(ob) == 1, (m, p, b.count(ob))
    open(p, 'wb').write(b.replace(ob, nb))
print('applied', m)
