# (1152) c113lc G-C0〈壊さない・等式〉の読み手：種ごとに、世界で最初に機関車を知る国が出る四半期（基準の railspend_s の discEnd=1 の最小の q）より前の
#   全ての表の行が、基準と腕でバイトで同じか。読むだけ・書くのは標準出力だけ。
#   python -X utf8 -I gc0.py <基準の ins> <腕の ins> <種の始め> <種の終わり>
# 時の列は 'q'・'quarter'（四半期）か 'year'（年＝その四半期を含む年の前の年まで）。時の列の値が整数でない行（開幕・助走の名乗りなど）は〈前〉に数える。
# 各表は先頭から〈前〉の行が続く間だけ比べる（時の列が境に届いた最初の行で止める＝両方に同じ規則）。腕の見出しが基準の見出しの後ろに列を足した形なら、
#   腕の行を基準の列の数で切って比べる（足した列＝事前登録の恒等の計器）。時の列の無い表と見出しが合わない表は数えて名を出す。
import sys, os, glob
from collections import defaultdict

base, arm, s0, s1 = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4])

def first_q(ins, s):
    p = os.path.join(ins, f'railspend_s{s}.tsv')
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); iq, idd = h.index('q'), h.index('discEnd')
        for ln in f:
            c = ln.rstrip('\n').split('\t')
            if c[idd] == '1': return int(c[iq])
    return None

JIT = {'burscan_s': ('seconds',), 'clock_s': ('worldTotalPerMonth',), 'dep_s': ('callerLine',)}   # ident.cfg の KNOWN（既知の揺れ）＝両側で空にして比べる

def head_rows(p, tcol, lim, ncut, blank=(), dcol=None):
    # 追記 2（結果の後の変更・読み手の 2 つの誤りの直し）：① 時の列の値が 'q144' の形（armsfill_s）は頭の q を外して読む（初版は整数でないとして〈前〉に数え、境の後の行まで比べた）。
    #   ② wars_s の q は戦争の始まりの四半期で、行は months（続いた月数）と deadK を持つ＝行の中身は終わりまでの量。終わり（q＋月数÷3）が境の後の行は飛ばす（初版は始まりで読んだ）。
    out = []
    with open(p, encoding='utf-8', newline='') as f:
        f.readline()
        for ln in f:
            c = ln.rstrip('\r\n').split('\t')
            if tcol < len(c):
                v = c[tcol][1:] if c[tcol][:1] == 'q' else c[tcol]
                try: t = int(v)
                except ValueError: t = None
                if t is not None and t >= lim: break
                if t is not None and dcol is not None and dcol < len(c):
                    try: m = int(c[dcol])
                    except ValueError: m = None
                    if m is None or t + (m + 2) // 3 >= lim: continue
            for i in blank:
                if i < len(c): c[i] = ''
            out.append('\t'.join(c[:ncut] if ncut else c))
    return out

T = defaultdict(int); bad = []; skipped = defaultdict(int)
for s in range(s0, s1 + 1):
    qf = first_q(base, s)
    if qf is None: print(f'種 {s}：基準で誰も機関車を知らない＝全行が〈前〉（数えない）'); continue
    yf = 1783 + qf // 4
    for pb in sorted(glob.glob(os.path.join(base, f'*{s}.tsv'))):
        name = os.path.basename(pb)
        stem = name[:-len(f'{s}.tsv')]
        if not stem.endswith('_s') and not stem.endswith('_'):
            if stem[-1:].isdigit(): continue
        pa = os.path.join(arm, name)
        if not os.path.exists(pa): skipped['腕に無い'] += 1; continue
        with open(pb, encoding='utf-8') as f: hb = f.readline().rstrip('\r\n').split('\t')
        with open(pa, encoding='utf-8') as f: ha = f.readline().rstrip('\r\n').split('\t')
        ncut = 0
        if ha != hb:
            if ha[:len(hb)] == hb: ncut = len(hb)
            else: skipped['見出しが違う'] += 1; bad.append(f'種 {s} {name}：見出しが違う'); continue
        if 'q' in hb: tcol, lim = hb.index('q'), qf
        elif 'quarter' in hb: tcol, lim = hb.index('quarter'), qf
        elif 'year' in hb: tcol, lim = hb.index('year'), yf
        else: skipped['時の列が無い'] += 1; continue
        blank = tuple(hb.index(k) for k in JIT.get(stem, ()) if k in hb)
        dcol = hb.index('months') if stem == 'wars_s' and 'months' in hb else None
        rb = head_rows(pb, tcol, lim, 0, blank, dcol)
        ra = head_rows(pa, tcol, lim, ncut, blank, dcol)
        T['表'] += 1; T['行'] += len(rb)
        if rb != ra:
            T['違う表'] += 1
            k = next((i for i in range(min(len(rb), len(ra))) if rb[i] != ra[i]), min(len(rb), len(ra)))
            bad.append(f'種 {s} {name}：行の数 基準 {len(rb)}・腕 {len(ra)}・最初に違う行 {k}')
    print(f'種 {s}：境 q{qf}（{yf} 年）')
print(f'比べた表 {T["表"]}・〈前〉の行 {T["行"]}・違う表 {T["違う表"]}・飛ばした {dict(skipped)}')
for b in bad[:30]: print('  ', b)
print('G-C0 → ' + ('緑' if T['違う表'] == 0 and T['表'] > 0 else '赤'))
