"""(721) rdh：州の間の移民（国内）のうち遠い組の量の割合（読むだけ）。python -X utf8 far_share.py <計器の出力ディレクトリ> [種...]
距離＝hol_common.dist（州の座標の大圏 km・読み手 an_h2 と同じ）。出力＝種ごとの総量（千人）と 3000 km 超・7000 km 超の割合、海越え（同じ陸塊でない）は数えない（座標だけ）。"""
import sys, os, collections
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'hol'))
import hol_common
d = sys.argv[1]
seeds = [int(x) for x in sys.argv[2:]] or sorted(int(f[len('migration_s'):-4]) for f in os.listdir(d) if f.startswith('migration_s'))
for sd in seeds:
    tot = far = vfar = 0.0
    for line in open(os.path.join(d, f'migration_s{sd}.tsv'), encoding='utf-8'):
        c = line.rstrip('\n').split('\t')
        if len(c) < 4 or not c[0].isdigit():   # (749) c3rt の後は 5 列
            continue
        v = float(c[3]); km = hol_common.dist(c[1], c[2])
        if km != km:
            continue
        tot += v
        if km > 3000: far += v
        if km > 7000: vfar += v
    print(f'{sd}\t{tot:.0f}\t{far / tot if tot else float("nan"):.4f}\t{vfar / tot if tot else float("nan"):.4f}')
