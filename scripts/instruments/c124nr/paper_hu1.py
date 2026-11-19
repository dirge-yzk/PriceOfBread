"""(1230) c124nr 紙の台本（読むだけ・近似）：nr の腕 B の生（keep_1219_rej/nrB）の統一の宣戦（wars_s の entryCode=declare）を、
旗 B（攻め手の厭戦＞0.5 で塞ぐ）と旗 A の形 1（f を最大の国の外の割合へ）で篩った残りを種ごとに数える（静的な反実仮想＝軌道は変わる）。
  厭戦＝scalars_s の warExhaustion（宣戦の年の前の年末）・民族主義＝ideology_s の ideoNationalNationalismShare（前の年末）。
  形 1 の近似：民族主義の増え（nrB−基準）に f1÷f0（攻め手の主の文化の開幕の値・out/f_forms.tsv）を掛け、基準（keep_1226/base_ins の同じ種・同じ国・同じ年）に足す。
    衝撃の項（DefeatShock→r[4]）は f を通らない＝増えの全てに比を掛けるのは減りの上限（甲）。比を掛けない（乙）＝厭戦だけの残り。
  使い方: python paper_hu1.py <nrB の生> <基準の生> <f_forms.tsv>"""
import sys, csv, collections
NRB, BASE, FF = sys.argv[1], sys.argv[2], sys.argv[3]
ratio = {}
for r in csv.DictReader(open(FF, encoding="utf-8", newline=""), delimiter="\t"):
    f0, f1 = float(r["f0_now"]), float(r["f1_outside_largest"])
    ratio[r["nation"]] = f1 / f0 if f0 > 0 else 1.0

def nat(d, s):
    out = {}
    with open(f"{d}/ideology_s{s}.tsv", encoding="utf-8", newline="") as f:
        h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip("\n").split("\t")
            try: out[(c[ix["nation"]], int(c[ix["year"]]))] = float(c[ix["ideoNationalNationalismShare"]])
            except (ValueError, IndexError): pass
    return out

def exh(d, s):
    out = {}
    with open(f"{d}/scalars_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            try: out[(r["nation"], int(r["year"]))] = float(r["warExhaustion"])
            except ValueError: pass
    return out

tot = []; wok = []; ka = []; kb = []; att = collections.Counter(); attA = collections.Counter()
for s in range(1, 11):
    nb, nm, ex = nat(NRB, s), nat(BASE, s), exh(NRB, s)
    t = w = a = b = 0
    with open(f"{NRB}/wars_s{s}.tsv", encoding="utf-8", newline="") as f:
        for r in csv.DictReader(f, delimiter="\t"):
            if r["civilWarFlag"] != "0" or r["entryCode"] != "declare": continue
            t += 1
            y = int(r["startYear"]) - 1; A = r["sideA"]
            e = ex.get((A, y))
            if e is None or e != e or e > 0.5: continue
            w += 1; att[A] += 1
            n1 = nb.get((A, y)); n0 = nm.get((A, y))
            if n1 is None: continue
            if n1 >= 0.25: b += 1
            na = (n0 if n0 is not None else n1) + (n1 - (n0 if n0 is not None else n1)) * ratio.get(A, 1.0)
            if na >= 0.25: a += 1; attA[A] += 1
    tot.append(t); wok.append(w); ka.append(a); kb.append(b)
print("種ごとの統一の宣戦 nrB", tot)
print("厭戦≦0.5 の残り（旗 B だけ）", wok, "平均", sum(wok) / 10)
print("乙＝旗 B＋民族主義≧0.25（比なし）", kb, "平均", sum(kb) / 10)
print("甲＝旗 B＋形 1 の比で縮めた民族主義≧0.25", ka, "平均", sum(ka) / 10)
print("厭戦≦0.5 の攻め手", att.most_common(10))
print("甲の攻め手", attA.most_common(10))
