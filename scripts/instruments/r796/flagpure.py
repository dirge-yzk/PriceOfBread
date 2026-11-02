# r796 読むだけ：既定が 0 でない欄のうち、SimCore（Coefficients.cs 自身と Instruments*.cs を含む）での全ての出現が
# 〈数との比べ〉（> 0・<= 0・== 1 など）だけの欄＝純粋な旗を数える。旗が比べ以外（掛け算・引数渡し）で読まれる行があれば〈混じり〉。
# 使い方: python -X utf8 flagpure.py <repo> <coef.tsv> <out.tsv>
import os, re, sys
repo, tsv, out = sys.argv[1], sys.argv[2], sys.argv[3]
srcs = []
for root, ds, fs in os.walk(os.path.join(repo, 'PriceOfBread.SimCore')):
    if any(x in root for x in ('obj', 'bin')):
        continue
    for f in fs:
        if f.endswith('.cs'):
            p = os.path.join(root, f)
            t = open(p, encoding='utf-8-sig', errors='replace').read()
            # コメントを落とす（// 以降）
            t = '\n'.join(l.split('//')[0] for l in t.split('\n'))
            srcs.append((f, t))
big = '\n'.join(f'@@FILE {f}\n' + t for f, t in srcs)
rows = [l.rstrip('\n').split('\t') for l in open(tsv, encoding='utf-8')][1:]
import collections, bisect
POS = collections.defaultdict(list)
for m in re.finditer(r'[A-Za-z_][A-Za-z0-9_]*', big):
    POS[m.group(0)].append(m)
FILEPOS = [m.start() for m in re.finditer('@@FILE ', big)]
with open(out, 'w', encoding='utf-8', newline='\n') as w:
    w.write('line\tname\tdefault\ttotal_reads\tcmp_reads\tpure\tfiles\tkw\n')
    for r in rows:
        line, name, lst, kw = r[0], r[2], r[5], r[13]
        try:
            d = float(r[4])
        except ValueError:
            d = 1.0
        if d == 0.0 or lst:
            continue
        occ = POS.get(name, [])
        # 宣言の 1 回を除く
        reads = [m for m in occ if not re.search(r'public\s+[\w<>\[\],?\.]+\s+$', big[max(0,m.start()-60):m.start()])]
        if not reads:
            continue
        cmpn = 0
        files = set()
        for m in reads:
            tail = big[m.end():m.end() + 30]
            head = big[max(0, m.start() - 30):m.start()]
            if re.match(r'\s*(>=|<=|==|!=|>|<)\s*\d', tail) or re.search(r'\d\s*(>=|<=|==|!=|>|<)\s*(\w+\.)?$', head):
                cmpn += 1
            fpos = FILEPOS[bisect.bisect_right(FILEPOS, m.start()) - 1]
            files.add(big[fpos + 7: big.find('\n', fpos)])
        pure = 'pure' if cmpn == len(reads) else ('mixed' if cmpn else '')
        if not cmpn:
            continue
        w.write('\t'.join([line, name, str(d), str(len(reads)), str(cmpn), pure, ','.join(sorted(files)), kw]) + '\n')
