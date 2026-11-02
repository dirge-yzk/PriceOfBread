# r796 読むだけ：Coefficients.cs の欄を全て拾い、休眠の名簿・既定・読み手の数・旗の比べ方を TSV に出す。
# 使い方: python -X utf8 coefscan.py <repo> <out.tsv>
# 限界：字句の出現だけ（到達は見ない）・コメントは // 以降を落とすだけ（文字列の中の // は誤って落とす）・同名の別の欄を区別しない。
import os, re, sys

repo, out = sys.argv[1], sys.argv[2]
coef = os.path.join(repo, 'PriceOfBread.SimCore', 'Coefficients.cs')
dit = os.path.join(repo, 'PriceOfBread.Tests', 'DormantIdentityTests.cs')

def arr(src, name):
    m = re.search(r'string\[\]\s+' + name + r'\s*=\s*\[(.*?)\n\s*\];', src, re.S)
    body = m.group(1)
    names = []
    for line in body.split('\n'):
        code = line.split('//')[0]
        names += re.findall(r'"(\w+)"', code)
    return names

dsrc = open(dit, encoding='utf-8-sig').read()
DZ = arr(dsrc, 'DormantZeroFields'); DO = arr(dsrc, 'DormantOneFields')
ZL = arr(dsrc, 'ZeroDefaultLedger'); RT = arr(dsrc, 'RetiredFields')

fld = re.compile(r'^\s*public\s+(?!sealed\b|static\b|class\b|const\b|readonly\b)([\w<>\[\],?\.]+)\s+(\w+)\s*(?:=\s*(.*?))?;\s*(//.*)?$')
fields = []
lines = open(coef, encoding='utf-8-sig').read().split('\n')
for i, l in enumerate(lines):
    m = fld.match(l)
    if not m or '(' in m.group(2):
        continue
    typ, name, init, cm = m.group(1), m.group(2), (m.group(3) or '').strip(), (m.group(4) or '')
    # 直前のコメント行（最大 2 行）
    pre = []
    j = i - 1
    while j >= 0 and len(pre) < 2 and lines[j].strip().startswith('//'):
        pre.insert(0, lines[j].strip()); j -= 1
    fields.append((i + 1, typ, name, init, cm, ' '.join(pre)))

def strip(src):
    out = []
    for l in src.split('\n'):
        s = l.split('//')[0]
        out.append(s)
    return '\n'.join(out)

groups = {'sim': [], 'ins': [], 'runner': [], 'app': [], 'tests': []}
for root, ds, fs in os.walk(repo):
    if any(x in root for x in ('\\obj', '/obj', '\\bin', '/bin', '.git')):
        continue
    for f in fs:
        if not f.endswith('.cs'):
            continue
        p = os.path.join(root, f); rel = os.path.relpath(p, repo).replace('\\', '/')
        if rel.startswith('PriceOfBread.SimCore/'):
            if f == 'Coefficients.cs':
                g = 'coefself'
            elif f.startswith('Instruments'):
                g = 'ins'
            else:
                g = 'sim'
        elif rel.startswith('PriceOfBread.Runner/'):
            g = 'runner'
        elif rel.startswith('PriceOfBread.App/'):
            g = 'app'
        elif rel.startswith('PriceOfBread.Tests/'):
            g = 'tests'
        else:
            continue
        groups.setdefault(g, []).append((rel, strip(open(p, encoding='utf-8-sig', errors='replace').read())))

word = re.compile(r'[A-Za-z_][A-Za-z0-9_]*')
cnt = {g: {} for g in groups}
for g, files in groups.items():
    for rel, s in files:
        for w in word.findall(s):
            cnt[g][w] = cnt[g].get(w, 0) + 1

cmpre_cache = {}
def flagcmp(name):
    pat = re.compile(r'\b' + name + r'\s*(>=|<=|==|!=|>|<)\s*(\d+(?:\.\d+)?)')
    hits = []
    for g in ('sim', 'coefself'):
        for rel, s in groups.get(g, []):
            for m in pat.finditer(s):
                hits.append(m.group(1) + m.group(2))
    return sorted(set(hits))

def num(init):
    m = re.search(r'-?\d+(?:\.\d+)?(?:[eE]-?\d+)?', init)
    return float(m.group(0)) if m else None

with open(out, 'w', encoding='utf-8', newline='\n') as w:
    w.write('line\ttype\tname\tinit\tdefault\tlist\tsim\tcoefself\tins\trunner\tapp\ttests\tflagcmp\tkw\tcomment\n')
    for (ln, typ, name, init, cm, pre) in fields:
        d = num(init) if init else 0.0
        lst = 'DZ' if name in DZ else 'DO' if name in DO else 'ZL' if name in ZL else ''
        com = (pre + ' ' + cm).replace('\t', ' ')
        kw = ','.join(k for k in ('点火', '休眠', '旗', '器', '既定 0', '既定0', '差し戻し', '墓標', '確証なし') if k in com)
        fc = flagcmp(name)
        w.write('\t'.join(str(x) for x in (ln, typ, name, init, d, lst,
            cnt['sim'].get(name, 0), cnt.get('coefself', {}).get(name, 0) - 1, cnt['ins'].get(name, 0),
            cnt['runner'].get(name, 0), cnt['app'].get(name, 0), cnt['tests'].get(name, 0),
            ' '.join(fc), kw, com[:300])) + '\n')
print('fields', len(fields), 'DZ', len(DZ), 'DO', len(DO), 'ZL', len(ZL), 'RT', len(RT))
missing = [n for n in DZ + DO if n not in {f[2] for f in fields}]
print('DZ/DO names not parsed:', missing)
