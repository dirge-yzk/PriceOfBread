# (847) c69hl 型の候補 T1・T2・T3 の空振りの率（ヌル腕 9 組の一つ抜き）と、選んだ型での腕の読み直し（読むだけ・事前登録 m847 §1〜§3・§7）
#   python -X utf8 -I loo847.py --hc <holclass840.py> --ratios <比の表> --an <an_h2 -v5 の出力のフォルダ（<名>.txt）>
#        --groups <名,...（ヌル腕 9 組）> --base <名:基線の名,...（- は無し）> --board <名:集合+集合,...> [--arms <名,...>] [--check <holclass840 の出力のフォルダ（new_<名>.txt）>]
#   T1 の中身は holclass840.py をそのまま呼ぶ（--null <母集合> --self <腕>）。T1＝(ii) が 1 以上で止める／T2＝c(腕) > thr（thr＝参照の組の (ii) の件数の最大）／T3＝T1 かつ T2。
#   一つ抜き：腕 k＝ヌル腕の 1 組のとき c(k)＝--null 9 組 --self k・参照 R＝9 組−{k}・c_R(j)＝--null R --self j（入れ子＝k は j の母集合にも入らない）。
#   腕がヌル腕でないとき：R＝9 組・c_R(j)＝--null 9 組 --self j。
import sys, re, subprocess, math
sys.stdout.reconfigure(newline='\n')   # Windows の文字モードの CRLF を避ける（出力は LF）
def opt(args, name, default=None):
    if name not in args: return default, args
    i = args.index(name); v = args[i + 1]
    return v, args[:i] + args[i + 2:]
args = sys.argv[1:]
HC, args = opt(args, '--hc'); RAT, args = opt(args, '--ratios'); AN, args = opt(args, '--an')
gs, args = opt(args, '--groups'); bs, args = opt(args, '--base', ''); bds, args = opt(args, '--board', '')
arms_s, args = opt(args, '--arms', ''); CHK, args = opt(args, '--check')
GROUPS = [x for x in gs.split(',') if x]
BASE = dict(x.split(':', 1) for x in bs.split(',') if x)
BOARD = {k: v.replace('+', ',') for k, v in (x.split(':', 1) for x in bds.split(',') if x)}
ARMS = [x for x in arms_s.split(',') if x]
SEEDS = {}
with open(RAT, encoding='utf-8') as fh:
    h = fh.readline().rstrip('\n').split('\t'); ix = {c: i for i, c in enumerate(h)}
    for line in fh:
        x = line.rstrip('\n').split('\t'); SEEDS.setdefault(x[ix['set']], set()).add(int(x[ix['seed']]))
def band(a): return '11-20' if SEEDS.get(a) and min(SEEDS[a]) >= 11 else '1-10'
PAT = re.compile(r'例外 (\d+)・元からの弱さ (\d+)・ヌル腕の幅の内 (\d+)・線が原因（新） (\d+)・食い違い (\d+)')
RROW = re.compile(r'\]\s+s(\d+)\s+(\S+?)（.*\tr ([0-9.naninf]+)\t.*記録\(a\) 全ての種 n=(\d+) 最小 ([0-9.naninf]+)')
CACHE = {}
USED = {}   # (853) holclass840 へ実際に渡した基線と盤面（腕ごと）＝割り当ての写し間違いを割り当ての表で読めるようにする
def hc(a, null):
    key = (a, tuple(null))
    if key in CACHE: return CACHE[key]
    cmd = [sys.executable, '-X', 'utf8', '-I', HC, f'{AN}/{a}.txt']
    if BASE.get(a, '-') != '-': cmd.append(f'{AN}/{BASE[a]}.txt')
    cmd += ['--ratios', RAT, '--null', ','.join(null), '--self', a]
    if a in BOARD: cmd += ['--board', BOARD[a]]
    USED[a] = (cmd[6][len(AN) + 1:-4] if cmd[6] != '--ratios' else '-', cmd[cmd.index('--board') + 1] if '--board' in cmd else '')
    out = subprocess.run(cmd, capture_output=True, encoding='utf-8', check=True).stdout
    m = PAT.search(out)
    cnt = tuple(int(v) for v in m.groups())
    rows = []
    for l in out.splitlines():
        if l.startswith('線が原因（新）'):
            r = RROW.search(l)
            rows.append((int(r.group(1)), r.group(2), float(r.group(3)), float(r.group(5))) if r else (None, l[:60], float('nan'), float('nan')))
    CACHE[key] = (cnt, rows)
    return CACHE[key]
def c_ii(a, null): return hc(a, null)[0][3]
def t1(c): return c >= 1
def t2(c, thr): return c > thr
# ---- 一つ抜き 9 組
print('# (847) 一つ抜き 9 組（ヌル腕 9 組を 1 本ずつ腕に見立てる）')
print('腕k\t帯\tc(k)\t参照の件数 c_R(j)\tthr\tT1\tT2\tT3')
STOP = {'T1': 0, 'T2': 0, 'T3': 0}
for k in GROUPS:
    ck = c_ii(k, GROUPS)
    R = [g for g in GROUPS if g != k]
    cR = {j: c_ii(j, R) for j in R}
    thr = max(cR.values())
    s1, s2 = t1(ck), t2(ck, thr); s3 = s1 and s2
    STOP['T1'] += s1; STOP['T2'] += s2; STOP['T3'] += s3
    print(f'{k}\t{band(k)}\t{ck}\t{",".join(f"{j}:{v}" for j, v in cR.items())}\t{thr}\t{"止める" if s1 else "-"}\t{"止める" if s2 else "-"}\t{"止める" if s3 else "-"}')
N = len(GROUPS)
print(f'## 空振り T1 {STOP["T1"]}/{N}・T2 {STOP["T2"]}/{N}・T3 {STOP["T3"]}/{N}')
# ---- 型を選ぶ規則（事前登録 m847 §3）：1/3 未満のうち最も低い・同じなら止める側が強い方（T1 ⊇ T2 ⊇ T3）
ORDER = ['T1', 'T2', 'T3']   # 止める側が強い順
ok = [t for t in ORDER if STOP[t] * 3 < N]
if ok:
    lo = min(STOP[t] for t in ok); pick = next(t for t in ORDER if t in ok and STOP[t] == lo)
    print(f'## 選んだ型 {pick}（空振り {STOP[pick]}/{N}・1/3 未満の型 {",".join(ok)}）')
else:
    pick = None; print('## 選んだ型 無し（全ての型の空振りが 1/3 以上＝記録へ下げるを推奨）')
# ---- 参照の件数（腕がヌル腕でないとき：R＝9 組）
C9 = {j: c_ii(j, GROUPS) for j in GROUPS}
thr9 = max(C9.values())
NULL6 = [g for g in GROUPS if band(g) == '1-10']
print(f'## 参照（9 組・自分を外した件数）{",".join(f"{j}:{v}" for j, v in C9.items())}・thr {thr9}・thr6（種 1〜10 の 6 組だけ・記録）{max(C9[j] for j in NULL6) if NULL6 else "nan"}')
if ARMS:
    print('# (847) 腕の読み直し（選んだ型で・T1/T2/T3 を全て並べる）')
    print('腕\t帯\t例外\t元からの弱さ\t(i)\t(ii)\t食い違い\tT1\tT2（c・thr）\tthr6（記録）\tthrB 同じ帯（記録）\tT3\t選んだ型\t(ii) の行（種・州・r・全ての種の最小・深さの比＝記録）')
    for a in ARMS:
        cnt, rows = hc(a, GROUPS)
        c = cnt[3]; b = band(a)
        thrB = max(C9[j] for j in GROUPS if band(j) == b)
        thr6 = max(C9[j] for j in NULL6)
        s1, s2 = t1(c), t2(c, thr9); s3 = s1 and s2
        sp = {'T1': s1, 'T2': s2, 'T3': s3}.get(pick)
        rr = '; '.join(f's{s} {g} r {r:.3f} 最小(a) {pa:.3f} 比 {r / pa:.2f}' if s is not None and pa > 0 else str(g) for s, g, r, pa in rows) or '-'
        print(f'{a}\t{b}\t{cnt[0]}\t{cnt[1]}\t{cnt[2]}\t{c}\t{cnt[4]}\t{"止める" if s1 else "-"}\t{"止める" if s2 else "内"}（{c}・{thr9}）\t{thr6}\t{thrB}\t{"止める" if s3 else "-"}\t'
              f'{"止める" if sp else ("止めない" if pick else "記録")}\t{rr}')
if CHK:
    print('# (847) 恒等の門 3：holclass840 の 9 組の呼び方（--null 9 組 --self 名）の件数＝門 2 の出力 new_<名>.txt の件数')
    bad = 0
    for a in GROUPS + ARMS:
        got = hc(a, GROUPS)[0]
        m = PAT.search(open(f'{CHK}/new_{a}.txt', encoding='utf-8').read())
        exp = tuple(int(v) for v in m.groups())
        bad += got != exp
        print(f'{a}\t{"一致" if got == exp else "不一致"}\t{got}\t{exp}')
    print(f'## 恒等の門 3：不一致 {bad}')
# ---- (853) 割り当ての表：holclass840 へ渡した基線と盤面（--base・--board の割り当てを mut847.sh が期待と突き合わせる）
for a in sorted(USED):
    print(f'## 割り当て {a} 基線 {USED[a][0]} 盤面 {USED[a][1]}')
