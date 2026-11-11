# (845) 合流：合わせた木の ident の計器つき 10 種の生を INSRAW（keep_845/base_ins）へ置き、
#   前の INSRAW（keep_839/base_ins）とバイトで同じ表をハードリンクに替える（D: の空きを守る・中身は 1 バイトも変えない）。
#   python -I -X utf8 mkkeep845.py <新しい INSRAW のフォルダ> <前の INSRAW のフォルダ> <出力の表>
#   ⚠ NTFS のハードリンク：前の keep を消しても新しい keep の中身は残る。前の keep を消して空くのは差のある表の分だけではない（リンクされた表は新しい keep が持ち続ける）。
#   ⚠ 生の表を書き換える道具は無い前提（書き換えると両方が変わる）。
import os, sys, filecmp, hashlib
new, old, out = sys.argv[1], sys.argv[2], sys.argv[3]
rows = []; linked = 0; kept = 0; saved = 0
for f in sorted(os.listdir(new)):
    pn, po = os.path.join(new, f), os.path.join(old, f)
    size = os.path.getsize(pn)
    if os.path.isfile(po) and os.path.getsize(po) == size and filecmp.cmp(pn, po, shallow=False):
        tmp = pn + '.lnk_tmp'
        os.link(po, tmp)
        if not filecmp.cmp(tmp, pn, shallow=False):
            os.remove(tmp); raise SystemExit('リンクの中身が違う ' + f)
        os.replace(tmp, pn)
        linked += 1; saved += size; rows.append((f, size, 'link'))
    else:
        kept += 1; rows.append((f, size, 'own'))
with open(out, 'w', encoding='utf-8', newline='\n') as w:
    w.write('# file\tbytes\tkind（link＝前の INSRAW とバイトで同じ＝ハードリンク・own＝この木の表）\n')
    for r in rows: w.write('%s\t%d\t%s\n' % r)
    w.write('# link %d 本 %.2f GB・own %d 本 %.2f GB\n' % (linked, saved / 1e9, kept, sum(r[1] for r in rows if r[2] == 'own') / 1e9))
print('link', linked, round(saved / 1e9, 2), 'GB・own', kept)
