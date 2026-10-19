import sys, shutil
M = sys.argv[1]
base = 'D:/PoB_work/wt_md/PriceOfBread.SimCore/'
bak = 'D:/PoB_work/md_446/bak2/'


def edit(f, old, new, extra=None):
    b = open(base + f, 'rb').read().decode('utf-8')
    assert b.count(old) == 1, (f, old, b.count(old))
    b = b.replace(old, new)
    open(base + f, 'wb').write(b.encode('utf-8'))


if M == 'restore':
    for f in ('EconomyStep.cs', 'Simulation.cs'):
        shutil.copy(bak + f, base + f)
else:
    for f in ('EconomyStep.cs', 'Simulation.cs'):
        shutil.copy(bak + f, base + f)   # 毎回、元から作る（前の突然変異を持ち越さない）
    if M == 'M1':
        edit('EconomyStep.cs', 'if (ownMed && medGood is not null) { if (medHeld) { NecessityRelease(region, scratch.MedHeld); medHeld = false; } medGood = null; }', 'if (ownMed && medGood is not null) { medGood = null; }')
    elif M == 'M1b':
        edit('EconomyStep.cs', 'if (ownMed && medGood is not null) { if (medHeld) { NecessityRelease(region, scratch.MedHeld); medHeld = false; } medGood = null; }', 'if (ownMed && medGood is not null) { medGood = null; }')
        edit('EconomyStep.cs', '        if (medHeld) NecessityRelease(region, scratch.MedHeld);   // ★(446) 保険', '        if (false) NecessityRelease(region, scratch.MedHeld);   // ★(446) 保険')
    elif M == 'M2':
        edit('EconomyStep.cs', '            double n = pops[i].Literacy;\n            n = n > 0 ? (n < 1 ? n : 1) : 0;', '            double n = 1;')
    elif M == 'M3':
        edit('EconomyStep.cs', '                    if (!staple && !foodNonStaple)\n                    {\n                        if (medGood is not null && !medHeld)', '                    if (true)\n                    {\n                        if (medGood is not null && !medHeld)')
    elif M == 'M4':
        edit('Simulation.cs', 'double healthcare = c.MedicalNeedByLiteracy > 0 ', 'double healthcare = true ')
    elif M == 'M5':
        edit('EconomyStep.cs', 'double h = r < avail ? r : avail;', 'double h = r;')
    elif M == 'M6':
        edit('EconomyStep.cs', 'else if (coef.EducationNeedByLiteracy > 0 && consumeOrder[mi].Id == Catalog.EducationId)', 'else if (false && consumeOrder[mi].Id == Catalog.EducationId)')
    elif M == 'M7':   # 教育の取り置きを医療の番まで持ち越さず、教育の番で戻さない（教育の取り置きが医療の買い物を削る）
        edit('EconomyStep.cs', 'if (ownEdu && eduGood is not null) { if (eduHeld) { NecessityRelease(region, scratch.EduHeld); eduHeld = false; } eduGood = null; }', 'if (ownEdu && eduGood is not null) { eduGood = null; }')
