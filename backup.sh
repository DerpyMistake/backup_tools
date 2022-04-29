#!/bin/bash
#
# Create an archive/snapshot for the specified path and upload it to glacier storage.
#
# Usage:
#   backup.sh <path>
#

MY_DIR=$(dirname $(readlink -f $0))
ARCHIVE_SOURCE_PATH=$1
ARCHIVE_NAME=$(basename ${ARCHIVE_SOURCE_PATH})

CONFIG_LOC="$MY_DIR/app_config.sh"
if [[ -f $CONFIG_LOC ]]; then
    echo "$0: Loading Configuration"
    source $CONFIG_LOC
fi

if [[ ! -d ./snapshots ]]; then
    mkdir ./snapshots
fi

if [[ ! -f ./tools/MultipartBackup ]]; then
    ./publish.sh
fi

# If called with no parameters, we kick-off the backup
echo "$0: Performing backup for $ARCHIVE_NAME"

# Upload each of the segments
./tools/Sync -c -g "./snapshots/$ARCHIVE_NAME.sngz" --path="$ARCHIVE_SOURCE_PATH" \
    | openssl enc -aes-256-cbc -md sha512 -pbkdf2 -iter 100000 -salt -pass pass:$GC_PASSWORD \
    | gzip -c \
    | ./tools/MultipartBackup --app-config=$MY_DIR/app_config.json --chunk-size=128M --path="$MY_DIR/$ARCHIVE_SOURCE_PATH"
