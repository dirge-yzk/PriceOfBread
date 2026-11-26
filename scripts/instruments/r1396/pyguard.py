# (1396) c148dm：GoodIdGuardTests の Python の規則（PyRules・870ce5520 の :529〜:541 の写し）をこのフォルダの .py に当てる（写す前の走査の番人の代わり）
import os, re, sys
R = [r"\b\w*(?:GoodId|[Gg]ood|[Gg]id)\w*\s*(?:==|!=)\s*['\"]?\d+", r"\b(?:g|id|gi|g2)\s*(?:==|!=)\s*['\"]?\d+",
     r"\b(?:Goods|Markets|RefPrices|Complexity|goods|markets|prices|stocks|ref_prices)\s*\[\s*\d+\s*\]", r"\bI\(\s*\d+\s*,", r"\bnew Input\(\s*\d+\s*,",
     r"\b(?:good|gid|good_id)\w*\s*=\s*\d+", r"\b(?:good|gid)\w*\s+in\s+[\(\[\{]\s*\d+", r"\bnew\s*(?:int)?\s*\[\s*\]\s*\{\s*\d+", r"\bGOODS?\w*\s*=\s*[\{\[\(]"]
d = os.path.dirname(os.path.abspath(__file__)); n = 0
for f in sorted(os.listdir(d)):
    if not f.endswith('.py') or f == os.path.basename(__file__): continue
    t = open(os.path.join(d, f), encoding='utf-8').read()
    for rx in R:
        for m in re.finditer(rx, t): n += 1; print('当たり', f, t.count('\n', 0, m.start()) + 1, m.group(0))
print('当たり', n, '件')
