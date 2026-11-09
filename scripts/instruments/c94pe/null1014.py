# (1014) c94pe ヌル腕の幅（読むだけ）：cpop_s（植民地格だけ）の商人の行で popK≤1e-9 の割合を種ごとと 10 種の和÷和で。
# 使い方：python null1014.py <生のフォルダ>...（各フォルダの cpop_s1..10）
import os, sys
for d in sys.argv[1:]:
    tot = [0, 0]; per = []
    for s in range(1, 11):
        p = os.path.join(d, f'cpop_s{s}.tsv')
        if not os.path.exists(p): continue
        n = e = 0
        with open(p, 'rb') as f:
            h = f.readline().decode('utf-8').rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            ci, pk = ix['className'], ix['popK']
            for ln in f:
                x = ln.decode('utf-8').rstrip('\r\n').split('\t')
                if x[ci] == '商人': n += 1; e += float(x[pk]) <= 1e-9
        per.append(e / max(1, n)); tot[0] += e; tot[1] += n
    print(f"{d}: 種 {len(per)} 和÷和 {tot[0]/max(1,tot[1]):.4f}（{tot[0]}/{tot[1]}） 種の幅 {min(per):.4f}〜{max(per):.4f}")
