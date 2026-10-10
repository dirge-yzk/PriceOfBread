#!/usr/bin/env bash
# 使い方: bash mkprobe.sh <出力フォルダ名> <dir:path> <probe cs ファイル>
set -e
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
D=/d/PoB_work/ars240/$1; SRC=$2; PR=$3
rm -rf $D && mkdir -p $D
tar -cf - -C ${SRC#dir:} --exclude=bin --exclude=obj --exclude=.git --exclude=docs --exclude=measurements --exclude=uishot --exclude=.vs --exclude=art . | tar -xf - -C $D
python - "$D/PriceOfBread.Runner/Program.cs" "$PR" <<'PY'
import sys
p=sys.argv[1]; q=sys.argv[2]
s=open(p,encoding='utf-8-sig').read()
blk=open(q,encoding='utf-8').read()
key='if (args.Length > 0 && args[0] == "selectprobe")'
assert key in s
s=s.replace(key, blk+'\n'+key,1)
open(p,'w',encoding='utf-8-sig').write(s)
PY
cd $D && dotnet build PriceOfBread.Runner/PriceOfBread.Runner.csproj -c Release --artifacts-path $D/art --disable-build-servers 2>&1 | tail -3
