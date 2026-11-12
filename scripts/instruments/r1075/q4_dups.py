# (1075) 同じ世界を何度も作って進める試験を、ソースの字面の〈世界の作り方＋進め方〉の署名で束ねる。読むだけ。
# 使い方: python -X utf8 -I q4_dups.py <試験の木> <nonfull.trx> <出力>
# 署名（AI の案・確証なし）：試験の本体の中の、世界を作る行（MultiNationSim・new(・StartWarmupShared・Board(・Two( ほか）と、
#   進める行（.Run(・StartWarmup(・StepQuarter の for の上限）を、空白を詰めて並べた物。補助関数の中身は追わない（呼びの字面だけ）。
# 限界：同じ署名でも、作った後に係数や法を替える試験は共有できない＝〈共有できる上限〉。署名が違っても同じ世界はありうる（下界でない）。
#   [Theory] は 1 つの署名に引数ごとの件が乗る（引数で盤面が違う＝共有の上限から外す）。
import sys, os, re, collections, xml.etree.ElementTree as ET
TSRC, TRX, OUT = sys.argv[1:4]
NSX = '{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}'
r = ET.parse(TRX).getroot()
def dur(s):
    h, m, x = s.split(':'); return int(h) * 3600 + int(m) * 60 + float(x)
meth = {}
for u in r.iter(NSX + 'UnitTest'):
    tm = u.find(NSX + 'TestMethod'); meth[u.get('id')] = (tm.get('className').split('.')[-1], tm.get('name').split('(')[0])
secs = collections.defaultdict(float); cnt = collections.Counter()
for u in r.iter(NSX + 'UnitTestResult'):
    k = meth[u.get('testId')]; secs[k] += dur(u.get('duration', '0:0:0')); cnt[k] += 1
COMMENT = re.compile(r'//[^\n]*|/\*.*?\*/', re.S)
CLS = re.compile(r'^\s*(?:(?:public|internal|private|static|sealed|partial)\s+)*class\s+([A-Za-z_][A-Za-z0-9_]*)', re.M)
FACT = re.compile(r'\[(Fact|Theory)[^\]]*\][^{;]*?\b(?:public|private|internal)?\s*(?:async\s+)?(?:void|Task)\s+([^\s(]+)\s*\(', re.S)
MK = re.compile(r'(new\s+MultiNationSim\s*\([^;]*|=\s*new\s*\(\s*(?:WorldData|\[|cfgs|configs)[^;]*|\b(?:Board|Two|Four|FourNations|NewSim|Make|Gt|Uk|Cess|PortugalRemainder|Sim|World|Fresh)\s*\([^;]*)')
RUN = re.compile(r'(\.Run\s*\(\s*[^)]*\)|\.StartWarmup\s*\([^)]*\)|for\s*\(\s*int\s+\w+\s*=\s*0\s*;\s*\w+\s*<\s*[^;]+;[^)]*\)\s*\{?\s*[\w.]*StepQuarter)')
sig = collections.defaultdict(list)
nfound = 0
for f in sorted(os.listdir(TSRC)):
    if not f.endswith('.cs'):
        continue
    t = COMMENT.sub('', open(os.path.join(TSRC, f), 'rb').read().decode('utf-8-sig', 'replace'))
    cls_pos = [(m.start(), m.group(1)) for m in CLS.finditer(t)]
    for m in FACT.finditer(t):
        name = m.group(2)
        c = [n for p, n in cls_pos if p < m.start()]
        c = c[-1] if c else '?'
        i = t.find('{', m.end())
        if i < 0: continue
        depth, j = 0, i
        while j < len(t):
            if t[j] == '{': depth += 1
            elif t[j] == '}':
                depth -= 1
                if depth == 0: break
            j += 1
        body = t[i:j]
        mk = [re.sub(r'\s+', '', x)[:160] for x in MK.findall(body)]
        rn = [re.sub(r'\s+', '', x)[:80] for x in RUN.findall(body)]
        if not mk or not rn:
            continue
        k = (c, name)
        if k not in secs:
            continue
        nfound += 1
        sig[(f, mk[0], rn[0])].append((c, name, secs[k], cnt[k], m.group(1)))
L = []
groups = [(k, v) for k, v in sig.items() if len(v) >= 2]
save = 0.0
rows = []
for k, v in groups:
    facts = [x for x in v if x[4] == 'Fact']
    if len(facts) < 2: continue
    s = sorted(x[2] for x in facts)
    sv = sum(s) - s[-1]   # 1 本だけ作り、他は共有した場合の上限（最も長い 1 本を残す）
    save += sv
    rows.append((sv, k, facts))
rows.sort(key=lambda x: -x[0])
L.append(f'署名を取れた試験 {nfound}・同じ署名の組（ファイルの中・[Fact] 2 本以上）{len(rows)}・共有の上限の秒（各組で最長 1 本を残した残りの和）{save:.1f}')
L.append('注：共有の上限は〈作って進めた後に盤面を替えない〉が成り立つ場合だけ＝本文を読まずに数えた上限。並列の段の中なので壁時計への効きは ÷16 程度（直列の集まりの中なら秒のまま）。')
L.append('\n== 上位 25 組：上限の秒・本数・ファイル・作り方・進め方・〔クラス.試験 秒〕')
for sv, k, facts in rows[:25]:
    L.append(f'{sv:.1f}\t{len(facts)}\t{k[0]}\t{k[1][:90]}\t{k[2]}')
    for c, n, s, ct, kind in sorted(facts, key=lambda x: -x[2])[:6]:
        L.append(f'\t\t{c}.{n[:60]}\t{s:.1f}')
with open(os.path.join(OUT, 'q4_dups.txt'), 'w', encoding='utf-8', newline='\n') as fh:
    fh.write('\n'.join(L) + '\n')
print('ok', nfound, len(rows), f'{save:.1f}')
