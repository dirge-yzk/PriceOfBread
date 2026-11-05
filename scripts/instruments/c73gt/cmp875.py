# (875) c73gt (b) の比べ：stoplines875.sh の出力の判定の行が、基準の出力（stop_mM.txt など）とバイトで一致することを確かめる（事前登録 [3] (b)）。
#   使い方: python -X utf8 -I cmp875.py <新しい出力> <基準の出力> <基準の字句> <新しい字句> [<基準の字句> <新しい字句> ...]（字句の組は ins のパスと出力のパス＝環境で変わる所だけ）
#   手順：新しい出力から、足した記録の節（印の行から末尾まで）を除く。基準の出力の ins のパスの字句を新しい ins の字句へ置き換える（差が出てよい所はそこだけ）。
#   その後に全行がバイトで一致するか。一致しなければ最初の違う行を出して終了コード 1。
import sys
MARK = '== pxmonth の名乗り（875・記録）'.encode('utf-8')
new = open(sys.argv[1], 'rb').read(); ref = open(sys.argv[2], 'rb').read()
pairs = [(sys.argv[k].encode('utf-8'), sys.argv[k + 1].encode('utf-8')) for k in range(3, len(sys.argv) - 1, 2)]
i = new.find(MARK)
if i < 0:
    print('VERDICT cmp875 fail 記録の節の印の行が無い'); sys.exit(1)
nl = new[:i]; rl = ref
for o, n in pairs: rl = rl.replace(o, n)
na, ra = nl.split(b'\n'), rl.split(b'\n')
if nl == rl:
    print('VERDICT cmp875 pass 判定の行は基準とバイトで一致（%d 行・置き換えた字句の出現 %d 回・足した節は %d バイト）' % (len(na) - 1, sum(ref.count(o) for o, _ in pairs), len(new) - i)); sys.exit(0)
for k in range(max(len(na), len(ra))):
    a = na[k] if k < len(na) else '(無し)'.encode('utf-8'); b = ra[k] if k < len(ra) else '(無し)'.encode('utf-8')
    if a != b:
        print('VERDICT cmp875 fail %d 行目が違う\n  新 : %s\n  基準: %s' % (k + 1, a[:200].decode('utf-8', 'replace'), b[:200].decode('utf-8', 'replace'))); break
sys.exit(1)
