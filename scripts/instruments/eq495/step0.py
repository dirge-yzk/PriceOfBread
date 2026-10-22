# (495) 線 eq 段0：Producer.Scale／BuiltScale と工業化度（ヴィンテージ込み）の書き手・読み手の全数表の生成器。
# 使い方：PYTHONUTF8=1 python scripts/instruments/eq495/step0.py [rev] > measurements/eq495_20261023_step0.txt
# 基点の rev（既定 8c8f987a）の木を git grep で読む（作業ツリーの編集に影響されない）。シミュは回さない。
import re, subprocess, sys, collections
rev = sys.argv[1] if len(sys.argv) > 1 else "8c8f987a"
def grep(pat, paths):
    r = subprocess.run(["git", "grep", "-n", "-I", "-E", pat, rev, "--"] + paths, capture_output=True, text=True, encoding="utf-8")
    out = []
    for ln in r.stdout.splitlines():
        # rev:path:line:text
        m = re.match(r"^[^:]+:([^:]+):(\d+):(.*)$", ln)
        if m: out.append((m.group(1), int(m.group(2)), m.group(3).strip()))
    return out
SIM = ["PriceOfBread.SimCore/*.cs"]; APP = ["PriceOfBread.App/*.cs", "PriceOfBread.App/**/*.cs"]; RUN = ["PriceOfBread.Runner/*.cs"]; TST = ["PriceOfBread.Tests/*.cs"]
def is_comment(t): return t.startswith("//") or t.startswith("///") or t.startswith("*")
def short(p): return p.replace("PriceOfBread.", "")
def cut(t, n=170): return t if len(t) <= n else t[:n] + "…"

# ---- (A) Scale と BuiltScale の書き手 ----
writer_pat = r"(\.|\b)(Scale|BuiltScale)\s*([-+*/]?=)[^=]|\bScale\s*=\s*[0-9]"
def code_of(t): return t.split("//")[0]
W = [x for x in grep(r"Scale\s*([-+*/]?=)", SIM + APP + RUN + TST) if not is_comment(x[2])
     and re.search(r"(\.|\b)(Scale|BuiltScale)\s*([-+*/]?=)[^=]", code_of(x[2]))
     and not re.search(r"(Military|mint|Mint)Scale|Coefficients\.cs", code_of(x[2]) + x[0])
     and "Scale = 0.05, " not in x[2] and not re.search(r"public double \w*Scale", code_of(x[2]))]
# 旗 1 での分類（手で付けた主な行・番号は基点 8c8f987a の行）
CLS = {
 ("PriceOfBread.SimCore/EconomyStep.cs", "s.Scale += catSpeed"): "旗1・適格＝使わない（束の購入・減価・完成・廃棄だけが変える）／旗0＝今のまま",
 ("PriceOfBread.SimCore/EconomyStep.cs", "s.Scale = 0;"): "門が0（未発見・退場）＝設備が無い／廃棄＝旗1でも今のまま",
 ("PriceOfBread.SimCore/EconomyStep.cs", "s.Scale = coef.FactoryReentryScale"): "旗1・適格＝Scale は据え置き（再参入で設備を作り直さない）",
 ("PriceOfBread.SimCore/EconomyStep.cs", "s.Bankrupt = true; s.Scale = 0"): "旗1・適格＝倒産しても Scale を 0 にしない（設備は残る・労働配分の重みだけ 0）",
 ("PriceOfBread.SimCore/Discovery.cs", "p.Scale = 1.0"): "産地（鉱床）の発見＝RGO は適格でない＝今のまま",
 ("PriceOfBread.SimCore/Simulation.cs", "ep.Scale = 0"): "教区学校＝役務は適格でない＝今のまま",
 ("PriceOfBread.SimCore/Simulation.cs", "pub.Scale = 0"): "公立学校＝役務は適格でない＝今のまま",
 ("PriceOfBread.SimCore/Simulation.cs", "lpr.Scale = Math.Max"): "家畜の徴発＝RGO は適格でない＝今のまま",
 ("PriceOfBread.SimCore/OpeningBake.cs", "p.Scale = row[1]"): "開幕の焼き込みの読み込み（旗 OpeningBakeEnabled 既定0）＝今のまま",
 ("PriceOfBread.SimCore/World.cs", "p.BuiltScale += Coef.FactoryBuildScale"): "旗1・適格＝Scale にも FactoryBuildScale を足す（国営の設備も買った分だけ）",
 ("PriceOfBread.SimCore/World.cs", "p.BuiltScale += take"): "接収＝持分の付け替え（Scale 不変）＝今のまま",
 ("PriceOfBread.SimCore/World.cs", "p.BuiltScale = Math.Max(0, p.BuiltScale - Coef.FactoryBuildScale)"): "払い下げ＝持分の付け替え（Scale 不変）＝今のまま",
 ("PriceOfBread.SimCore/World.cs", "p.BuiltScale = 0;"): "払い下げ＝持分の付け替え＝今のまま",
 ("PriceOfBread.SimCore/Simulation.cs", "fp.BuiltScale *= scrapKeep"): "旗1・適格＝同じ個体の Scale にも同じ割合を掛ける（持分が動かない）",
}
def cls_of(path, text):
    for (p, k), v in CLS.items():
        if p == path and k in text: return v
    if "public double Scale = 1.0" in text or "Scale = isFactory" in text or "Output = 30, Scale = 1.0" in text: return "構築時の初期値（生産者の初期化子＝設備の開幕の置き場。工場 0.05・職人と自家農家 1.0・旗1の助走は旧の規則で自由に伸びる）"
    if "ApplyEndowment(" in text: return "賦存の適用（産地の RGO の面積＝適格でない）＝今のまま"
    if path.startswith("PriceOfBread.Tests/"): return "試験（設定）"
    if path.startswith("PriceOfBread.Runner/"): return "Runner（設定・診断）"
    return "※手で分類が必要"
print("(495) 線 eq 段0：生産者の操業規模 Scale と BuiltScale、および工業化度の書き手と読み手の全数表（grep・シミュは回していない）")
print(f"基点 {rev}・書いた日 2026-10-23・書いたモデル Claude Sonnet 5.5・生成器 scripts/instruments/eq495/step0.py（git grep・コメント行を除く・分類は主な行を手で付けた辞書＋字面）")
print("探した語：Scale／BuiltScale（Producer の欄）・Industrialization／TotalIndustrialization／ProductiveIndustrialization／OldVintageCapital／StrandedVintage／VintageTechLevel／indMult／MeanIndustrialization。対象：SimCore・App・Runner・Tests の .cs。")
print()
print("== (A) Producer.Scale／BuiltScale の書き手 ==")
cnt = collections.Counter()
for p, n, t in sorted(W):
    c = cls_of(p, t)
    print(f"{short(p)}:{n}\t{c}\t{cut(t)}")
print("件数（層別）：" + "，".join(f"{k.replace('PriceOfBread.','')}={v}" for k, v in collections.Counter(p.split('/')[0] for p, _, _ in W).items()))
# 生産者の構築時の初期値
ini = [x for x in grep(r"Scale = (0\.05|1\.0|isFactory)", ["PriceOfBread.SimCore/Simulation.cs", "PriceOfBread.SimCore/Model.cs"]) if not is_comment(x[2]) and re.search(r"\bScale = (0\.05|1\.0|isFactory)", code_of(x[2]))]
print(f"\n-- 生産者の構築時の初期値（Producer の初期化子）{len(ini)} 行：工場 0.05・自家農家と職人 1.0。旗1の助走（!warmFrozen でない間）は旧の規則で自由に伸びる＝助走の終わりの値が開幕の設備（[0]⑥）")
for p, n, t in sorted(ini)[:6]: print(f"   {short(p)}:{n}\t{cut(t, 120)}")
print("   …（同じ形の行が続く）" if len(ini) > 6 else "")

# ---- (B) Scale の読み手 ----
print("\n== (B) Producer.Scale の読み手（書き手を除く・SimCore と App のみ全行・Runner は件数） ==")
R = [x for x in grep(r"\.Scale\b|\bs\.Scale\b|\bpr\.Scale\b", SIM + APP) if not is_comment(x[2])]
Wset = {(p, n) for p, n, _ in W}
for p, n, t in sorted(R):
    if (p, n) in Wset or "MilitaryScale" in t or "Coef" in t.split(".Scale")[0][-6:]: continue
    print(f"{short(p)}:{n}\t{cut(t)}")
RR = [x for x in grep(r"\.Scale\b", RUN) if not is_comment(x[2])]
print(f"Runner：{len(RR)} 行（診断・報告のみ＝力学に入らない）")
print("読み手の旗1での扱い：EconomyStep:650 prodW＝LaborWeight×Scale（労働配分の重み）が唯一の力学の読み手＝設備が重みになる／World.cs の StateFrac・PrivateUnitsOf・Seize＝BuiltScale÷Scale の持分（Scale と BuiltScale を同じ割合で動かして保つ）／SleepState＝Scale=0 かつ産出0 で眠る／Simulation:1813 と SelfPlay:1295 は記録・AI の読み。")

# ---- (C) 工業化度の書き手・読み手 ----
print("\n== (C) 工業化度（Industrialization・OldVintageCapital・StrandedVintage・VintageTechLevel）の書き手 ==")
cw = r"\b(Industrialization|OldVintageCapital|StrandedVintage|VintageTechLevel)\b\s*([-+*/]?=)[^=]"
CW = [x for x in grep(cw, SIM) if not is_comment(x[2])]
for p, n, t in sorted(CW): print(f"{short(p)}:{n}\t{cut(t)}")
print(f"書き手の行数（SimCore）：{len(CW)}")
print("\n== (C2) 倍率として読む 8 か所（旗 IndustrializationOff の削除対象・事前登録 [3] の 1〜8）：基点の行 ==")
for key, pat, path in [("1 indMult", r"double indMult\b", SIM), ("2 effWageMax", r"effWageMax\s*=", SIM), ("3 Mechanization", r"region\.Mechanization\s*=", SIM),
                       ("4 MonopolyDissolveInd", r"MonopolyDissolveInd", SIM), ("5 Refrigeration", r"RefrigerationIndOnset", SIM),
                       ("6 DevelopmentMemory／Vintage", r"DevelopmentMemory|VintageMigrateRate|VintageObsolescence", SIM),
                       ("7 IndustryInvestRegionCap", r"IndustryInvestRegionCap", SIM), ("8 InvestmentReturn sat", r"double sat = ", SIM)]:
    for p, n, t in sorted(grep(pat, path)):
        if is_comment(t) or "public double" in t: continue
        print(f"{key}\t{short(p)}:{n}\t{cut(t, 140)}")
print("\n== (C3) 在庫として読む 8 か所（触らない）：基点の行 ==")
for key, pat in [("9 都市化の天井", r"UrbanPerInd"), ("10 扶養力", r"IndustrializationPopWeighted|avgInd \+="), ("11 インフラ保守", r"InfraMaintRate \* "), ("12 信用の深さ", r"CreditDevScale"),
                 ("13 外資の吸い寄せ", r"CapitalIndTarget"), ("14 合資会社", r"maxInd"), ("15 疫病の衛生の代理", r"EpidemicSanitationHalf"), ("16 世界最先端", r"IndustrialFrontier")]:
    for p, n, t in sorted(grep(pat, SIM)):
        if is_comment(t) or "public " in t or "const " in t: continue
        print(f"{key}\t{short(p)}:{n}\t{cut(t, 140)}")
print("\n== 層別の件数（全参照・コメントを含む）==")
for lab, paths in [("SimCore", SIM), ("App", APP), ("Runner", RUN), ("Tests", TST)]:
    a = grep(r"Industrialization|OldVintageCapital|StrandedVintage|VintageTechLevel|indMult", paths)
    b = [x for x in a if not is_comment(x[2])]
    print(f"  {lab}\t全参照 {len(a)}\tコメントを除く {len(b)}")
