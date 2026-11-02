# (784) inheritintent_s の M（c8ii の計器つき）と c8pd の腕 B（c8ii の前の計器）の比べ（ident_check.py の cols_diff の rows_grown をそのまま使う・読むだけ）
#   python -X utf8 iicmp.py <M の ins> <B の ins>
import sys, os, glob
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'ident'))
import ident_check as ic
a, b = sys.argv[1], sys.argv[2]
ok = 0; bad = []; added = 0; cols = set()
for pb in sorted(glob.glob(os.path.join(b, 'inheritintent_s*.tsv'))):
    pa = os.path.join(a, os.path.basename(pb))
    kn, real, extra = ic.cols_diff(pb, pa, {}, rows_grown=True)
    cols |= set(extra)
    if real: bad.append((os.path.basename(pb), real))
    else: ok += 1; added += ic.ROWS_ADDED.get(pb, 0)
print(f'inheritintent_s：基準の行が M の行の順序つき部分列 {ok} 本・違う {len(bad)} 本・M で増えた行 {added}・M で増えた列 {sorted(cols)}')
for f, r in bad: print('  違う', f, r)
print('判定:', '一致（行と列の増えだけ）' if not bad else '不一致')
