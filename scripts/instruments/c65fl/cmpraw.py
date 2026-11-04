# (819) c65fl 使い回しの確かめ（作者に聞く物 (B) の道具・読むだけ）：旗 1 の木で回した計器つき 10 種の生と、(815) の腕 B の生 keep_818_rej/c14flB_10 を表ごとにバイトで比べる。
#   使い方: python -X utf8 -I cmpraw.py <新しい生の ins> [<比べる生の ins>（既定 D:/PoB_work/keep_818_rej/c14flB_10/out_c14flB_10/ins）] [--ignore 表の名前の頭,...]
#   出力：両方にある表の数・バイトで同じ数・違う表（最初に違う行の番号）・片方にしか無い表。burscan_s と clock_s は秒の列を含む＝ident.cfg の KNOWN と同じく既定で外す（--ignore で足す）。
#   ⚠限界：合流の木に他の挙動の線が入っていれば一致しない（その線の軌道が混ざる）＝c65fl だけを足した木の生で読む。表の名前の頭は種の番号を除いた名（例 cpwd_s）。
import sys, os, re
args = [a for a in sys.argv[1:] if not a.startswith('--')]
new = args[0]
old = args[1] if len(args) > 1 else 'D:/PoB_work/keep_818_rej/c14flB_10/out_c14flB_10/ins'
ign = ['burscan_s', 'clock_s']
if '--ignore' in sys.argv: ign += sys.argv[sys.argv.index('--ignore') + 1].split(',')
def stem(f): return re.sub(r'\d+\.tsv$', '', f)
fa = set(os.listdir(new)); fb = set(os.listdir(old))
both = sorted(f for f in fa & fb if stem(f) not in ign)
same = 0; diff = []
for f in both:
    a = open(os.path.join(new, f), 'rb').read(); b = open(os.path.join(old, f), 'rb').read()
    if a == b: same += 1; continue
    la = a.split(b'\n'); lb = b.split(b'\n'); k = next((i for i in range(min(len(la), len(lb))) if la[i] != lb[i]), min(len(la), len(lb)))
    diff.append((f, k + 1, len(la), len(lb)))
print(f'両方にある表 {len(both)}（外した頭 {",".join(ign)}）・同じ {same}・違う {len(diff)}・新にだけ {len(fa - fb)}・旧にだけ {len(fb - fa)}')
for f, k, na, nb in diff[:40]: print(f'  違う {f}：最初に違う行 {k}（行数 新 {na}・旧 {nb}）')
for f in sorted(fa - fb)[:20]: print('  新にだけ', f)
for f in sorted(fb - fa)[:20]: print('  旧にだけ', f)
print('判定:', '一致' if not diff and not (fa - fb) and not (fb - fa) else '不一致')
