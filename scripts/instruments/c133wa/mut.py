import subprocess,sys,io
R='D:/PoB_work/wt_c133wa/'
M={
'M1':('PriceOfBread.SimCore/Discovery.cs','&& ReferenceEquals(r.AdvSitesList, list) && r.AdvSitesCount == list.Count',''),
'M2':('PriceOfBread.SimCore/Discovery.cs','for (int s = 0; s < sites.Length; s += 2)','for (int s = 0; s < sites.Length - 2; s += 2)'),
'M3':('PriceOfBread.SimCore/Model.cs','internal void NoteInputsChanged() { if (_siteRegion is { } sr) sr.AdvSitesDirty = true; }','internal void NoteInputsChanged() { }'),
'M4':('PriceOfBread.SimCore/Model.cs','Inputs[i] = v; NoteInputsChanged(); }','Inputs[i] = v; }'),
'M5':('PriceOfBread.SimCore/Model.cs','if (Inputs[i] == v) return;','if (Inputs[i] != v) return;'),
'M6':('PriceOfBread.SimCore/Model.cs','{ if (Inputs[i] == v) return; Inputs[i] = v;','{ Inputs[i] = v;'),
}
for k,(f,o,n) in M.items():
    p=R+f; b=open(p,'rb').read(); s=b.decode('utf-8'); assert s.count(o)==1,(k,o)
    open(p,'wb').write(s.replace(o,n).encode('utf-8'))
    r=subprocess.run(['C:/Program Files/Git/bin/bash.exe','-c','cd /d/PoB_work/wt_c133wa && dotnet test PriceOfBread.Tests -c Release --artifacts-path D:/PoB_work/c133wa_art3 --filter "FullyQualifiedName~WorldAdvLaborSites" 2>&1 | grep -aE "error CS|[0-9]+.{1,3}[0-9]+.{1,3}[0-9]+.{1,3}[0-9]+|^ +[^ ]+ \[FAIL\]" | head -8'],capture_output=True)
    open(p,'wb').write(b)
    print(k,r.stdout.decode('utf-8','replace').strip().replace('\n',' | '),flush=True)
