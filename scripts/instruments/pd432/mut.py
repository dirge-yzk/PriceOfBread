import subprocess,sys,os
os.environ['PYTHONUTF8']='1'
REPO='D:/PoB_work/wt_pd'
muts=[
 ('M1 栄養の掛け目を州に掛けない','PriceOfBread.SimCore/PopulationDynamics.cs','if (board) nutR[ri0] = NutritionMul(c, r.FoodFill);','if (board) nutR[ri0] = 1.0;'),
 ('M2 不足の無い州にも負の不足を許す','PriceOfBread.SimCore/PopulationDynamics.cs','return !(deficit > 0) ? 1.0 : 1.0 + c.PopNutritionExcess * deficit;','return 1.0 + c.PopNutritionExcess * deficit;'),
 ('M3 ηの分子から1を外す','PriceOfBread.SimCore/Discovery.cs','return (1.0 + sum) / (1.0 + tot);','return sum / (1.0 + tot);'),
 ('M4 ηを常に1にする','PriceOfBread.SimCore/PopulationDynamics.cs','double eta = board ? Discovery.KnowledgeEfficacy(w.Discovered) : 1.0;','double eta = 1.0;'),
 ('M5 州の栄養を死の率へ掛けない','PriceOfBread.SimCore/PopulationDynamics.cs','rate *= nutr;','rate *= 1.0;'),
 ('M6 旗1でも pc の水準を使う','PriceOfBread.SimCore/PopulationDynamics.cs','double level = board ? c.PopDeathLevelBoard : c.PopDeathLevel;','double level = c.PopDeathLevel;'),
 ('M7 旗0の開幕解禁を外す','PriceOfBread.SimCore/Discovery.cs','if (!(w.Coef.PopDeathFromBoard > 0)) foreach (var kr in KnowledgeRows) if (kr >= 0) w.Discovered[kr] = true;','/* removed */'),
 ('M8 知識の行の放射を外す','PriceOfBread.SimCore/Discovery.cs','|| row == BirthLimitRow || IsKnowledgeRow(row)) return','|| row == BirthLimitRow) return'),
 ('M9 知識の必要性を植民地の割合で読む','PriceOfBread.SimCore/Discovery.cs','else if (IsKnowledgeRow(row)) s = w.UrbanShare;','else if (IsKnowledgeRow(row)) s = colonialShare;'),
 ('M10 衛生の傾きを pc の係数のまま','PriceOfBread.SimCore/PopulationDynamics.cs','(board ? c.PopSanitationExcess : c.SanitationCapacityWeight)','c.SanitationCapacityWeight'),
 ('M12 栄養の不足を飢えの門で頭打ちにしない','PriceOfBread.SimCore/PopulationDynamics.cs','double deficit = 1.0 - Math.Max(regionFoodFill, c.FamineDeathFillFloor);','double deficit = 1.0 - regionFoodFill;'),
 ('M13 栄養の頭打ちの門を別の値に固定する','PriceOfBread.SimCore/PopulationDynamics.cs','Math.Max(regionFoodFill, c.FamineDeathFillFloor)','Math.Max(regionFoodFill, 0.9)'),
 ('M11 医療の項に pc の式を使う','PriceOfBread.SimCore/PopulationDynamics.cs','double healthTerm = board ? c.PopMedEfficacy * eta * doctorCare :','double healthTerm = board ? c.PopMedEfficacy * doctorCare :'),
]
sel=sys.argv[1:]
out=[]
for name,f,old,new in muts:
    if sel and name.split()[0] not in sel: continue
    p=os.path.join(REPO,f); b=open(p,'rb').read().decode('utf-8')
    if b.count(old)!=1: out.append((name,'NOTFOUND')); print(name,'NOTFOUND',flush=True); continue
    open(p,'wb').write(b.replace(old,new).encode('utf-8'))
    try:
        r=subprocess.run(['dotnet','build','PriceOfBread.Tests/PriceOfBread.Tests.csproj','-c','Release','--artifacts-path','D:/PoB_work/pd_432b/art_mut','-v','q'],cwd=REPO,capture_output=True,text=True,encoding='utf-8',errors='replace')
        if ' error ' in r.stdout: res='BUILDFAIL'
        else:
            t=subprocess.run(['dotnet','test','PriceOfBread.Tests/PriceOfBread.Tests.csproj','-c','Release','--artifacts-path','D:/PoB_work/pd_432b/art_mut','--no-build','--filter','FullyQualifiedName~PopulationDeathBoardTests|FullyQualifiedName~PopulationDynamicsTests|FullyQualifiedName~DiscoveryScoreTests|FullyQualifiedName~DiscoveryTierTests'],cwd=REPO,capture_output=True,text=True,encoding='utf-8',errors='replace')
            tail=[l for l in t.stdout.splitlines() if '失敗' in l or '成功!' in l]
            res='RED' if any('失敗!' in l or l.strip().startswith('失敗 ') for l in tail) else 'GREEN'
            res+=' '+' | '.join(l.strip()[:90] for l in tail[:3])
    finally:
        open(p,'wb').write(b.encode('utf-8'))
    out.append((name,res)); print(name,res,flush=True)
