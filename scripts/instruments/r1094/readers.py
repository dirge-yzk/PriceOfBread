# (1199) 注：(1193) で欄（国民意識）が消えた・旧の生でだけ動く（中身は変えていない）
# (1094) S14 の名ごとの読み手の数（コードの行と注の行を分ける・範囲＝SimCore/App/Runner/Tests/XAML/scripts/measure.sh/measurements の .py/.sh/docs）
# 使い方: python -X utf8 -I readers.py <repo> <out>
import os, re, sys
repo, out = sys.argv[1], sys.argv[2]
NAMES = """ConsciousnessSpeed NewsConsciousnessFloor UrbanConsciousnessBoost ConsciousnessFromSchoolingStock BandwagonSensitivity BandwagonWeight
MilitancySpeed PoliticalDistanceWeight WantWeight WarWearinessMilitancy ParticipationDiscontent DiscontentLawGap LivingRefSpeed
OpenBallotReality PaperFranchiseReality MinorityMilitancy NoblePrivilegeWeight ReactionFromLoss DefeatNationalism ExternalPressureNationalism
LostLandNationalism VictoryImperialism VictoryNationalism DynMandateFoodAnchor FamineRepublicanism FamineSocialism FamineMemoryKeepPerQuarter
DiscoveryContactHalf FascismCrisisLeftRef FascismCrisisSlumpRef FascismGate ManifestoCreditDecay ManifestoFulfillCredit IdeologyAffinitySlope
IdeologyVoiceFloor PolityCoordDriftWeight ReformPressureBoost UnionMobilization DesperationChallengeWeight DesperationConsciousnessGate
RiotMilitancyThreshold FamineDriveGain FamineDropOnset ExternalPressureInlandFactor BourgeoisCoupWeight DominanceInstabilitySatisfaction
PettyInterestGate PettyInterest NationalismFromConsciousness DepressionSocialism DepressionCommunism DepressionReaction DesperationMilitancyOverride DesperationLayerBlend
UnemploymentMilitancy UnemploymentInsuranceRelief GiniMilitancy FamineAbsoluteMilitancy MiddleLayerMilitancy
ExternalPressure FamineMemory VictoryShock DefeatShock LostRegionsCumulative ManifestoIdeology ManifestoCredit MeanSatisfactionPol
ReformistPower RadicalPower IdeologyShares Consciousness LivingRef FoodRef MilVoice MilWar MilNation MilFamine
UpdateShares EraGates FascismCrisis BaseWeights IdealPoint SupportAt DefectionShare AffinityOf DriftedPolityCoord MeasureMobilization
LivingWant FamineDrive EffectiveVoice Discontent DiscontentRaw AvgMassDiscontent AvgMassMilitancy GiniIndex MandateBase UnionRepression UnionLegality
PrivilegeIntact StatePatronage""".split()
AREAS = [("SimCore", "PriceOfBread.SimCore"), ("App", "PriceOfBread.App"), ("Runner", "PriceOfBread.Runner"), ("Tests", "PriceOfBread.Tests"),
         ("scripts", "scripts"), ("meas", "measurements"), ("docs", "docs")]
EXT = {"SimCore": (".cs",), "App": (".cs", ".axaml", ".xaml"), "Runner": (".cs",), "Tests": (".cs",),
       "scripts": (".py", ".sh"), "meas": (".py", ".sh"), "docs": (".md",)}
pats = {n: re.compile(r"(?<![A-Za-z0-9_])" + re.escape(n) + r"(?![A-Za-z0-9_])") for n in NAMES}
cnt = {n: {a: [0, 0, set()] for a, _ in AREAS} for n in NAMES}  # code, comment, files
def scan(area, path):
    try:
        data = open(path, "rb").read().decode("utf-8", "replace")
    except Exception:
        return
    cm = "#" if path.endswith((".py", ".sh")) else "//"
    for line in data.split("\n"):
        for n, p in pats.items():
            m = p.search(line)
            if not m:
                continue
            c = cnt[n][area]
            if area == "docs":
                c[1] += 1
            else:
                k = line.find(cm)
                if k >= 0 and k < m.start():
                    c[1] += 1
                else:
                    c[0] += 1
            c[2].add(os.path.relpath(path, repo).replace("\\", "/"))
for area, sub in AREAS:
    root = os.path.join(repo, sub)
    for dp, dn, fn in os.walk(root):
        dn[:] = [d for d in dn if d not in ("bin", "obj", ".git")]
        for f in fn:
            if f.endswith(EXT[area]):
                scan(area, os.path.join(dp, f))
mp = os.path.join(repo, "measure.sh")
if os.path.exists(mp):
    scan("scripts", mp)
with open(out, "w", encoding="utf-8", newline="\n") as o:
    o.write("name\t" + "\t".join(f"{a}_code\t{a}_cmt" for a, _ in AREAS) + "\tsimcore_files\n")
    for n in NAMES:
        row = [n]
        for a, _ in AREAS:
            row += [str(cnt[n][a][0]), str(cnt[n][a][1])]
        row.append(",".join(sorted(os.path.basename(x) for x in cnt[n]["SimCore"][2]))[:300])
        o.write("\t".join(row) + "\n")
print("ok", len(NAMES))
