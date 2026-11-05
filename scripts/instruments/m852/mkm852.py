# (852) 合流：M ≡ 線の腕 B2 を表ごとに確かめ（★(826) の型）、バイトで同じ表は M の側を B2 のファイルへのハードリンクに替える（D: の空きのため・中身は 1 バイトも変えない）。
#   python -I -X utf8 mkm852.py <M の ins> <B2 の ins> <リポジトリ> <B2 の木のコミット> <M の木のコミット> <出力の表>
#   違う表の扱い（これ以外は〈違う〉＝採らない理由）：
#     subst_s・secession_s：B2 の木に無い末尾の列（c69ss・c69po の旗 0 の器）＝M の頭の列が B2 と全行同じか（cmpprefix826.py の型）。
#     cearn_s：経路の 3 列を B2 の木から M の木へ写して比べる（ident_check.py の cearn_same_but_location・列は同じ）。
#     flow_s：呼び出し位置を除いた行の多重集合（ident_check.py の flow_same_but_location）。
#   ⚠ハードリンクの後は B2 の生を消してよい（リンクの中身は M の側に残る）。生の表をその場で書き換える道具は無い前提。
import sys, os, filecmp, importlib.util, csv
mins, bins, repo, cb, cm, out = sys.argv[1:7]
spec = importlib.util.spec_from_file_location('ic', os.path.join(repo, 'scripts', 'instruments', 'ident', 'ident_check.py'))
ic = importlib.util.module_from_spec(spec); spec.loader.exec_module(ic)
ic.CEARN.update(root=repo, base=cb, arm_commit=cm, tree=None)
def prefix_same(pb, pm):
    with open(pb, encoding='utf-8', newline='') as fb, open(pm, encoding='utf-8', newline='') as fm:
        rb = csv.reader(fb, delimiter='\t'); rm = csv.reader(fm, delimiter='\t')
        hb = next(rb); hm = next(rm); n = len(hb)
        if hm[:n] != hb or len(hm) <= n: return False, 0
        k = 0
        for xb in rb:
            xm = next(rm, None)
            if xm is None or xm[:n] != xb: return False, k
            k += 1
        if next(rm, None) is not None: return False, k
    return True, len(hm) - n
rows = []; cnt = {}
for f in sorted(os.listdir(mins)):
    pm, pb = os.path.join(mins, f), os.path.join(bins, f)
    t = ic.table_of(f) if f.endswith('.tsv') else f
    if not os.path.isfile(pb):
        kind = 'M にだけ'
    elif os.path.getsize(pm) == os.path.getsize(pb) and filecmp.cmp(pm, pb, shallow=False):
        tmp = pm + '.lnk_tmp'; os.link(pb, tmp)
        if not filecmp.cmp(tmp, pm, shallow=False):
            os.remove(tmp); raise SystemExit('リンクの中身が違う ' + f)
        os.replace(tmp, pm); kind = 'link'
    elif t in ('subst_s', 'secession_s'):
        ok, extra = prefix_same(pb, pm); kind = f'頭の列が同じ（+{extra} 列）' if ok else '違う'
    elif t == 'cearn_s':
        ok, mv, ext = ic.cearn_same_but_location(pb, pm); kind = f'経路を写して同じ（{mv} 欄）' if ok and not ext else '違う'
    elif t == 'flow_s':
        kind = '呼び出し位置だけ' if ic.flow_same_but_location(pb, pm) else '違う'
    else:
        kind = '違う'
    cnt[kind.split('（')[0]] = cnt.get(kind.split('（')[0], 0) + 1
    rows.append((f, os.path.getsize(pm), kind))
onlyb = sorted(set(os.listdir(bins)) - set(os.listdir(mins)))
with open(out, 'w', encoding='utf-8', newline='\n') as w:
    w.write(f'# (852) M ≡ B2：M {mins}・B2 {bins}・B2 の木 {cb}・M の木 {cm}\n# file\tbytes\tkind\n')
    for r in rows: w.write('%s\t%d\t%s\n' % r)
    w.write(f'# B2 にだけ {len(onlyb)} 本 {onlyb[:5]}\n# ' + '・'.join(f'{k} {v}' for k, v in sorted(cnt.items())) + '\n')
    bad = cnt.get('違う', 0) + cnt.get('M にだけ', 0) + len(onlyb)
    w.write('# 判定：' + ('M ≡ B2（違うのは宣言した 4 つの型だけ）' if bad == 0 else f'違う表 {bad} 本＝帰属を読むまで採らない') + '\n')
print(open(out, encoding='utf-8').read().splitlines()[-2:])
