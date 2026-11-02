# (791) c10cp 突然変異：使い捨ての木に 1 つずつ誤りを入れる（門の台本と機構の試験が赤になることを確かめる）。
#   使い方: python -I mutate.py <木の根> <Mu1..Mu5>（batch.sh prep の編集の台本として呼ぶときは環境変数 MU=Mu1 を立てる）。どの変異も旗の既定を 1 にした木に入れる
#   Mu1 旗を読まない（崩壊の集合を作らない＝旧の N 倍）／Mu2 取り分を二重に掛ける／Mu3 白紙の崩壊の講和も集合に入れる／Mu4 押しが最小の勝者の取り分を 0 にする／Mu5 本国の割譲の枠を減らさない
import sys
sys.path.insert(0, 'D:/PoB_work/c10cp/tools')
from edit import rep
import os
root = sys.argv[1]; mu = sys.argv[2] if len(sys.argv) > 2 else os.environ['MU']   # batch.sh prep は木の根だけを渡す＝環境変数 MU で選ぶ
rep(root + '/PriceOfBread.SimCore/Coefficients.cs', 'public double CollapsePeaceOneShare = 0; ', 'public double CollapsePeaceOneShare = 1; ')   # 突然変異の木は旗 1 の既定
D = root + '/PriceOfBread.SimCore/Diplomacy.cs'
if mu == 'Mu1':
    rep(D, "        bool cpOn = c.CollapsePeaceOneShare > 0;", "        bool cpOn = false;   // MUTANT")
elif mu == 'Mu2':
    rep(D, "double saPay = cg is null ? sa : cg.Sa * cgShare;", "double saPay = cg is null ? sa : cg.Sa * cgShare * cgShare;   // MUTANT")
elif mu == 'Mu3':
    rep(D, "                    && EndedWarRecord.VerdictOf(war.Score, false, c.WhitePeaceScore) != WarVerdict.Blank)\n", "                    )   // MUTANT\n")
elif mu == 'Mu4':
    rep(D, "        var order = new int[m]; for (int k = 0; k < m; k++) order[k] = k;\n", "        { int kmin = 0; for (int k = 1; k < m; k++) if (push[k] < push[kmin]) kmin = k; share[kmin] = 0; }   // MUTANT\n        var order = new int[m]; for (int k = 0; k < m; k++) order[k] = k;\n")
elif mu == 'Mu5':
    rep(D, "        if (ok && cg is not null) cg.CedeLeft--;", "        // MUTANT: 枠を減らさない")
else:
    raise SystemExit('unknown ' + mu)
print('mutated', mu)
