# (1096) 合流：〈次の波（波105〜）〉の行に、前の行（波104〜）の節を中身のまま運ぶ（反証役の指摘＝〈前の行のまま〉は畳んだ行を指して中身が消える）。
#   前の行の <br> の節のうち、0（見出しと ④ 群 1＝閉じた）・2（計器 MCi→MC＝新しい行の 1 ② へ）・21（推奨の組＝置き換え）を除き、他を中身のまま足す。
O = 'D:/PoB_work/wt_m1096/docs/202609/00_未着手_部分実装_差し戻し_一覧_20260906.md'
s = open(O, 'rb').read().decode('utf-8'); nl = '\r\n' if '\r\n' in s else '\n'
L = s.split(nl)
old = open('D:/PoB_work/m1096_work/next104.txt', 'rb').read().decode('utf-8').rstrip('\r\n')
seg = old.split('<br>'); assert len(seg) == 22, len(seg)
assert seg[21].startswith('**この次の波の推奨の組**'), seg[21][:20]
carry = [seg[i] for i in range(1, 21) if i != 2]
k = [i for i, l in enumerate(L) if l.startswith('| **次の波（波105〜・(1096)')]; assert len(k) == 1
row = L[k[0]]; ns = row.split('<br>'); assert len(ns) == 8, len(ns)
assert ns[5].startswith('**前の行から続く物**') and ns[6].startswith('**元の残りタスク**')
add = '**(1096) の足し**：⑲＋uifix の 18d6253e8 の後・Runner/Program.cs:1116 の文言〈CenterIsFirstProvince〉・FactoryBuildProgress の priceLevel の呼び口。⑳＋fb の持ち分の外の古い注〔Simulation.cs :695〜:699 付近・Coefficients.cs の Compost の直前・ScarceMineralGate の墓標の上〕。'
head = '**前の行（波104〜）から運んだ節**（中身は前の行のまま・(1096) で閉じた物＝計器 MCi・支配の力の計器 S1〜S4・読む〈開幕の数年で欧州の陸軍力が半分以下に落ちる理由〉〔(1092)＝R1 → R2〕・uifix の 601cd9c2e の後〜18d6253e8・⑳ の InstrumentsRulePower.cs:7・:85 の注と an_rp.py の 40 四半期〔(1091) が直した〕）：' + carry[0]
newrow = '<br>'.join(ns[:5] + [add, head] + carry[1:] + [ns[7]])
L[k[0]] = newrow
open(O, 'wb').write(nl.join(L).encode('utf-8'))
print('ok', 'carried', len(carry), 'len', len(newrow))
