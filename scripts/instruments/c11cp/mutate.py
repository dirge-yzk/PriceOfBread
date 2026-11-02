# (800) c11cp 突然変異：使い捨ての木に 1 つずつ誤りを入れる。使い方: MU=Mu6 python -I mutate.py <木の根>（batch.sh prep の編集の台本として呼ぶ＝木の根だけを渡す）。バイトで読み書きする（CRLF と BOM を保つ）。
#   Mu6   実際の賠償の元が saPay でなく sa のまま（repBase の戦勝点を取り分に掛けない）＝旧の列 repRealIndex〔CpRepRaw＝saPay で組み直した値〕は通り、新しい列 repMovedIndex の門で落ちる
#   T1m1  前走査が塹壕の前の押しで同値の敗者を決める（今の (791) の形）＝試験 T1 が赤
#   T1m2  TrenchAdjust の A・B の機関銃を取り違える＝試験 T1 が赤
import sys, os
root = sys.argv[1]; mu = sys.argv[2] if len(sys.argv) > 2 else os.environ['MU']
D = root + '/PriceOfBread.SimCore/Diplomacy.cs'
def rep(path, old, new):
    b = open(path, 'rb').read()
    crlf = b'\r\n' in b
    s = b.decode('utf-8').replace('\r\n', '\n')
    assert s.count(old) == 1, (mu, old[:60], s.count(old))
    s = s.replace(old, new)
    open(path, 'wb').write((s.replace('\n', '\r\n') if crlf else s).encode('utf-8'))
if mu == 'Mu6':
    rep(D, "double repBase = (c.ReparationGdpFraction.Value * loser.GdpPerMonth * saPay * (farCrush", "double repBase = (c.ReparationGdpFraction.Value * loser.GdpPerMonth * sa * (farCrush   /*MUTANT*/")
elif mu == 'T1m1':
    rep(D, "if (ac && bc) TrenchAdjust(c, cw, ref tA, ref tB);", "/*MUTANT*/")
elif mu == 'T1m2':
    rep(D, "aPush *= 1.0 - c.TrenchDefenseWeight * qB * (1.0 - t3A);", "aPush *= 1.0 - c.TrenchDefenseWeight * qA * (1.0 - t3A);   // MUTANT")
else:
    raise SystemExit('unknown ' + mu)
print('mutated', mu)
