# (1350) c142lr 記録の読み手（読むだけ・門にしない・エンジンの関数を呼ばない）：
#   州ごとに〈ピークの後の最小÷ピーク〉を種ごとに出す（region_s の populationK・activeFlag 1 の行）。既定の州はバーミンガム（(1348) の合わせた木で種 8 が 0.095）。
#   使い方：python bham.py <ins> [州の名]
import sys, os, glob, re
ins = sys.argv[1]; reg = sys.argv[2] if len(sys.argv) > 2 else 'バーミンガム'
print(f'# {ins} 州={reg}')
print('seed\tpeakK\tpeakYear\tminAfterPeakK\tminYear\tminOverPeak')
for p in sorted(glob.glob(os.path.join(ins, 'region_s*.tsv')), key=lambda x: int(re.search(r'_s(\d+)', os.path.basename(x)).group(1))):
    s = int(re.search(r'_s(\d+)', os.path.basename(p)).group(1))
    ser = []
    with open(p, 'rb') as fh:
        h = fh.readline().rstrip(b'\r\n').decode('utf-8').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fh:
            c = line.rstrip(b'\r\n').decode('utf-8').split('\t')
            if c[ix['region']] != reg or c[ix['activeFlag']] != '1': continue
            ser.append((int(c[ix['quarter']]), c[ix['year']], float(c[ix['populationK']])))
    if not ser: print(f'{s}\tNA'); continue
    ip = max(range(len(ser)), key=lambda i: ser[i][2])
    tail = ser[ip:]; im = min(range(len(tail)), key=lambda i: tail[i][2])
    pk = ser[ip][2]; mn = tail[im][2]
    print(f'{s}\t{pk:.1f}\t{ser[ip][1]}\t{mn:.1f}\t{tail[im][1]}\t{(mn / pk if pk > 0 else float("nan")):.3f}')
