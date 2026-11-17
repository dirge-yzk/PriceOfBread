# (1188) c118ur 統一戦争の AI の待ちと 30% の乱数を外す線の読み手（計器の表だけを読む・エンジンの関数を呼ばない）。
#   python -X utf8 -I ur.py paper <ins（D:/…）> <種の起点> <種の終わり> <出力.json>
#   python -X utf8 -I ur.py gates <腕の paper.json> <基線の paper.json>   ＝止める線（戦争の数の発散）と史実の桁の線を読む
# 読む表：wars_s<n>.tsv（startYear civilWarFlag sideA sideB months deadK q entryCode）・ideology_s<n>.tsv。
# entryCode＝declare は Diplomacy.DeclareWar の既定の札（InstrumentsWar.WarTagDefault 3）＝selfplay で DeclareWar を呼ぶのは統一の AI だけ
#   （:2105 の仲裁の拒否は札 9 を先に立てる・プレイヤーの DeclareUnificationWarOn は PlayHarness と App だけ）。
import sys, json, csv, os

GERMAN_ITALIAN = ["プロイセン", "バイエルン", "ザクセン", "ハノーファー", "ヴュルテンベルク", "ライン諸邦",
                  "サルデーニャ＝ピエモンテ", "両シチリア", "教皇領", "トスカーナ", "ヴェネツィア", "ジェノヴァ"]
FORMED = ["ドイツ", "イタリア", "北ドイツ連邦"]


def read_wars(path):
    rows = []
    with open(path, encoding="utf-8", newline="") as f:
        rd = csv.reader(f, delimiter="\t")
        hdr = next(rd)
        ix = {h: i for i, h in enumerate(hdr)}
        for c in rd:
            if len(c) < len(hdr):
                continue
            rows.append({"y": int(c[ix["startYear"]]), "civil": c[ix["civilWarFlag"]] != "0", "a": c[ix["sideA"]],
                         "b": c[ix["sideB"]], "m": int(float(c[ix["months"]])), "q": int(c[ix["q"]]), "code": c[ix["entryCode"]]})
    return rows


def natio(path):
    mx = {}
    n25 = {}
    if not os.path.exists(path):
        return mx, n25
    with open(path, encoding="utf-8", newline="") as f:
        rd = csv.DictReader(f, delimiter="\t")
        for r in rd:
            n = r["nation"]
            if n not in GERMAN_ITALIAN and n not in FORMED:
                continue
            try:
                v = float(r["ideoNationalNationalismShare"])
            except ValueError:
                continue
            if v != v:
                continue
            if v > mx.get(n, -1):
                mx[n] = v
            if v >= 0.25:
                n25[n] = n25.get(n, 0) + 1
    return mx, n25


def paper(ins, sb, se, out):
    res = {"ins": ins, "seeds": {}}
    for s in range(sb, se + 1):
        p = os.path.join(ins, f"wars_s{s}.tsv")
        if not os.path.exists(p):
            res["seeds"][str(s)] = None
            continue
        w = read_wars(p)
        dec = [r for r in w if r["code"] == "declare"]
        # 多正面：同じ攻め手の統一の宣戦が、前の統一の戦争の続く間（q＋月÷3）に立った件数
        multi = 0
        for i, r in enumerate(dec):
            for r0 in dec[:i]:
                if r0["a"] == r["a"] and r0["q"] <= r["q"] < r0["q"] + (r0["m"] + 2) // 3:
                    multi += 1
                    break
        # 統一の宣戦の時点で同じ攻め手が別の戦争（内戦を除く・どの札でも）を戦っていた件数（q は四半期・月は切り上げで四半期へ）
        busy = 0
        for r in dec:
            for r0 in w:
                if r0 is r or r0["civil"]:
                    continue
                # 前の四半期までに立った戦争だけ（同じ四半期の行は宣戦の帰結＝相手の同盟の参戦）
                if (r0["a"] == r["a"] or r0["b"] == r["a"]) and r0["q"] < r["q"] < r0["q"] + (r0["m"] + 2) // 3:
                    busy += 1
                    break
        mx, n25 = natio(os.path.join(ins, f"ideology_s{s}.tsv"))
        res["seeds"][str(s)] = {
            "wars": sum(1 for r in w if not r["civil"]),
            "declare": len(dec),
            "declareList": [f'{r["y"]} {r["a"]}->{r["b"]} {r["m"]}か月' for r in dec],
            "multiFront": multi,
            "busyAtDeclare": busy,
            "formedAsSide": sorted({x for r in w for x in (r["a"], r["b"]) if x in FORMED}),
            "natioMax": {k: round(v, 4) for k, v in sorted(mx.items())},
            "natioSamplesGe25": n25,
        }
    seeds = [v for v in res["seeds"].values() if v]
    res["sum"] = {"seeds": len(seeds), "wars": sum(v["wars"] for v in seeds), "declare": sum(v["declare"] for v in seeds),
                  "declareSeeds": sum(1 for v in seeds if v["declare"] > 0), "multiFront": sum(v["multiFront"] for v in seeds),
                  "busyAtDeclare": sum(v["busyAtDeclare"] for v in seeds),
                  "declareMaxPerSeed": max((v["declare"] for v in seeds), default=0)}
    with open(out, "w", encoding="utf-8", newline="\n") as f:
        json.dump(res, f, ensure_ascii=False, indent=1)
    print(json.dumps(res["sum"], ensure_ascii=False))
    for s, v in res["seeds"].items():
        if v:
            print(f's{s} wars={v["wars"]} declare={v["declare"]} multi={v["multiFront"]} busy={v["busyAtDeclare"]} {"; ".join(v["declareList"])}')


def gates(armp, basep):
    a = json.load(open(armp, encoding="utf-8"))
    b = json.load(open(basep, encoding="utf-8"))
    stop = 0
    print("# 止める線（戦争の数の発散）：種ごとの内戦を除く戦争の件数が、基線の同じ種の 10 倍以上なら止める")
    for s, v in a["seeds"].items():
        bv = b["seeds"].get(s)
        if not v or not bv:
            print(f"s{s} 欠け")
            stop = 1
            continue
        r = v["wars"] / max(1, bv["wars"])
        flag = "STOP" if r >= 10 else "ok"
        if r >= 10:
            stop = 1
        print(f's{s} wars 腕 {v["wars"]} 基線 {bv["wars"]} 比 {r:.3f} {flag}')
    print("# 史実の桁の線：統一の宣戦（declare）の件数／種 ＞50（史実 普 3・伊 2＝5 の 10 倍）なら止める（下側は基線で既に 1/10 未満＝元から・記録）")
    mxd = a["sum"]["declareMaxPerSeed"]
    print(f"declare 種ごとの最大 {mxd} {'STOP' if mxd > 50 else 'ok'}")
    if mxd > 50:
        stop = 1
    print(f'# 記録：declare 腕 {a["sum"]["declare"]}（{a["sum"]["declareSeeds"]} 種）・基線 {b["sum"]["declare"]}（{b["sum"]["declareSeeds"]} 種）・多正面 腕 {a["sum"]["multiFront"]} 基線 {b["sum"]["multiFront"]}・宣戦の時に別の戦争中 腕 {a["sum"]["busyAtDeclare"]} 基線 {b["sum"]["busyAtDeclare"]}・戦争 腕 {a["sum"]["wars"]} 基線 {b["sum"]["wars"]}')
    print("GATES " + ("STOP" if stop else "PASS"))
    return stop


if __name__ == "__main__":
    if sys.argv[1] == "paper":
        paper(sys.argv[2], int(sys.argv[3]), int(sys.argv[4]), sys.argv[5])
    elif sys.argv[1] == "gates":
        sys.exit(gates(sys.argv[2], sys.argv[3]))
