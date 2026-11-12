# (1061) 試走の比べ：計器つきの走りを POB_INSTRUMENTS_TABLES で絞った出力が、基準の生（INSRAW）と選んだ表でバイト一致か・選ばない表のファイルが 0 本かを見る。
#   python -X utf8 -I trial_cmp.py <試走の ins フォルダ> <INSRAW> <表,...> <種の範囲 1-3> [BYPASS 表,...]    ＝ 0 で合格・1 で不合格
import os, re, sys, hashlib

def sha(p):
    h = hashlib.sha1()
    with open(p, 'rb') as f:
        for c in iter(lambda: f.read(1 << 20), b''): h.update(c)
    return h.hexdigest()

def main():
    new, base, tables, rng = sys.argv[1], sys.argv[2], [x for x in sys.argv[3].split(',') if x], sys.argv[4]
    bypass = [x for x in (sys.argv[5] if len(sys.argv) > 5 else '').split(',') if x]
    a, b = rng.split('-'); seeds = range(int(a), int(b) + 1)
    bad = []; same = 0; absent = 0
    for t in tables:
        for s in seeds:
            pb, pn = os.path.join(base, f'{t}{s}.tsv'), os.path.join(new, f'{t}{s}.tsv')
            eb, en = os.path.isfile(pb), os.path.isfile(pn)
            if eb != en: bad.append(f'{t}{s}.tsv：基準 {"有" if eb else "無"}・試走 {"有" if en else "無"}')
            elif eb and os.path.getsize(pb) == os.path.getsize(pn) and sha(pb) == sha(pn): same += 1
            elif eb: bad.append(f'{t}{s}.tsv：バイトが違う')
            else: absent += 1
    names = [f for f in os.listdir(new) if os.path.isfile(os.path.join(new, f))]
    ok_t = set(tables) | set(bypass)
    outside = [f for f in names if (re.match(r'^(.*_s)\d+\.tsv$', f) or [None, f])[1] not in ok_t]
    if outside: bad.append(f'選ばない表のファイルが {len(outside)} 本：{outside[:4]}')
    if same == 0: bad.append('バイト一致が 1 本も無い（空の比べ）')
    print(f'試走の比べ：選んだ表 {len(tables)}・バイト一致 {same} 本・両方に無い {absent} 本・選ばない表のファイル {len(outside)} 本（Open を通らない表 {len(bypass)} 種は除く）')
    for x in bad[:10]: print('  ✗ ' + x)
    print('試走の判定: ' + ('合格' if not bad else '不合格'))
    return 1 if bad else 0

if __name__ == '__main__': sys.exit(main())
