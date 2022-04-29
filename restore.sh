#!/bin/bash

MY_DIR=$(dirname $(readlink -f $0))

while [ -n "$1" ]; do
    case "$1" in
        -a) shift && ARCHIVE_NAME=$1 ;;
        --out) shift && OUTPUT_PATH=$1 ;;
        --job) shift && JOB_ID=$1 ;;
        --) shift && break ;;
        *) shift ;;
    esac
    shift
done

CONFIG_LOC="$MY_DIR/app_config.sh"
if [[ -f $CONFIG_LOC ]]; then
    echo "$0: Loading Configuration"
    source $CONFIG_LOC
fi

if [[ -n "$JOB_ID" ]]; then
    mkdir "$OUTPUT_PATH"

    ./tools/Restore --job $JOB_ID \
        | gzip -cd \
        | openssl enc -d -aes-256-cbc -md sha512 -pbkdf2 -iter 100000 -salt -pass pass:$PASSWORD \
        | ./tools/Sync -x -g /dev/null --path="$ARCHIVE_SOURCE_PATH"

else
    # if no archive name is specified, we'll restore everything
    ./tools/Restore --app-config=app_config.json -a $ARCHIVE_NAME --cmd="/bin/bash" --args="./restore.sh --job \"\$JOB_ID\" --out \"\$OUTPUT_FOLDER\""
fi