# (1075) 直列の集まり（[Collection("Instruments")]）のクラスが、何の静的な状態に触れるかを字面で数える。読むだけ。
# 使い方: python -X utf8 -I coll_why.py <試験の木> <class_secs.tsv> <出力>
# 限界：ファイルの単位（1 ファイルに複数のクラスがあると、ファイルの字面を全てのクラスに当てる）＝上界。
import sys, os, re, collections
TSRC, CS, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
COMMENT = re.compile(r'//[^\n]*|/\*.*?\*/', re.S)
PAT = {
    'Instruments.On': re.compile(r'Instruments\.On\b|Instruments\.Configure\('),   # Configure は On を書く（Instruments.cs:65）
    'Instruments.他の静的': re.compile(r'Instruments\.(?!On\b)[A-Z][A-Za-z]*\s*='),
    '環境変数を書く': re.compile(r'SetEnvironmentVariable\('),
    'Instruments を読む': re.compile(r'\bInstruments[A-Za-z]*\.'),
}
rows = [l.rstrip('\n').split('\t') for l in open(CS, encoding='utf-8')][1:]
out = []
agg = collections.defaultdict(lambda: [0, 0.0])
for cls, f, coll, world, n, secs, a, b in rows:
    if coll != 'Instruments' or not f:
        continue
    t = COMMENT.sub('', open(os.path.join(TSRC, f), 'rb').read().decode('utf-8-sig', 'replace'))
    hits = [k for k, p in PAT.items() if p.search(t)]
    key = 'On を切り替える（Configure を含む）' if 'Instruments.On' in hits else ('環境変数を書く' if '環境変数を書く' in hits else ('計器を読むだけ' if hits else '計器の字なし'))
    agg[key][0] += 1; agg[key][1] += float(secs)
    out.append(f'{cls}\t{f}\t{n}\t{float(secs):.1f}\t{key}\t{",".join(hits)}')
with open(OUT, 'w', encoding='utf-8', newline='\n') as fh:
    fh.write('== 区分：クラス数・秒\n')
    for k, v in sorted(agg.items(), key=lambda kv: -kv[1][1]):
        fh.write(f'{k}\t{v[0]}\t{v[1]:.1f}\n')
    fh.write('\n== クラスごと（秒の降順）：クラス・ファイル・件・秒・区分・当たった字\n')
    for l in sorted(out, key=lambda l: -float(l.split('\t')[3])):
        fh.write(l + '\n')
print('ok', len(out))
