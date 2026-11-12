#!/usr/bin/env bash
# (728) 恒等の判定（測った種で一致）の 1 コマンド：結果を 1 ビットも変えないはずの変更（整理・計器・器）が本当に恒等かを、基準の成績表と突き合わせて 1 枚に出す。
#
#   bash scripts/instruments/ident/ident.sh run   <ラベル> [--tree <木>] [宣言...]   起動して全部終わるまで走り、続けて check する（長い＝Bash を run_in_background で）
#   bash scripts/instruments/ident/ident.sh start <ラベル> [--tree <木>] [--work <作業フォルダ>] [--saveleak] [--no-ins] [--no-tests]   腕を起こして終わるまで走る（check はしない）
#   bash scripts/instruments/ident/ident.sh check <ラベル> [--work <作業フォルダ>] [--save <日付 YYYYMMDD>] [宣言...]                 走り終えた出力を基準と突き合わせる（数十秒〜数分）
#   bash scripts/instruments/ident/ident.sh mark  <ラベル>                                                                            印のファイルの状態（できたか・何本）
#
# 何を回すか（腕は同時に走る＝batch.sh の上限 POB_MAX_ARMS を守る）：
#   a ＝ 素の 5 種            batch.sh prep→run ia 5            ─ 成績表 b5 を基準の素の 5 種と全行（名乗り行を除く）で突き合わせる
#   b ＝ 計器つきの 10 種      INS=1 batch.sh run ib 10          ─ 成績表 b10・計器の表（約 380 本×10 種）・注視の表を基準と突き合わせる（計器の国は batch.sh の 9 国の既定）
#   t ＝ テストの数            Tests をビルドして --list-tests   ─ Category!=FullLength と FullLength の数を基準と突き合わせる
#   s ＝ savetest と leakhunt  （--saveleak を付けたときだけ）  ─ saveleak.sh：savetest OK と leakhunt の保存則違反なし
# 測るのは --tree の木（既定＝この台本のある木）。回す道具（batch.sh・measure.sh は木の物）。基準は ident.cfg（--cfg で替える。合流で新しい基線を作ったら cfg を書き換える）。
#
# 宣言（check に渡す・渡した物だけ許す。宣言が無ければ 1 バイトでも違えば〈不一致〉）：
#   --removed-tables <表の名前の頭,...>   基準にあって腕に無くてよい計器の表（消した計器）
#   --added-tables   <表の名前の頭,...>   腕にだけあってよい計器の表（足した計器）
#   --grown-ok       <表の名前の頭,...>   列が末尾に増えてよい表（足した列）
#   --rows-grown-ok  <表の名前の頭,...>   行が増えてよい表（基準の行〔共通の列〕が腕の行の順序つき部分列であること＝足した行だけが増える・(781)）
#   --known-extra    <表:列[@列=値],...>  既知の揺れに足す表と列（cfg の KNOWN 以外。@列=値 はその値の行だけ。表ごとの揺れの本数を報告に出す）
#   （flow_s は呼び出し位置 sourceFile・sourceLine を除いた行の多重集合で比べる＝並べ替え・移動は宣言なしで通る。2 つの呼び出し位置を 1 つに合わせると不一致＝その宣言はまだ無い）
#   --tests-delta    <整数>               Category!=FullLength の数の増減（消したメンバーのテスト分だけ減ってよい＝負の数）
#   --cost-allow     <件数の行の名前の頭,...>  .cost の件数の行が違ってよい線（コードの削除で件数が変わる〈番人を写しただけ〉の例外＝線ごとに名乗る）
#   --ng                                   ng.sh を回して NG の集合を基準と比べる（既定は回さない）
# 終了コード：0＝恒等・1＝不一致・2＝走りが足りない（出力が無い・腕の終了コードが 0 でない・出力の SOURCE-HASH が測る木の物と違う）。最後の行が  判定: 恒等  か  判定: 不一致  か  判定: 未了。
# start は同じラベルの前の回の出力を消してから走る（古い出力で〈恒等〉にならない）。
export PYTHONUTF8=1 AVALONIA_TELEMETRY_OPTOUT=1
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
SP="${IDENT_SP:-$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)}"      # scripts/instruments/ident（実行中に台本を書き換えられても壊れないよう、start／run は台本を作業フォルダへ写してそれを走らせる＝写しからは IDENT_SP で元の場所を渡す）
IS="$(cd "$SP/.." && pwd)"                               # scripts/instruments
ROOT="$(cd "$SP/../../.." && pwd)"                       # この台本のある木
CMD="${1:-}"; LABEL="${2:-}"
[ -n "$CMD" ] || { sed -n 2,30p "${BASH_SOURCE[0]}"; exit 2; }
shift 2 2>/dev/null
case "$CMD" in
  _arm)   # 内部：腕 1 本（xargs -P から呼ぶ）  _arm <腕> <ラベル> <木> <作業フォルダ> <フラグ>
    ARM="$LABEL"; L="$1"; TREE="$2"; W="$3"; FLAGS="$4"
    M="$W/marks"; rm -f "$M/$ARM.done"
    export POB_WORK="$W" POB_REPO="$TREE" MEASURE_CACHE=0
    case "$ARM" in
      a) bash "$IS/batch.sh" prep ia "dir:$TREE" plain && bash "$IS/batch.sh" run ia 5 ;;
      b) bash "$IS/batch.sh" prep ib "dir:$TREE" plain && { [ -s "$M/small.txt" ] && export POB_INSTRUMENTS_TABLES="$(cat "$M/small.txt")"; INS=1 bash "$IS/batch.sh" run ib 10; } ;;
      t) bash "$IS/batch.sh" prep it "dir:$TREE" plain && {
           T="$W/src_it"
           dotnet build "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --artifacts-path "$W/art_it" --disable-build-servers > "$W/tests_build.txt" 2>&1 || exit 1
           for f in "Category!=FullLength:nonfull" "Category=FullLength:full"; do   # 数えるのは ident_check.py（出力の tests_list_*.txt の 4 空白で始まる行）
             dotnet test "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --no-build --artifacts-path "$W/art_it" --list-tests --filter "${f%%:*}" > "$W/tests_list_${f##*:}.txt" 2>&1
           done
         } ;;
      s) POB_WORK="$W" SAVELEAK_SRC="$TREE" bash "$IS/saveleak.sh" ;;
    esac
    RC=$?
    echo "rc=$RC end=$(date '+%F %T')" > "$M/$ARM.done"
    exit 0 ;;
  start|run)
    TREE="$ROOT"; W=""; CFG=""; SAVELEAK=0; NOINS=0; NOTESTS=0; FORCE2=0; EXTRA=()
    while [ $# -gt 0 ]; do case "$1" in
      --tree) TREE="$2"; shift 2 ;; --work) W="$2"; shift 2 ;; --cfg) CFG="$2"; shift 2 ;;
      --saveleak) SAVELEAK=1; shift ;; --stage2) FORCE2=1; shift ;; --no-ins) NOINS=1; shift ;; --no-tests) NOTESTS=1; shift ;;
      *) EXTRA+=("$1"); shift ;; esac; done
    TREE="$(cd "$TREE" && pwd)"; W="${W:-/d/PoB_work/ident_$LABEL}"; mkdir -p "$W/marks"
    rm -f "$W/marks/"*.done "$W/tests_count.txt"
    # 前の回の出力を消す（同じラベルで回し直して腕が走らなかったとき、古い出力で〈恒等〉にならないように）。写しの木 src_*・ビルド art_* は prep が作り直す
    rm -rf "$W"/b5_ia_5.txt* "$W"/b10_ib_10.txt* "$W/mon_ib_10.txt" "$W/out_ia_5" "$W/out_ib_10" "$W"/tests_list_*.txt "$W/tests_build.txt" \
           "$W/savetest.txt" "$W/leakhunt.txt" "$W/saveleak.log" "$W/ng" "$W/marks/tree_hash.txt" "$W/prune_manifest.tsv" "$W/prune_deleted.txt" "$W/check_out.txt"
    # 測る木の SOURCE-HASH（measure.sh の src_manifest と同じ数え方＝写した後と同じ値）。check が腕 a・b のマニフェストと突き合わせる
    ( cd "$TREE" && find . \( -name bin -o -name obj -o -name docs -o -name dist -o -name .vs -o -name .git -o -name measurements -o -name uishot \) -prune \
        -o -type f \( -name '*.cs' -o -name '*.json' -o -name '*.axaml' -o -name '*.csproj' -o -name '*.slnx' \) -print | sort | xargs sha256sum ) | sha256sum | cut -c1-16 > "$W/marks/tree_hash.txt"
    # (1061) 判定の段：stage.py が基準のコミットとの差から決める（段 1 → 2 へ上げる --stage2 だけがある＝段 2 から 1 へ手で下げる口は無い）。段 1 の腕 b は SMALL の表だけを出す。
    CF0="${CFG:-$ROOT/scripts/instruments/ident/ident.cfg}"; SBASE=$(grep '^STAGE_BASE=' "$CF0" | head -1 | cut -d= -f2- | tr -d '\r')
    if [ -n "$SBASE" ]; then python -X utf8 -I "$SP/stage.py" "$TREE" "$SBASE" > "$W/marks/stage.txt" 2>&1; else printf '段=2\nSTAGE_BASE が cfg に無い\n' > "$W/marks/stage.txt"; fi
    [ "$FORCE2" = 1 ] && printf '段=2\n--stage2 で上げた\n' > "$W/marks/stage.txt"
    head -1 "$W/marks/stage.txt" | grep -q '^段=[12]$' || { echo "stage.py が段を返さなかった＝段 2 で回す" >&2; printf '段=2\nstage.py 失敗\n' > "$W/marks/stage.txt"; }
    if head -1 "$W/marks/stage.txt" | grep -q '^段=1$'; then grep '^SMALL=' "$CF0" | head -1 | cut -d= -f2- | tr -d '\r' > "$W/marks/small.txt"; else rm -f "$W/marks/small.txt"; fi
    echo "ident start：$(head -1 "$W/marks/stage.txt")（$(sed -n 2p "$W/marks/stage.txt")）"
    ARMS="a"; [ $NOINS = 0 ] && ARMS="$ARMS b"; [ $NOTESTS = 0 ] && ARMS="$ARMS t"; [ $SAVELEAK = 1 ] && ARMS="$ARMS s"
    echo "ident start：ラベル=$LABEL 木=$TREE 作業=$W 腕=[$ARMS] 開始=$(date '+%F %T')"
    echo "$ARMS" > "$W/marks/arms.txt"
    cp "$SP/ident.sh" "$W/ident_run.sh"; export IDENT_SP="$SP"   # 走っている最中に元の台本が書き換えられても、この走りは写しを読む
    printf '%s\n' $ARMS | xargs -P 4 -I{} bash "$W/ident_run.sh" _arm {} "$LABEL" "$TREE" "$W" "" > "$W/marks/start.log" 2>&1
    echo "rc=0 end=$(date '+%F %T')" > "$W/marks/all.done"
    echo "ident start：腕は全て終わった（$(ls "$W/marks" | grep -c '\.done$') 印）"
    if [ "$CMD" = run ]; then
      if [ "${PRUNE:-0}" != 1 ] || ! head -1 "$W/marks/stage.txt" | grep -q '^段=2$'; then   # 段 1 の生は小さい表だけ＝刈らない   # (1046) 既定（PRUNE 未設定）は今までと同じ exec
        exec bash "$W/ident_run.sh" check "$LABEL" --work "$W" --tree "$TREE" ${CFG:+--cfg "$CFG"} $([ $SAVELEAK = 1 ] && echo --saveleak) "${EXTRA[@]}"
      fi
      # (1046) PRUNE=1：判定の行が出た後に限り、基準と全体が一致した表の生を消す（tools/prune.py・manifest は $W/prune_manifest.tsv）。PRUNE_HEAVY=1 で重い 4 表も対象（HEAVY_KEEP=表の頭,... は残す）。
      bash "$W/ident_run.sh" check "$LABEL" --work "$W" --tree "$TREE" ${CFG:+--cfg "$CFG"} $([ $SAVELEAK = 1 ] && echo --saveleak) "${EXTRA[@]}" | tee "$W/check_out.txt"
      RCC=${PIPESTATUS[0]}
      CF="${CFG:-$ROOT/scripts/instruments/ident/ident.cfg}"
      BASEINS=$(grep '^INSRAW=' "$CF" | head -1 | cut -d= -f2- | tr -d '\r')
      KP=$(grep '^KNOWN=' "$CF" | head -1 | cut -d= -f2- | tr ',' '\n' | cut -d: -f1 | tr '\n' ',')$(grep '^PRUNE_KEEP=' "$CF" | head -1 | cut -d= -f2- | tr -d '\r'),   # (1061) NG の読み手の 4 表は刈らない
      i=0; while [ $i -lt ${#EXTRA[@]} ]; do case "${EXTRA[$i]}" in --added-tables|--grown-ok|--removed-tables|--rows-grown-ok|--known-extra) KP="$KP$(echo "${EXTRA[$((i+1))]}" | tr ',' '\n' | cut -d: -f1 | cut -d@ -f1 | tr '\n' ',')" ;; esac; i=$((i+1)); done
      SAVED=""; i=0; while [ $i -lt ${#EXTRA[@]} ]; do [ "${EXTRA[$i]}" = "--save" ] && SAVED="${EXTRA[$((i+1))]}_"; i=$((i+1)); done
      PRESPEC=$(grep '^PRESPEC=' "$CF" | head -1 | cut -d= -f2- | tr -d '\r'); [ -n "$PRESPEC" ] && PRESPEC="$ROOT/$PRESPEC"   # (1061) cfg の物（m1053 の決め打ちをやめた）
      # (1054) manifest は measurements/ident_<日付>_<ラベル>_manifest.tsv へ写す（--save が無ければ日付なし）。--allow は自分の作業フォルダの名だけ。
      python -X utf8 -I "$IS/tools/prune.py" prune "$W/out_ib_10/ins" "$BASEINS" --verdict-file "$W/check_out.txt" --manifest "$W/prune_manifest.tsv" --copy-to "$ROOT/measurements/ident_${SAVED}${LABEL}_manifest.tsv" ${PRESPEC:+--pre-spec "$PRESPEC"} --allow "$(basename "$W")" --deleted "$W/prune_deleted.txt" --keep-prefix "$KP" --heavy-keep "${HEAVY_KEEP:-}" $([ "${PRUNE_HEAVY:-0}" = 1 ] && echo --heavy) | tee -a "$W/check_out.txt"
      RCP=${PIPESTATUS[0]}
      # (1061) 刈り込みの結果を check_out の末尾 1 行で名乗る（拒み・刈らないも含む）
      echo "刈り込みの結果：rc=$RCP・$(grep -E '^(刈り込み|刈らない|拒む)' "$W/check_out.txt" | tail -1)" | tee -a "$W/check_out.txt"
      exit $RCC
    fi ;;
  check)
    exec python -I "$SP/ident_check.py" "$LABEL" --root "$ROOT" "$@" ;;
  mark)
    W="/d/PoB_work/ident_$LABEL"; echo "腕: $(cat "$W/marks/arms.txt" 2>/dev/null)"; for f in "$W"/marks/*.done; do [ -e "$f" ] && echo "$(basename "$f"): $(cat "$f")"; done ;;
  *) echo "不明な命令 $CMD"; exit 2 ;;
esac
