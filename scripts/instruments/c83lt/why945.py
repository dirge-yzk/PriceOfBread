# (945) c83lt 腕 B の G4・G8a・G8b の破れの帰属（読むだけ）：landfix_s の行ごとに、写しだけの凍結（lbFrozenCopyNoEngine）が増えた四半期で
#   ①写しの数（lbFrozenCopyRgo）が増え、エンジンの凍結の数（lbFrozenEngineRgo）が増えていないか（＝州が戻った時に計器の写しが残る型）
#   ②破れ（lbAdjBreakPopMonths・lbOverBasePopMonths）が写しだけの凍結の無い行に出ていないか、を数える。
#   使い方: python -X utf8 why945.py <ins フォルダ>
import csv, glob, sys, collections
I = sys.argv[1]
inc = ret = other = 0; brk_no_copy = brk_rows = 0; rows_no = collections.Counter()
for f in sorted(glob.glob(I + '/landfix_s*.tsv')):
    prev = {}
    for r in csv.DictReader(open(f, encoding='utf-8'), delimiter='\t'):
        n = r['nation']; cn = int(float(r['lbFrozenCopyNoEngine'])); cp = int(float(r['lbFrozenCopyRgo'])); en = int(float(r['lbFrozenEngineRgo']))
        ab = float(r['lbAdjBreakPopMonths']); ob = float(r['lbOverBasePopMonths'])
        if ab or ob:
            brk_rows += 1
            if cn == 0: brk_no_copy += 1; rows_no[(f[-8:], n, r['year'])] += 1
        if n in prev:
            pcn, pcp, pen = prev[n]
            if cn > pcn:
                inc += 1
                if cp - pcp >= cn - pcn and en <= pen: ret += 1
                else: other += 1
        prev[n] = (cn, cp, en)
print(f'写しだけの凍結が増えた四半期 {inc}・うち写しが増えエンジンが増えない型 {ret}・それ以外 {other}')
print(f'破れのある行 {brk_rows}・うち写しだけの凍結が 0 の行 {brk_no_copy}', dict(list(rows_no.items())[:10]))
