# (1330) c139sl 突然変異：使い捨ての写し（D:/PoB_work/c139sl_work/mut_<名>）に書き換えを入れ、selftest1330 を回す（赤が期待）。
#   使い方: python -X utf8 -I mut1330.py <D1..D7>
import os, shutil, subprocess, sys
HERE = os.path.dirname(os.path.abspath(__file__))
F = 'gdp_jump1330.py'
M = {
 'D1': [("elif thin(e['rp']): kind = KINDS[1]", "elif False: kind = KINDS[1]")],                # (i) を外す
 'D2': [("elif zero_run(e['win']): kind = KINDS[2]", "elif False: kind = KINDS[2]")],            # (ii) を外す
 'D3': [("kind = KINDS[3] if ok else KINDS[4]", "kind = KINDS[4]")],                             # (iii) を外す
 'D4': [("        run = run + 1 if (g <= 0 and p >= ZPOP) else 0\n", "        run = run + 1 if (g <= 0 and p >= ZPOP) else run\n")],   # 連続を数えだけに替える（G の 0 が窓に散るだけで当たる）
 'D5': [("return (tot >= NEAR and LO <= ra / tot <= HI), tot", "return (tot >= NEAR), tot")],     # (iii) の ok3 を外す（上側の区間）
 'D6': [("            elif zero_run(e['win']): kind = KINDS[2]\n            else:\n                ok, tot = out_cause(reg.get((e['pq'], e['pidx']), {}), reg.get((e['q'], e['cidx']), {}), e['ra']); e['tot'] = tot\n                kind = KINDS[3] if ok else KINDS[4]\n", "            else:\n                ok, tot = out_cause(reg.get((e['pq'], e['pidx']), {}), reg.get((e['q'], e['cidx']), {}), e['ra']); e['tot'] = tot\n                kind = KINDS[3] if ok else (KINDS[2] if zero_run(e['win']) else KINDS[4])\n"),
        ("not thin(e['rp']) and not zero_run(e['win'])}", "not thin(e['rp'])}", 2)],   # (1337) 区分の順：(ii) と (iii) を入れ替える（need の母集合も同じ向きに替える）＝Q（両方当たる）が (iii) になって赤
 'D8': [("return (tot >= NEAR and LO <= ra / tot <= HI), tot", "return (tot >= NEAR and ra / tot <= HI), tot")],   # (iii) の下側の区間を外す
 'D9': [("    if sa <= 0: return False, (math.inf if sb > 0 else float('nan'))", "    if sa <= 0: return sb > 0, math.inf if sb > 0 else float('nan')")],   # (1337) tot=inf の枝を戻す＝P・J が (iii) になって赤
 'D7': [("            if e['age'] <= K: kind = KINDS[0]", "            if e['age'] <= 0: kind = KINDS[0]")],   # 建国の直後の幅を 0 にする
}
n = sys.argv[1]
dst = f'D:/PoB_work/c139sl_work/mut_{n}'
shutil.rmtree(dst, ignore_errors=True); shutil.copytree(HERE, dst)
p = os.path.join(dst, F); s = open(p, 'rb').read().decode('utf-8')
for m in M[n]:
    a, b = m[0], m[1]; cnt = m[2] if len(m) > 2 else 1
    if s.count(a) != cnt: print(n, '置換の元の数が違う', repr(a), s.count(a)); sys.exit(2)
    s = s.replace(a, b)
open(p, 'wb').write(s.encode('utf-8'))
o = subprocess.run([sys.executable, '-X', 'utf8', '-I', os.path.join(HERE, 'selftest1330.py'), '--dir', dst], capture_output=True, encoding='utf-8')
print(o.stdout, end='')
print(f'{n} selftest rc={o.returncode}（赤＝1 が期待）')
