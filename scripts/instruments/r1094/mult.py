# (1094) 倍率の一掃の取りまとめで S14 の係数がどの節・区分に載るか
# 使い方: python -X utf8 -I mult.py <md> <out>
import re, sys
md, out = sys.argv[1], sys.argv[2]
NAMES = """ConsciousnessSpeed NewsConsciousnessFloor UrbanConsciousnessBoost ConsciousnessFromSchoolingStock BandwagonSensitivity BandwagonWeight
MilitancySpeed PoliticalDistanceWeight WantWeight WarWearinessMilitancy ParticipationDiscontent DiscontentLawGap LivingRefSpeed
OpenBallotReality PaperFranchiseReality MinorityMilitancy NoblePrivilegeWeight ReactionFromLoss DefeatNationalism ExternalPressureNationalism
LostLandNationalism VictoryImperialism VictoryNationalism DynMandateFoodAnchor FamineRepublicanism FamineSocialism FamineMemoryKeepPerQuarter
DiscoveryContactHalf FascismCrisisLeftRef FascismCrisisSlumpRef FascismGate ManifestoCreditDecay ManifestoFulfillCredit IdeologyAffinitySlope
IdeologyVoiceFloor PolityCoordDriftWeight ReformPressureBoost UnionMobilization DesperationChallengeWeight DesperationConsciousnessGate
RiotMilitancyThreshold FamineDriveGain FamineDropOnset ExternalPressureInlandFactor BourgeoisCoupWeight DominanceInstabilitySatisfaction""".split()
lines = open(md, "rb").read().decode("utf-8").split("\n")
sec = ""
hits = {n: [] for n in NAMES}
for i, l in enumerate(lines, 1):
    if l.startswith("#"):
        sec = l.strip("# ").strip()[:40]
        continue
    for n in NAMES:
        if re.search(r"(?<![A-Za-z0-9_])" + n + r"(?![A-Za-z0-9_])", l):
            m = re.match(r"\s*-\s*([ABCTI])（", l)
            t = re.match(r"\|\s*[^|]*\|\s*([^|]{1,30})\|", l)
            cls = m.group(1) if m else (t.group(1).strip() if t else "?")
            hits[n].append(f"L{i}[{sec}]{cls}")
with open(out, "w", encoding="utf-8", newline="\n") as o:
    for n in NAMES:
        o.write(n + "\t" + ("; ".join(hits[n]) if hits[n] else "（載らない）") + "\n")
print("ok")
