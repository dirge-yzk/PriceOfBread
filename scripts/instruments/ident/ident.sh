#!/usr/bin/env bash
# (728) 恒等の証明の 1 コマンド：結果を 1 ビットも変えないはずの変更（整理・計器・器）が本当に恒等かを、基準の成績表と突き合わせて 1 枚に出す。
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
#   --known-extra    <列名,...>           既知の揺れの列に足す列（cfg の KNOWN 以外。表ごとの揺れの本数を報告に出す）
#   --tests-delta    <整数>               Category!=FullLength の数の増減（消したメンバーのテスト分だけ減ってよい＝負の数）
#   --cost-allow     <件数の行の名前の頭,...>  .cost の件数の行が違ってよい線（コードの削除で件数が変わる〈番人を写しただけ〉の例外＝線ごとに名乗る）
#   --ng                                   ng.sh を回して NG の集合を基準と比べる（既定は回さない）
# 終了コード：0＝恒等・1＝不一致・2＝走りが足りない（出力が無い）。最後の行が  判定: 恒等  か  判定: 不一致  か  判定: 未了。
export PYTHONUTF8=1 AVALONIA_TELEMETRY_OPTOUT=1
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"      # scripts/instruments/ident
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
      b) bash "$IS/batch.sh" prep ib "dir:$TREE" plain && INS=1 bash "$IS/batch.sh" run ib 10 ;;
      t) bash "$IS/batch.sh" prep it "dir:$TREE" plain && {
           T="$W/src_it"
           dotnet build "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --artifacts-path "$W/art_it" --disable-build-servers > "$W/tests_build.txt" 2>&1 || exit 1
           for f in "Category!=FullLength:nonfull" "Category=FullLength:full"; do
             dotnet test "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --no-build --artifacts-path "$W/art_it" --list-tests --filter "${f%%:*}" > "$W/tests_list_${f##*:}.txt" 2>&1
             echo "${f##*:} $(awk '/The following Tests are available|次のテストを利用できます/{on=1;next} on && /^    [^ ]/{n++} END{print n+0}' "$W/tests_list_${f##*:}.txt")" >> "$W/tests_count.txt.tmp"
           done
           mv "$W/tests_count.txt.tmp" "$W/tests_count.txt"
         } ;;
      s) POB_WORK="$W" SAVELEAK_SRC="$TREE" bash "$IS/saveleak.sh" ;;
    esac
    RC=$?
    echo "rc=$RC end=$(date '+%F %T')" > "$M/$ARM.done"
    exit 0 ;;
  start|run)
    TREE="$ROOT"; W=""; CFG=""; SAVELEAK=0; NOINS=0; NOTESTS=0; EXTRA=()
    while [ $# -gt 0 ]; do case "$1" in
      --tree) TREE="$2"; shift 2 ;; --work) W="$2"; shift 2 ;; --cfg) CFG="$2"; shift 2 ;;
      --saveleak) SAVELEAK=1; shift ;; --no-ins) NOINS=1; shift ;; --no-tests) NOTESTS=1; shift ;;
      *) EXTRA+=("$1"); shift ;; esac; done
    TREE="$(cd "$TREE" && pwd)"; W="${W:-/d/PoB_work/ident_$LABEL}"; mkdir -p "$W/marks"
    rm -f "$W/marks/"*.done "$W/tests_count.txt"
    ARMS="a"; [ $NOINS = 0 ] && ARMS="$ARMS b"; [ $NOTESTS = 0 ] && ARMS="$ARMS t"; [ $SAVELEAK = 1 ] && ARMS="$ARMS s"
    echo "ident start：ラベル=$LABEL 木=$TREE 作業=$W 腕=[$ARMS] 開始=$(date '+%F %T')"
    echo "$ARMS" > "$W/marks/arms.txt"
    printf '%s\n' $ARMS | xargs -P 4 -I{} bash "$SP/ident.sh" _arm {} "$LABEL" "$TREE" "$W" "" > "$W/marks/start.log" 2>&1
    echo "rc=0 end=$(date '+%F %T')" > "$W/marks/all.done"
    echo "ident start：腕は全て終わった（$(ls "$W/marks" | grep -c '\.done$') 印）"
    if [ "$CMD" = run ]; then
      exec bash "$SP/ident.sh" check "$LABEL" --work "$W" --tree "$TREE" ${CFG:+--cfg "$CFG"} $([ $SAVELEAK = 1 ] && echo --saveleak) "${EXTRA[@]}"
    fi ;;
  check)
    exec python -I "$SP/ident_check.py" "$LABEL" --root "$ROOT" "$@" ;;
  mark)
    W="/d/PoB_work/ident_$LABEL"; echo "腕: $(cat "$W/marks/arms.txt" 2>/dev/null)"; for f in "$W"/marks/*.done; do [ -e "$f" ] && echo "$(basename "$f"): $(cat "$f")"; done ;;
  *) echo "不明な命令 $CMD"; exit 2 ;;
esac
