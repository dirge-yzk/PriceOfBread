# (1083) 開幕の四半期の行だけを抜く（各種の表の最初の quarter）。使い方: python -X utf8 -I q0_extract.py <表のフォルダ> <出力>
import sys, os
d, out = sys.argv[1], sys.argv[2]
with open(out, 'w', encoding='utf-8', newline='\n') as fo:
    hdr_done = False
    for s in range(1, 11):
        p = os.path.join(d, f'rulepower_s{s}.tsv')
        with open(p, encoding='utf-8') as fi:
            h = fi.readline().rstrip('\n')
            if not hdr_done: fo.write('seed\t' + h + '\n'); hdr_done = True
            q0 = None; n = 0
            for line in fi:
                q = line.split('\t', 1)[0]
                if q0 is None: q0 = q
                if q != q0: break
                fo.write(f'{s}\t' + line); n += 1
        print('seed', s, 'q0', q0, 'rows', n)
