"""(192)＝(188) の写し（表 H の 1939 年以後を直した）。(188) 平時の徴兵法の史実の段との一致（読むだけ）＝an_mil182 の ① の表（10年の窓の志願/選抜/皆兵の割合）から、窓ごとに史実の段の割合を拾い、窓の平均を国ごとに。
python an_lawmatch188.py <ラベル>=<① を含むテキスト>[:<節の名>] ...   史実の段＝窓の始めの年で決める（【推】は表 H の注記）。"""
import sys, re
H = {  # 国: [(この年から, 段 0志願/1選抜/2皆兵)]  出典＝an_mil182 の UNIV の錨・(178) §2 の開幕の制度【推】
 'イギリス': [(1783, 0), (1939, 2)], 'アメリカ': [(1783, 0), (1940, 1)],   # ★(192) 反証役 vm2#10：英 1939 軍事訓練法→1947 国民役務・米 1940 選抜訓練法→1948 選抜徴兵法【推】
 'フランス': [(1783, 1), (1798, 2), (1818, 1), (1872, 2)],   # 1798 ジュルダン法・1818 グヴィオン＝サン＝シール法（籤）・1872
 'プロイセン': [(1783, 1), (1814, 2)], 'ロシア': [(1783, 1), (1874, 2)], '日本': [(1783, 0), (1873, 2)],
 '清': [(1783, 0), (1933, 2)], 'オーストリア': [(1783, 1), (1868, 2)], 'スペイン': [(1783, 1), (1912, 2)],
 'オスマン': [(1783, 0), (1843, 1)],
}
def hist(n, y):
    lv = None
    for y0, l in H[n]:
        if y >= y0: lv = l
    return lv
out = {}
for arg in sys.argv[1:]:
    lab, spec = arg.split('=', 1)
    path, sec = (spec.rsplit(':', 1) if spec.count(':') > 1 else (spec, None))
    t = open(path, encoding='utf-8').read()
    if sec:
        m = re.search(r'^### ' + re.escape(sec) + r'\n```\n(.*?)\n```', t, re.S | re.M); t = m.group(1)
    i = t.index('## ① 平時の徴兵法の割合'); j = t.index('## ②', i); blk = t[i:j]
    lines = blk.split('\n'); yrs = None; res = {}
    for ln in lines:
        if re.match(r'^\s+1784\s', ln): yrs = [int(x) for x in ln.split()]; continue
        m = re.match(r'^\s+(\S+):\s+(.*)$', ln)
        if not m or yrs is None or m.group(1) not in H: continue
        cells = re.findall(r'(\d+)/\s*(\d+)/\s*(\d+)', m.group(2))
        vals = []
        for y, c in zip(yrs, cells):
            v = [int(x) for x in c]
            if sum(v) == 0: continue
            vals.append(v[hist(m.group(1), y)])
        if m.group(1) not in res and vals: res[m.group(1)] = sum(vals) / len(vals)
    out[lab] = res
labs = list(out)
print('# 平時の徴兵法が史実の段と一致した割合%（17 の10年の窓の平均・史実の段は表 H）')
print('国 | ' + ' | '.join(labs))
for n in H:
    print(n + ' | ' + ' | '.join(f"{out[l].get(n, float('nan')):.0f}" for l in labs))
print('10か国の平均 | ' + ' | '.join(f"{sum(out[l].values()) / len(out[l]):.1f}" for l in labs))
