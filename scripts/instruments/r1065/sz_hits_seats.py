# (1065) 問い 3 の細かい分け＋国の空席の近似（読むだけ）。呼び方：python -X utf8 -I sz_hits_seats.py <生> <種の始め> <種の終わり> <hit の一覧> <出力> <組>
# hit の分け：a＝前の 12 四半期が全て表に在り全て兵 0／b＝表に在る分（12 未満＝系列が窓の中で始まった）が全て兵 0／c＝0 と正が混ざる／d＝全て兵あり／e＝表に無い
#   反証役の数（39/53）との突き合わせ用に、年末の行（quarter%4==3）で y-3〜y-1 の在る分が全て 0 も数える。
# 国の空席〈近似〉：admrule_s の seatsK（定員＝EstablishmentOf）と soldierK（兵の和）の比の、国ごとの年末の行の中央（種の中央）。
#   限界：定員は兵 0 の州の分も含む（兵士 POP が在れば大きさ 0 でも RegionManpower が正）＝空席が兵 0 の州から来たとは、この表だけでは言えない（近似）。
import sys, os, statistics as st
from collections import defaultdict, Counter
RAW, S0, S1, HITS, OUT, TAG = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4], sys.argv[5], sys.argv[6]
hits = defaultdict(list)
f = False
for line in open(HITS, encoding="utf-8"):
    if line.startswith("# A1 で 0 に届く州の一覧"): f = True; continue
    if f and line.strip():
        s, n, r, y, t = line.rstrip("\n").split("\t"); hits[int(s)].append((n, r, int(y), t))
SOL = "\t兵士\t".encode()
cat = Counter(); catN = Counter(); ye = Counter()
ratio = defaultdict(list)
for seed in range(S0, S1 + 1):
    want = {(n, r) for (n, r, y, t) in hits.get(seed, [])}
    S = {}
    cp = os.path.join(RAW, f"cpop_s{seed}.tsv")
    if os.path.exists(cp) and want:
        with open(cp, "rb") as fh:
            fh.readline()
            for line in fh:
                if SOL not in line: continue
                t = line.split(b"\t")
                k = (t[2].decode(), t[3].decode())
                if k in want: S[(int(t[0]),) + k] = float(t[8])
    for (n, r, y, ti) in hits.get(seed, []):
        qh = (y - 1783) * 4
        got = [S[(q, n, r)] for q in range(qh - 12, qh) if (q, n, r) in S]
        if not got: c = "e 表に無い"
        elif all(x == 0 for x in got): c = "a 12四半期すべて0" if len(got) == 12 else "b 在る分すべて0（系列が窓の中で始まる）"
        elif any(x == 0 for x in got): c = "c 0と正が混ざる"
        else: c = "d 兵あり"
        cat[c] += 1; catN[(n, c[0])] += 1
        yr = [S[(q, n, r)] for q in range(qh - 12, qh) if q % 4 == 3 and (q, n, r) in S]
        ye["年末の行 y-3〜y-1 の在る分が全て0" if yr and all(x == 0 for x in yr) else ("年末の行が無い" if not yr else "それ以外")] += 1
    ap = os.path.join(RAW, f"admrule_s{seed}.tsv")
    with open(ap, "rb") as fh:
        h = fh.readline().decode().rstrip("\r\n").split("\t")
        iq, ino, ise, iso = h.index("quarter"), h.index("nation"), h.index("seatsK"), h.index("soldierK")
        per = defaultdict(list)
        for line in fh:
            t = line.decode().rstrip("\r\n").split("\t")
            if int(t[iq]) % 4 != 3: continue
            try:
                se, so = float(t[ise]), float(t[iso])
            except ValueError: continue
            if se > 0: per[t[ino]].append(so / se)
        for n, xs in per.items(): ratio[n].append(st.median(xs))
with open(OUT, "w", encoding="utf-8", newline="\n") as o:
    o.write(f"# (1065) 問い3 の細分と国の空席〈近似〉 組={TAG} 生={RAW} 種={S0}〜{S1}\n")
    o.write("A1 の hit\t" + "\t".join(f"{k}={v}" for k, v in sorted(cat.items())) + f"\t計={sum(cat.values())}\n")
    o.write("年末の行\t" + "\t".join(f"{k}={v}" for k, v in sorted(ye.items())) + "\n")
    o.write("国×区分\t" + "\t".join(f"{a}/{b}={v}" for (a, b), v in sorted(catN.items(), key=lambda z: -z[1])) + "\n")
    o.write("# 兵÷定員（年末の行の国の中央の、種の中央）＝小さいほど空席が多い〈近似〉\n")
    for n in ["清", "マラータ同盟", "日本", "イギリス", "アメリカ", "スペイン", "フランス", "ロシア", "ポルトガル", "ネーデルラント", "プロイセン", "オーストリア"]:
        if ratio.get(n): o.write(f"{n}\t{st.median(ratio[n]):.3f}\t種={len(ratio[n])}\n")
