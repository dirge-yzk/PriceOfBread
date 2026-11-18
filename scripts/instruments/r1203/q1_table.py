# (1203) 問い① 成績表の〈ハーネス不合格〉の行（× と △ の満足度・最小国・下流の無い財）を種ごとに表へ（成績表の文字だけを読む）
# 使い方: python -X utf8 -I q1_table.py <measurements のフォルダ> <出力> <成績表の名…>
import sys, re
md, outp, names = sys.argv[1], sys.argv[2], sys.argv[3:]
pat = re.compile(r'seed(\d+)=実測([\d.]+)（([^・）]+)(?:・州(\d+)/(\d+)・主食([\d.]+)・人口(\d+)(?:・直近8四半期 平均([\d.]+) 振れ([\d.]+))?)?')
with open(outp, 'w', encoding='utf-8', newline='\n') as o:
    o.write('report\titem\tverdict\tseed\tvalue\tnation\tregions\tfounding\tfoodFill\tpopK\trecent8_mean\trecent8_swing\n')
    for n in names:
        for l in open(f'{md}/{n}', encoding='utf-8'):
            m0 = re.match(r'^\s+([×△]) ([^：]+)：(\d+)/(\d+)', l)
            if not m0 or m0.group(2) not in ('満足度・最小国', '下流の無い財'): continue
            for m in pat.finditer(l):
                o.write('\t'.join([n, m0.group(2), m0.group(1)] + [x or '' for x in m.groups()]) + '\n')
