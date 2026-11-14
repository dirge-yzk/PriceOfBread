# (1126) 最初の施行の年（国×種の最初の 0→1 以上の上げ・試合の頭で既に施行の q0 は上げが無いので数えない＝旧の米 7/10 種などは表に出ない）を腕ごとに：中央・1894 年より前の割合・主要 9 国の種ごとの年
#   python -X utf8 -I q4.py <q1 の _ups.tsv> <q1 の _nation.tsv の頭は使わない> 
import sys, csv, collections, statistics
sys.stdout.reconfigure(newline='\n')
first = {}
with open(sys.argv[1], encoding='utf-8', newline='') as f:
    r = csv.reader(f, delimiter='\t'); h = next(r)
    for a, s, n, q, y, fr, to, cat, dd, pp in r:
        if fr == '0':
            k = (a, s, n)
            if k not in first: first[k] = (int(y), cat)
K9 = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'オーストリア', 'アメリカ', '日本', '清', 'オスマン']
arms = sorted({k[0] for k in first}, key=lambda a: list(dict.fromkeys(k[0] for k in first)).index(a))
for a in arms:
    ys = [v[0] for k, v in first.items() if k[0] == a]
    pre = sum(1 for y in ys if y < 1894)
    print(f'{a}\t国×種 {len(ys)}\t最初の年の中央 {statistics.median(ys):.0f}\t1894 年より前 {pre}（{pre/len(ys):.2f}）')
    for n in K9:
        L = sorted((int(k[1]), v[0], v[1]) for k, v in first.items() if k[0] == a and k[2] == n)
        print(f'   {n}\t{len(L)}/10\t' + ' '.join(f's{s}:{y}{c[0]}' for s, y, c in L))
