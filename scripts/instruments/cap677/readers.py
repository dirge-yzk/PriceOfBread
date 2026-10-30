import re,os,sys
root=sys.argv[1]
W=re.compile(r"\b(TotalIndustrialization|ProductiveIndustrialization|MeanIndustrialization|IndustrialFrontier|UrbanIndSmoothed|OldVintageCapital|StrandedVintage|IndustryInvestRegionCap|avgInd|indPopSum|indPopWeight)\b|\.Industrialization\b|\bIndustrialization\s*=|\bcfg\.Industrialization\b|\bc\.Industrialization\b")
X=re.compile(r"\b(InvestmentReturn|ReturnShare|ModernizeReturnShare|FactoryEquipment|UrbanShareCap|UrbanCapOff|CapitalHeadroomFromReturn|Mechanization|DevelopmentPeak|CumPrivateIndustrialization|IndInvestmentFrac|IndPerConstruction|IndCapitalOutputYears|IndInvestmentSpeed|IndDepreciation|PrivateAbsorptionBlend|JointStockInvestBonus|IndCapitalCost|IndTechLeverage|IndProductivityGain|IndProductivityScale|PrivateIndPerConstruction|RecipeEquipment|IndustrializationOff)\b")
d=os.path.join(root,"PriceOfBread.SimCore")
for dp,_,fs in os.walk(d):
  for f in sorted(fs):
    if not f.endswith(".cs"):continue
    p=os.path.join(dp,f)
    for i,l in enumerate(open(p,encoding='utf-8-sig'),1):
      c=l.split("//",1)[0]
      tag=("W" if W.search(c) else "")+("X" if X.search(c) else "")
      if tag: print(f"{tag}\t{os.path.relpath(p,d)}:{i}\t{c.strip()[:230]}")
