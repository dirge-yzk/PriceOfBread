# (1065) 国ごとの最初に徴兵制（選抜以上）になる年（種ごと）・全期間の志願の四半期の割合（読むだけ）。呼び方：python -X utf8 -I sz_lawfirst.py <生> <種の始め> <種の終わり>
import sys, os
RAW, S0, S1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
NAT = ["清", "マラータ同盟", "日本", "イギリス", "アメリカ", "スペイン"]
res = {n: [] for n in NAT}; vol = {n: [] for n in NAT}
for s in range(S0, S1 + 1):
    with open(os.path.join(RAW, f"nation_s{s}.tsv"), "rb") as f:
        h = f.readline().decode().rstrip("\r\n").split("\t"); iq, ino, il, iy = h.index("quarter"), h.index("nation"), h.index("conscriptionLaw"), h.index("year")
        first = {}; cnt = {n: [0, 0] for n in NAT}
        for line in f:
            t = line.decode().rstrip("\r\n").split("\t"); n = t[ino]
            if n not in res: continue
            cnt[n][0] += 1; cnt[n][1] += 1 if t[il] == "0" else 0
            if t[il] != "0" and n not in first: first[n] = int(t[iy])
    for n in NAT:
        res[n].append(first.get(n, "なし")); vol[n].append(f"{cnt[n][1] / cnt[n][0]:.2f}" if cnt[n][0] else "-")
for n in NAT: print(n, "最初の徴兵制の年:", ",".join(map(str, res[n])), " 志願の割合:", ",".join(vol[n]))
