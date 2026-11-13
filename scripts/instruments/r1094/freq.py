# (1094) S14 の頻度（4 腕×10 種・事前登録 prereg.txt 追記 C の表と列だけ）
# 使い方: python -X utf8 -I freq.py <keep_dir> <out>
import os, sys, statistics as st
keep, out = sys.argv[1], sys.argv[2]
ARMS = [("M", "base_ins", range(1, 11)), ("N1", "n1", range(1, 11)), ("N2", "n2", range(1, 11)), ("S11", "a11", range(11, 21))]
IDEO = ["Traditionalism", "Reactionism", "Liberalism", "Republicanism", "Nationalism", "Socialism", "Communism", "Anarchism", "Imperialism", "Fascism", "Royalism"]

def rows(path):
    with open(path, "rb") as f:
        hdr = f.readline().decode("utf-8").rstrip("\r\n").split("\t")
        ix = {h: i for i, h in enumerate(hdr)}
        for line in f:
            yield ix, line.decode("utf-8").rstrip("\r\n").split("\t")

def fl(x):
    try:
        return float(x)
    except Exception:
        return float("nan")

def q(v, p):
    if not v:
        return float("nan")
    v = sorted(v)
    return v[min(len(v) - 1, int(p * (len(v) - 1) + 0.5))]

def share(n, d):
    return f"{(n / d if d else float('nan')):.4f}({n}/{d})"

res = {}
for arm, sub, seeds in ARMS:
    R = {}
    def add(k, v):
        R.setdefault(k, []).append(v)
    cnt = {}
    def inc(k, n=1):
        cnt[k] = cnt.get(k, 0) + n
    for s in seeds:
        d = os.path.join(keep, sub)
        # scalars（年末）：政体を dcont と結ぶ・defeatShock・externalPressure・dynMandate
        pol = {}
        for ix, r in rows(os.path.join(d, f"scalars_s{s}.tsv")):
            qt = int(r[ix["quarter"]]); nat = r[ix["nation"]]
            pc = int(fl(r[ix["polityCode"]])) if r[ix["polityCode"]] not in ("", "NaN") else -1
            pol[(qt, nat)] = pc
            ep = fl(r[ix["externalPressure"]])
            if ep > 1e-9:
                inc("ep_nonzero_any"); add("ep_nonzero_q", qt)
            inc("ep_rows_any")
            if qt % 4 != 3:
                continue
            inc("sc_rows")
            if fl(r[ix["defeatShock"]]) > 1e-6: inc("defeat_pos")
            if ep > 1e-9: inc("ep_pos")
            dm = fl(r[ix["dynMandate"]])
            if abs(dm - 0.5) > 0.01: inc("dm_moved")
            add("dm", dm)
        # dcont（年末）
        for ix, r in rows(os.path.join(d, f"dcont_s{s}.tsv")):
            qt = int(r[ix["quarter"]]); nat = r[ix["nation"]]
            if qt % 4 != 3:
                continue
            inc("dc_rows")
            comp = {}
            for c in ("milVoiceMeanShare", "milWarMeanShare", "milNationMeanShare", "milFamineMeanShare"):
                v = fl(r[ix[c]]); comp[c] = v
                if v > 1e-6: inc("pos_" + c)
                add(c, v)
            sm = sum(v for v in comp.values() if v == v)
            if sm > 1e-9:
                for c, v in comp.items():
                    add("frac_" + c, v / sm)
            for c in ("discontentMeanShare", "livingDropMeanIndex", "dGtMShare", "militancyMeanShare", "satMeanShare"):
                v = fl(r[ix[c]]); add(c, v)
                if v > 1e-6: inc("pos_" + c)
            if fl(r[ix["militancyMeanShare"]]) > 0.5: inc("mil_gt05")
            po, pn = fl(r[ix["partOldK"]]), fl(r[ix["partNewK"]])
            if po > 1e-9: add("partNewOverOld", pn / po)
            if pn > 1e-9: inc("partNew_pos")
            pc = pol.get((qt, nat), -1)
            if pc in (3, 4):
                inc("parl_rows")
                if fl(r[ix["satMeanShare"]]) < 0.43: inc("parl_sat_lt043")
        # ideology（年末）
        for ix, r in rows(os.path.join(d, f"ideology_s{s}.tsv")):
            qt = int(r[ix["quarter"]])
            if qt % 4 != 3:
                continue
            inc("id_rows")
            for k in IDEO:
                v = fl(r[ix[f"ideoNational{k}Share"]])
                add("id_" + k, v)
                if v > 0.01: inc("idpos_" + k)
                if v > 0.001: inc("idpos3_" + k)
        # idn（全ての国・毎四半期）
        first = {}
        for ix, r in rows(os.path.join(d, f"idn_s{s}.tsv")):
            qt = int(r[ix["quarter"]]); nat = r[ix["nation"]]; yr = int(r[ix["year"]])
            inc("idn_rows")
            for k in ("knownSoc", "knownCom", "knownImp", "knownFas"):
                if fl(r[ix[k]]) > 0.5:
                    inc("idn_" + k)
                    first.setdefault((nat, k), yr)
            if fl(r[ix["crisis"]]) > 1e-9: inc("idn_crisis_pos")
        for (nat, k), yr in first.items():
            add("first_" + k, yr)
        # monlaw（全ての国・毎四半期）：秘密投票の枝
        for ix, r in rows(os.path.join(d, f"monlaw_s{s}.tsv")):
            suf = fl(r[ix["Suffrage"]]); pc = int(fl(r[ix["polityCode"]]))
            inc("ml_rows")
            if not (suf > 0):
                continue
            inc("ml_suf")
            if pc in (3, 4):
                if fl(r[ix["SecretBallot"]]) > 0.5: inc("br_parl_secret")
                else: inc("br_parl_open")
            else:
                inc("br_nonparl")
        # crownpc（全ての国・毎四半期）：動員と便乗の勢い
        prev = {}
        for ix, r in rows(os.path.join(d, f"crownpc_s{s}.tsv")):
            qt = int(r[ix["quarter"]]); nat = r[ix["nation"]]
            mob, co = fl(r[ix["mobilizationIndex"]]), fl(r[ix["effectiveCoerceIndex"]])
            inc("cp_rows")
            if mob > 1e-9: inc("mob_pos")
            if nat in prev and prev[nat][0] == qt - 1:
                m0, c0 = prev[nat][1], prev[nat][2]
                if m0 == m0 and c0 == c0:
                    mom = min(1.0, max(0.0, m0 / max(1.0, c0) * 0.45))
                    add("momentum", mom); inc("mom_rows")
                    if mom > 0.01: inc("mom_gt001")
                    if mom > 0.1: inc("mom_gt01")
                    if mom >= 1.0: inc("mom_sat")
            prev[nat] = (qt, mob, co)
        # reform（9 国は毎四半期）＋ momentum の写しで便乗の寄与（近似）
        # crownpc を国×四半期で持ち直すのは重いので、年末の行だけ再計算
        mom_ye = {}
        prev = {}
        for ix, r in rows(os.path.join(d, f"crownpc_s{s}.tsv")):
            qt = int(r[ix["quarter"]]); nat = r[ix["nation"]]
            mob, co = fl(r[ix["mobilizationIndex"]]), fl(r[ix["effectiveCoerceIndex"]])
            if qt % 4 == 3 and nat in prev and prev[nat][0] == qt - 1:
                mom_ye[(qt, nat)] = min(1.0, max(0.0, prev[nat][1] / max(1.0, prev[nat][2]) * 0.45))
            prev[nat] = (qt, mob, co)
        for ix, r in rows(os.path.join(d, f"reform_s{s}.tsv")):
            qt = int(r[ix["quarter"]]); nat = r[ix["nation"]]
            if qt % 4 != 3 or (qt, nat) not in mom_ye:
                continue
            lit = fl(r[ix["literacyShare"]])
            if lit == lit:
                add("bw_term", 0.32 * mom_ye[(qt, nat)] * (1 - lit)); add("lit", lit)
        # prds（全ての国・毎四半期）：組合の法
        for ix, r in rows(os.path.join(d, f"prds_s{s}.tsv")):
            u = r[ix["unionLaw"]]
            inc("pr_rows"); inc("union_" + u)
        # disc：思想の行の伝わりの項
        for ix, r in rows(os.path.join(d, f"disc_s{s}.tsv")):
            if r[ix["kind"]] != "E" or r[ix["row"]] not in ("社会主義", "共産主義", "帝国主義", "ファシズム"):
                continue
            t, n_, tr = fl(r[ix["techTerm"]]), fl(r[ix["needTerm"]]), fl(r[ix["transmitTerm"]])
            inc("de_rows"); inc("de_" + r[ix["row"]])
            if tr > 1e-9: inc("de_tr_pos")
            tot = t + n_ + tr
            if tot > 1e-9: add("de_tr_frac", tr / tot); add("de_need_frac", n_ / tot); add("de_tech_frac", t / tot)
    res[arm] = (R, cnt)

with open(out, "w", encoding="utf-8", newline="\n") as o:
    def line(k, f):
        o.write(k + "\t" + "\t".join(f(res[a][0], res[a][1]) for a, _, _ in ARMS) + "\n")
    o.write("item\t" + "\t".join(a for a, _, _ in ARMS) + "\n")
    C = lambda key, den: (lambda R, c: share(c.get(key, 0), c.get(den, 0)))
    Q = lambda key, p: (lambda R, c: f"{q([x for x in R.get(key, []) if x == x], p):.4g}")
    line("rows_dcont_yearend", lambda R, c: str(c.get("dc_rows", 0)))
    for col in ("milVoiceMeanShare", "milWarMeanShare", "milNationMeanShare", "milFamineMeanShare", "discontentMeanShare", "livingDropMeanIndex", "dGtMShare", "militancyMeanShare", "satMeanShare"):
        line("pos_" + col, C("pos_" + col, "dc_rows"))
        line("p50_" + col, Q(col, 0.5)); line("p90_" + col, Q(col, 0.9))
    for col in ("milVoiceMeanShare", "milWarMeanShare", "milNationMeanShare", "milFamineMeanShare"):
        line("fracOfSum_p50_" + col, Q("frac_" + col, 0.5))
    line("partNew_pos", C("partNew_pos", "dc_rows"))
    line("partNewOverOld_p50", Q("partNewOverOld", 0.5)); line("partNewOverOld_p90", Q("partNewOverOld", 0.9))
    line("mil_mean_gt05", C("mil_gt05", "dc_rows"))
    line("parl_sat_lt043", C("parl_sat_lt043", "parl_rows"))
    line("defeatShock_pos_yearend", C("defeat_pos", "sc_rows"))
    line("externalPressure_pos_yearend", C("ep_pos", "sc_rows"))
    line("externalPressure_pos_allrows", C("ep_nonzero_any", "ep_rows_any"))
    line("externalPressure_pos_maxQuarter", lambda R, c: str(max(R.get("ep_nonzero_q", [-1]))))
    line("dynMandate_moved", C("dm_moved", "sc_rows")); line("dynMandate_p10", Q("dm", 0.1)); line("dynMandate_p90", Q("dm", 0.9))
    for k in IDEO:
        line(f"ideo_{k}_gt001", C("idpos_" + k, "id_rows")); line(f"ideo_{k}_gt0001", C("idpos3_" + k, "id_rows"))
        line(f"ideo_{k}_p50", Q("id_" + k, 0.5)); line(f"ideo_{k}_p90", Q("id_" + k, 0.9))
    for k in ("knownSoc", "knownCom", "knownImp", "knownFas"):
        line("idn_" + k, C("idn_" + k, "idn_rows"))
        line("firstYear_p50_" + k, Q("first_" + k, 0.5)); line("firstYear_p10_" + k, Q("first_" + k, 0.1))
        line("nationsOpened_" + k, lambda R, c, k=k: str(len(R.get("first_" + k, []))))
    line("idn_crisis_pos", C("idn_crisis_pos", "idn_rows"))
    line("secret_branch_parl_secret", C("br_parl_secret", "ml_suf")); line("secret_branch_parl_open", C("br_parl_open", "ml_suf")); line("secret_branch_nonparl", C("br_nonparl", "ml_suf"))
    line("suffrage_pos_rows", C("ml_suf", "ml_rows"))
    line("mobilization_pos", C("mob_pos", "cp_rows"))
    line("momentum_gt001", C("mom_gt001", "mom_rows")); line("momentum_gt01", C("mom_gt01", "mom_rows")); line("momentum_saturated", C("mom_sat", "mom_rows"))
    line("momentum_p50", Q("momentum", 0.5)); line("momentum_p90", Q("momentum", 0.9))
    line("bandwagonTerm_p50_9nations", Q("bw_term", 0.5)); line("bandwagonTerm_p90_9nations", Q("bw_term", 0.9)); line("literacy_p50_9nations", Q("lit", 0.5))
    for u in ("0", "1", "2"):
        line("unionLaw_" + u, C("union_" + u, "pr_rows"))
    line("discIdea_rows", lambda R, c: str(c.get("de_rows", 0)))
    for k in ("社会主義", "共産主義", "帝国主義", "ファシズム"):
        line("discIdea_" + k, lambda R, c, k=k: str(c.get("de_" + k, 0)))
    line("discIdea_transmit_pos", C("de_tr_pos", "de_rows"))
    line("discIdea_transmitFrac_p50", Q("de_tr_frac", 0.5)); line("discIdea_needFrac_p50", Q("de_need_frac", 0.5)); line("discIdea_techFrac_p50", Q("de_tech_frac", 0.5))
print("ok")
