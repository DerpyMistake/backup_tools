const http = require('http');
const fs = require('fs');
const path = require('path');
const { exec } = require('child_process');

// Server configuration
const CONFIG_FILE_PATH = 'app_config.json';
const BACKUP_ROOT_PATHS = [ '/mnt/p', '/mnt/z' ];
const hostname = '127.0.0.1';
const port = 3000;
const WWW_PATH = path.resolve(`${__dirname}/www/`);

console.log(`Using web path: ${WWW_PATH}`);

const Database = {
    LogEntries: JSON.parse(fs.readFileSync('db/logEntries.json')),
    BackupEntries: JSON.parse(fs.readFileSync('db/backupEntries.json')),
};

fs.watch('db', { }, function (type, path) {
    const fname = path.split('\\').pop().split('/').pop();
    const data = JSON.parse(fs.readFileSync('db/' + path));

    switch (fname) {
        case 'logEntries.json': Database.LogEntries = data; break;
        case 'backupEntries.json': Database.BackupEntries = data; break;
    }
});

const all_backup_folders = [];
BACKUP_ROOT_PATHS.forEach(function (p) {
    // Recurse through the hierarchy for each of our root backup paths
    function getFolders(rootPath, level) {
        if (level < 4) {
            all_backup_folders.push(rootPath);
            fs.readdirSync(rootPath, { withFileTypes: true })
                .filter(dirent => dirent.isDirectory())
                .forEach(function (f) {
                    try {
                        let fullPath = `${rootPath}/${f.name}`;

                        getFolders(fullPath, level + 1);
                    }
                    catch (ex) { }
                });
        }
    }

    getFolders(p, 0);
});


// API callbacks for the /api/ url
const API = {
    get_backup_folders: function (req) {
        return all_backup_folders;
    },

    save_config: function (config) {
        fs.writeFile(CONFIG_FILE_PATH, JSON.stringify(config, null, 4), function () { });

        console.log('Updating CRON Jobs');
        const updateSchedule = exec(`./update_cron.sh ${config.backupSchedule} ${config.tidySchedule}`, function (error, stdout, stderr) {
            if (error) {
                console.log(error.stack);
                console.log('Error code: ' + error.code);
                console.log('Signal received: ' + error.signal);
            }
        });

        updateSchedule.on('exit', function (code) {
            if (code) {
                console.log('\t - process exited with exit code ' + code);
            }
        });


        {
            let backupPaths = config.folders.map(f => `"${f}"`).join(' ');
            let gc = config.glacierConfig;
            let content = `
GC_VAULT_NAME="${gc.vaultName}}"
GC_ACCESS_KEY_ID="${gc.accessKeyID}}"
GC_SECRET_ACCESS_KEY="${gc.secretAccessKey}}"
GC_REGION="${gc.region}}"
GC_PASSWORD="${gc.encPassword}}"
BACKUP_PATHS=(${backupPaths})
`;
            fs.writeFile('app_config.sh', content, function () { });
        }
    },

    get_config: function () {
        if (fs.existsSync(CONFIG_FILE_PATH)) {
            return JSON.parse(fs.readFileSync(CONFIG_FILE_PATH));
        }
        else {
            return {};
        }
    },

    get_logs: function () {
        return Database.LogEntries;
    },

    get_archives: function () {
        return Database.BackupEntries;
    },

    start_backup: function (config) {
        config.folders.forEach(function (f) {
            console.log(`Starting Backups For ${config.glacierConfig.vaultName}:${f}`);
            const backup = exec(`./backup.sh ${f}`, function (error, stdout, stderr) {
                if (error) {
                    console.log(error.stack);
                    console.log('Error code: ' + error.code);
                    console.log('Signal received: ' + error.signal);
                }
            });

            backup.on('exit', function (code) {
                if (code) {
                    console.log('\t - process exited with exit code ' + code);
                }
            });
        });
    }
};

const extMimeTypes = {
    ico: 'image/vnd.microsoft.icon',
    js: 'application/javascript',
    json: 'application/json',
    css: 'text/css',
    html: 'text/html'
};

const server = http.createServer((req, res) => {
    const ext = req.url.substring(req.url.lastIndexOf('.') + 1);
    const fpath = path.resolve(`./www/${req.url}`);

    if (req.url.startsWith('/api/')) {
        let data = '';
        req.on('data', chunk => {
            data += chunk;
        })
        req.on('end', () => {
            const method = req.url.substring(5);
            if (API[method]) {
                let result = API[method](JSON.parse(data || '{}')) || {};
                res.writeHeader(200, { "Content-Type": 'application/json' });
                res.write(JSON.stringify(result));
            }
            res.end();
        });
    }
    else if (fpath.startsWith(WWW_PATH) && extMimeTypes[ext]) {
        outputFile(res, fpath, extMimeTypes[ext]);
    }
    else {
        outputFile(res, './www/index.html');
    }

    function outputFile(res, fname, mimeType) {
        fs.readFile(fname, function (err, content) {
            if (err) {
                res.writeHeader(404, { "Content-Type": "text/html" });
                console.error(err);
                res.end();
            }
            else {
                res.writeHeader(200, { "Content-Type": mimeType || "text/html" });
                res.write(content);
                res.end();
            }
        });
    }
});

server.listen(port, hostname, () => {
    console.log(`Server running at http://${hostname}:${port}/`);
});
