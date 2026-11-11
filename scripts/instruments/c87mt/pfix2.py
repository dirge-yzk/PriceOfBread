# 紙の台本：pfix_s の kind=2（実質 GDP が正でない四半期）を国ごとに数え、国の通し番号の最初の 2 つ（w0〜w1・q0〜q1）の行を列挙＝初期化の四半期の近くで GDP が 0 だった国の候補
import sys, os, collections
d, a0, a1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
tot = 0
for s in range(a0, a1 + 1):
    p = os.path.join(d, f'pfix_s{s}.tsv')
    if not os.path.exists(p): print(s, 'pfix なし'); continue
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        c = collections.Counter(); early = []
        for l in f:
            x = l.rstrip('\n').split('\t')
            if x[ix['kind']] != '2': continue
            c[x[ix['nation']]] += 1
            t = x[ix['time']]
            if int(t[1:]) <= 1: early.append((t, x[ix['nation']], x[ix['year']]))
    tot += sum(c.values())
    print(s, 'kind2 行', sum(c.values()), '国', len(c), '最初の2つ', early[:6], sep='\t')
print('計', tot)
