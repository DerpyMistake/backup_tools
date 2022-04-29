const fs = require('fs');
const crypto = require('crypto');

let args = {
    'chunk-size': '100M'
};

for (var i = 0; i < process.argv.length; i++) {
    let argName = process.argv[i].replace(/^-/g, '');
    if (process.argv[i].startsWith('--')) {
        args[argName] = process.argv[++i];
    }
    else if (process.argv[i].startsWith('-')) {
        args[argName] = process.argv[++i];
    }
}

process.stdin.on('readable', function () {
    let buffer = null;
    let chunkSize = parseBits(args['chunk-size']);

    while (process.stdin.readable) {
        buffer = process.stdin.read(chunkSize);
        if (!buffer) break;

        processData(buffer);
        buffer = null;
    }

    if (buffer) {
        processData(buffer);
    }
});

const tmpFile = tempFileName();
if (fs.existsSync('output.zip')) {
    fs.unlinkSync('output.zip');
}

function processData(data) {
    fs.writeFileSync('output.zip', data, { flag: 'as' });

    fs.writeFileSync(tmpFile, data);
    fs.unlinkSync(tmpFile);    
}

function parseBits(size) {
    let res = parseInt(size);
    
    // accumulate the multipliers for each level
    switch (size[size.length - 1].toUpperCase()) {
        case 'G': res *= 1024;
        case 'M': res *= 1024;
        case 'K': res *= 1024;
    }

    return res;
}

function tempFileName(prefix) {
    prefix = prefix || 'tmp';

    do {
        var res = prefix + crypto.randomBytes(16).toString('base64').replace(/\//, '_');
    } while (fs.existsSync(res));

    return res;
}
