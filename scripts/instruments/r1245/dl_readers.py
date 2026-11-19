# (1189) 問い1：削除の候補ごとに、木 <commit> の読み手の行を数える（注を除く・語の境界）。
#  区分：E＝SimCore の本体（Instruments・Display・Coefficients*.cs の宣言の行を除く）・I＝SimCore/Instruments・D＝SimCore/Display・C＝Coefficients*.cs・
#        T＝PriceOfBread.Tests・U＝PriceOfBread.Tests.Ui・A＝PriceOfBread.App・R＝PriceOfBread.Runner・S＝scripts・J＝*.json のデータ
#  本体の行は〈比べ・条件の演算子を含むか〉〈+= か Militancy／Tension への加算か〉を印にして読む（(c) の材料・最後は目で確かめる）。
#  使い方: python -X utf8 -I dl_readers.py <repo> <commit> <候補の tsv> <出力 tsv> <本体の行の出力>
import sys, subprocess, re, collections, csv
sys.stdout.reconfigure(newline='\n')
repo, commit, cand, out, linesout = sys.argv[1:6]
def area(p):
    if p.startswith('PriceOfBread.SimCore/Instruments/'): return 'I'
    if p.startswith('PriceOfBread.SimCore/Display/'): return 'D'
    if p.startswith('PriceOfBread.SimCore/') and p.split('/')[-1].startswith('Coefficients'): return 'C'
    if p.startswith('PriceOfBread.SimCore/'): return 'J' if p.endswith('.json') else 'E'
    if p.startswith('PriceOfBread.Tests.Ui/'): return 'U'
    if p.startswith('PriceOfBread.Tests/'): return 'T'
    if p.startswith('PriceOfBread.App/'): return 'A'
    if p.startswith('PriceOfBread.Runner/'): return 'R'
    if p.startswith('scripts/'): return 'S'
    return None
def code_part(s):
    i = s.find('//')
    return s if i < 0 else s[:i]
rows = list(csv.reader(open(cand, encoding='utf-8'), delimiter='\t'))
hdr, rows = rows[0], rows[1:]
fo = open(out, 'w', encoding='utf-8', newline='\n'); fl = open(linesout, 'w', encoding='utf-8', newline='\n')
fo.write('\t'.join(['id', '名', 'E', 'I', 'D', 'C', 'T', 'U', 'A', 'R', 'S', 'J', 'E_files', 'E_cond', 'E_add', '識別子']) + '\n')
for r in rows:
    cid, name, idents = r[0], r[1], [x for x in r[2].split(',') if x]
    cnt = collections.Counter(); files = collections.Counter(); cond = add = 0; seen = set()
    for ident in idents:
        pat = ident if ident.startswith('re:') else None
        args = ['git', '-C', repo, 'grep', '-n', '-P' if pat else '-w', (pat[3:] if pat else ident), commit, '--']
        res = subprocess.run(args, capture_output=True).stdout.decode('utf-8', 'replace')
        for l in res.split('\n'):
            if not l: continue
            m = re.match(r'^[^:]+:([^:]+):(\d+):(.*)$', l)
            if not m: continue
            p, ln, txt = m.group(1), int(m.group(2)), m.group(3)
            a = area(p)
            if a is None: continue
            key = (p, ln)
            if key in seen: continue
            if a in 'EICDTUAR':
                c = code_part(txt)
                if not (re.search(r'\b' + re.escape(ident) + r'\b', c) if not pat else re.search(pat[3:], c)): continue
            seen.add(key); cnt[a] += 1
            if a == 'E':
                files[p.split('/')[-1]] += 1
                c = code_part(txt)
                isc = bool(re.search(r'[<>]=?(?!=)|\?|Math\.(Max|Min)|Clamp', c)); isa = bool(re.search(r'\+=|Militancy|Tension', c))
                cond += isc; add += isa
                fl.write(f'{cid}\t{p}:{ln}\t{"条" if isc else ""}{"加" if isa else ""}\t{c.strip()[:200]}\n')
    fo.write('\t'.join([cid, name] + [str(cnt[a]) for a in 'EIDCTUARSJ'] + [' '.join(f'{k}:{v}' for k, v in files.most_common()), str(cond), str(add), ','.join(idents)]) + '\n')
print('候補', len(rows))
