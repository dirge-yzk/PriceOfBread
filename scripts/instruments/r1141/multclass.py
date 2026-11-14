# (1141) 台帳 S25 の行の錨の係数名が、倍率の一掃の分類の文書のどの節・どの区分（A／B／I ほか）に出るかを引く（読むだけ）
import sys, re
DOC, ROWS = sys.argv[1], sys.argv[2]
sys.stdout.reconfigure(newline='\n')
L = open(DOC, encoding='utf-8-sig').read().split('\n')
sec = ''; where = {}
for i, l in enumerate(L):
    if l.startswith('#'): sec = l.strip('# ').strip()[:24]
    m = re.match(r'^\s*-\s*([A-Z甲乙丙I]{1,2})（\d+）[：:](.*)$', l)
    names = re.findall(r'[A-Z][A-Za-z0-9]+', l)
    for n in names:
        tag = f'{sec}/{m.group(1)}' if m else f'{sec}/L{i+1}'
        where.setdefault(n, []).append(tag)
for row in open(ROWS, encoding='utf-8'):
    c = [x.strip() for x in row.strip().strip('|').split('|')]
    rid, anc = c[0], c[4]
    out = []
    for a in anc.split(';'):
        m = re.match(r'[^:]+:\d+:(?:c\.)?([A-Za-z_0-9.]+)', a.strip())
        if not m: continue
        n = m.group(1).split('.')[-1]
        if n in where: out.append(n + '=' + ','.join(sorted(set(where[n]))[:3]))
    print(rid + '\t' + (' ; '.join(out) if out else '—'))
