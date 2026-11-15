# (1151) c113cr 突然変異ごとの門の赤と緑の表（gates_<M>.txt を読むだけ）。
# 使い方: python -X utf8 -I mut_table_cr.py <作業フォルダ> <M...> > mut_table.txt
import sys, os
sys.stdout.reconfigure(encoding='utf-8', newline='\n')
W = sys.argv[1]; ms = sys.argv[2:]
gates = ['C1 同点', 'C2 凍結', 'C4 ρ の回数と重み', 'C5 q', 'C6 海軍', 'C7 i', 'C10 目標の写し', 'C11 候補の集合', 'C12 τ の並び順', 'C13 開幕のデータ', 'C14 外から先に削る', '開幕 目標−RuleInit',
         'C8a 寄せ', 'C8c″ 力の落ち', 'C8e 州が1つの国の寄せ', 'G2 交戦中の首都', 'C5 壊さない', 'C9′ 壊さない', 'C15 尾の質量', 'G1′ 遠い漂い']
res = {}
for m in ms:
    d = {}
    p = os.path.join(W, f'gates_{m}.txt')
    if os.path.exists(p):
        for line in open(p, encoding='utf-8'):
            c = line.rstrip('\n').split('\t')
            if len(c) >= 3 and c[1] in ('緑', '赤'): d[c[0]] = (c[1], c[2][:70])
    res[m] = d
print('門\t' + '\t'.join(ms))
for g in gates: print(g + '\t' + '\t'.join(res[m].get(g, ('-',))[0] for m in ms))
print()
for g in ('C15 尾の質量', 'C5 壊さない', 'C9′ 壊さない', 'C8c″ 力の落ち'):
    for m in ms:
        if g in res[m]: print(g, m, res[m][g][1])
