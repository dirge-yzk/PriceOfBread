# 使い方: python -X utf8 -I seedinfra_c137.py <腕の rawdir> <基準の rawdir>
# (1319) c137k49 記録の読み手（読むだけ・腕の後に書いた＝門ではない）：門 H の赤 2 行（英・仏の基盤の 3 生産者の 1913 の能力）を種ごとに出す（帰属の材料）。
import sys
INFRA={"水道局","ガス灯","電灯網"}
def one(raw,s):
    acc={}
    with open(f"{raw}/prodcap_s{s}.tsv",encoding="utf-8") as f:
        h=f.readline().rstrip("\n").split("\t"); ix={k:i for i,k in enumerate(h)}
        for l in f:
            if not l.startswith("1913\t"): continue
            c=l.rstrip("\n").split("\t")
            if c[ix["recipe"]] in INFRA and c[ix["ownerNation"]] in ("イギリス","フランス"):
                k=(c[ix["ownerNation"]],c[ix["recipe"]]); acc[k]=acc.get(k,0)+float(c[ix["capacity"]])
    return acc
for tag,raw in (("腕",sys.argv[1]),("基",sys.argv[2])):
    for s in range(1,11):
        a=one(raw,s)
        print(tag,s,"英",round(sum(v for k,v in a.items() if k[0]=="イギリス"),2),{k[1]:round(v,2) for k,v in a.items() if k[0]=="イギリス"},"仏",round(sum(v for k,v in a.items() if k[0]=="フランス"),2))
