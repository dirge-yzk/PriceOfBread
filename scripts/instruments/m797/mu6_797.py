# (797) 合流の突然変異 Mu6（C_lo を落とすはずの誤り＝集合の本国の割譲の枠を最初から 0 にする）。使い捨ての木にだけ当てる。
#   使い方: python -I mu6_797.py <木の根>（batch.sh prep の編集の台本）。反証役 c10cp の low〈C_lo を落とす突然変異が無い〉への答え。
import sys
root = sys.argv[1]
D = root + '/PriceOfBread.SimCore/Diplomacy.cs'
b = open(D, 'rb').read()
old = 'if (cg is not null && cg.CedeLeft <= 0) return false;'.encode('utf-8')
new = 'if (cg is not null) return false;   // MUTANT Mu6'.encode('utf-8')
assert b.count(old) == 1, b.count(old)
open(D, 'wb').write(b.replace(old, new))
print('mutated Mu6')
