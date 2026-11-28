#!/usr/bin/env bash
# (1434) c154tl 門と突然変異の台本（使い捨て）。使い方: gate_tl.sh <木> <作業フォルダ W=ident の出力> <出力フォルダ> <fl_gate の旧>
R="$1"; W="$2"; O="$3"; OLDFL="$4"; mkdir -p "$O"
CK="$R/scripts/instruments/ident/ident_check.py"; MK="$W/marks/arms.txt"
chk(){ python -X utf8 -I "$1" c154tl --root "$R" --work "$W" --tree "$R" ${2:+--saveleak} 2>&1; }
cp "$MK" "$O/arms_orig.txt"
# (a) 揃った W
chk "$CK" 1 > "$O/a_full.txt"; echo "(a) $(grep '^判定:' "$O/a_full.txt")"
# (b) b を外した写し（arms.txt だけ差し替え・終わりに戻す）
printf 'a t s\n' > "$MK"; chk "$CK" 1 > "$O/b_nob.txt"; echo "(b) $(grep '^判定:' "$O/b_nob.txt")"; grep -c '段 2 で腕 b' "$O/b_nob.txt"
# μ1 miss を消す（使い捨ての写し）
python - "$CK" "$O/ck_mu1.py" <<'PY'
import sys
b=open(sys.argv[1],'rb').read().decode()
o="        miss('段 2 で腕 b〔計器つき 10 種〕の出力が無い＝計器の表を見ていない')\n"
assert o in b
open(sys.argv[2],'wb').write(b.replace(o,"        pass\n").encode())
PY
chk "$O/ck_mu1.py" 1 > "$O/mu1.txt"; echo "μ1（b 無し・miss 消し） $(grep '^判定:' "$O/mu1.txt")"
printf 'a b t s\n' > "$MK"
# μ3 b がある写しでも miss が出る
python - "$CK" "$O/ck_mu3.py" <<'PY'
import sys
b=open(sys.argv[1],'rb').read().decode()
o="if STAGE == 2 and 'b' not in arms:"
assert o in b
open(sys.argv[2],'wb').write(b.replace(o,"if STAGE == 2:").encode())
PY
chk "$O/ck_mu3.py" 1 > "$O/mu3.txt"; echo "μ3（b 有り・常に miss） $(grep '^判定:' "$O/mu3.txt")"
cp "$O/arms_orig.txt" "$MK"
