#!/bin/bash

set -e

SCRIPTDIR=$(dirname ${BASH_SOURCE[0]})
INTEROP_DIR="$SCRIPTDIR/../PrjFSLib.Linux.Managed/Interop"

CC=${CC:-cc}

TMPFILE=$(mktemp -t vfsforgit.tmp.XXXXXX) || exit 1
trap "rm -f -- '$TMPFILE'" EXIT

echo | >"$TMPFILE" \
  $CC -E -xc-header -dM $CFLAGS $CPPFLAGS -include projfs_notify.h -

PROJFS_CONSTS=$(cat "$TMPFILE" |
  awk '/^#define PROJFS_[^ ]+ [^ ]+$/ { print length($3) " " $0 }' |
  sort -k 1,1n -k4,4 | sed 's/.*PROJFS/PROJFS/' |
  sed 's/himask(0x\([0-9]\{4\}\))/0x\100000000/')

ERRNO_NAMES=$(cat "$INTEROP_DIR/Errno.cs" |
  grep 'Constants\.E' | sed 's/.*Constants\.E/E/' |
  sed 's/^\(E[A-Z]\+\).*/\1/' | sort -u |
  paste -s -d'|')

echo | >"$TMPFILE" \
  $CC -E -xc-header -dM $CFLAGS $CPPFLAGS -include errno.h -

ERRNO_CONSTS=$(cat "$TMPFILE" |
  grep -E "^#define (${ERRNO_NAMES}) [0-9]+" |
  sed 's/#define //' | sort -k2,2n)

exec >"$INTEROP_DIR/ProjFS.Constants.cs"
cat <<EOF
// This file is auto-generated by ProjFS.Linux/Script/GenerateConstants.sh.
// Any changes made directly in this file will be lost.
namespace PrjFSLib.Linux.Interop
{
    internal partial class ProjFS
    {
        internal static class Constants
        {
EOF

echo "$PROJFS_CONSTS" | while read name value; do
  cat <<EOF
            public const ulong $name = $value;
EOF
done

cat <<EOF
        }
    }
}
EOF

exec >"$INTEROP_DIR/Errno.Constants.cs"
cat <<EOF
// This file is auto-generated by ProjFS.Linux/Script/GenerateConstants.sh.
// Any changes made directly in this file will be lost.
namespace PrjFSLib.Linux.Interop
{
    internal static partial class Errno
    {
        internal static class Constants
        {
EOF

echo "$ERRNO_CONSTS" | while read name value; do
  cat <<EOF
            public const int $name = $value;
EOF
done

cat <<EOF
        }
    }
}
EOF

rm -f -- "$TMPFILE"
trap - EXIT
exit 0
