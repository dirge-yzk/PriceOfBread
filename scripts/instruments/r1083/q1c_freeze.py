# (1083) 問い 1 の補い：冬の港の凍結（計器の限界②）で首都からの距離が inf になる州の届き ρ と影の区分を、種 1 の四半期 0〜9 で並べる。
# 使い方: python -X utf8 -I q1c_freeze.py <表のフォルダ> > q1c_freeze_out.txt
import sys, os
import sys as _s; _s.stdout.reconfigure(newline='\n')  # 出力は LF
d = sys.argv[1]
fi = open(os.path.join(d, 'rulepower_s1.tsv'), encoding='utf-8'); h = fi.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
print('四半期\t州\t首都からの km\tρ\t影の最大\t最大の外国')
for line in fi:
    c = line.rstrip('\n').split('\t'); q = int(c[0])
    if q > 9: break
    if c[ix['region']] in ('カナダ', 'ナイン', 'フォートウィリアム', 'ルイジアナ・フロリダ'):
        print(q, c[ix['region']], c[ix['directKm']], c[ix['rhoNewIndex']], c[ix['bestClass']], c[ix['foreignTopKey']], sep='\t')
