(function () {
    async function api(method, args) {
        const response = await fetch(`/api/${method}`, {
            method: 'post',
            body: JSON.stringify(args),
            headers: { 'Content-Type': 'application/json' }
        });

        return await response.json();
    }

    const PushNoteType = {
        Success: 'success',
        Notice: 'notice',
        Error: 'error'
    }

    class vmPushNote {
        message = '';
        type = PushNoteType.Success;
        isHidden = ko.observable(false);
        isVisible = ko.observable(true);

        get icon() {
            switch (this.type) {
                case PushNoteType.Success: return 'fas fa-check';
                case PushNoteType.Notice: return 'fas fa-note';
                case PushNoteType.Error: return 'fas fa-exclamation-triangle';
            }
        }

        constructor(msg, type) {
            this.message = msg || '';
            this.type = type || PushNoteType.Success;

            setTimeout(this.hide.bind(this), 2000);
        }

        hide() {
            this.isHidden(true);

            setTimeout(function () {
                this.isVisible(false);
            }.bind(this), 1000);
        }
    }

    class vmApp {
        #origConfig = ko.observable();

        constructor() {
            this.loaded = ko.observable(false);
            this.notifications = ko.observableArray();
            this.backupFolders = ko.observableArray([]);
            this.logs = ko.observable();
            this.archives = ko.observable();
            this.config = ko.observable(new vmConfig(this));

            this.hasChanges = ko.computed(this.getHasChanges.bind(this));

            this.config.subscribe(this.onConfig.bind(this));
        }

        onConfig(config) {
            if (config.folders) {
                let checkedFolders = config.folders
                    .reduce(function (obj, f) { obj[f] = f; return obj; }, {});
            
                this.allBackupFolders()
                    .forEach(function (f) {
                        f.showChildren(false);
                        f.isChecked(!!checkedFolders[f.name]);
                    });
            }
            
            this.#origConfig(JSON.stringify(config.toJS()));
        }

        getHasChanges() {
            return this.#origConfig() != JSON.stringify(this.config().toJS());
        }

        allBackupFolders() {
            let res = [];

            function readFolder(f) {
                res.push(f);
                f.children.forEach(readFolder);
            }

            this.backupFolders().forEach(readFolder);
            return res;
        }

        pushNote(msg, type) {
            this.notifications.push(new vmPushNote(msg, type));
        }

        setBackupFolders(data) {
            let folders = [];
            let currNode = null;

            data.forEach(function (f) {
                let parent = currNode;
                while (parent && !f.startsWith(`${parent.path}/`)) {
                    parent = parent.parent;
                }

                currNode = new vmFolderInfo(parent, f);
                if (parent) {
                    parent.children.push(currNode);
                }
                else {
                    folders.push(currNode);
                }
            });

            this.backupFolders(folders);
        }

        async saveConfig() {
            let config = this.config().toJS();
            await api('save_config', config);

            this.#origConfig(JSON.stringify(config))
            this.pushNote('Configuration Saved')
        }

        async resetConfig() {
            let config = new vmConfig(this, await api('get_config'));
            this.config(config);

            this.pushNote('All Changes Reverted')
        }

        restartService() {

        }

        async executeBackup() {
            await api('start_backup', this.config().toJS());
        }
    }

    class vmConfig {
        constructor(app, config) {
            this.app = app;
            config = config || {};

            this.folders = config.folders || [];
            this.backupSchedule = ko.observable(config.backupSchedule || '0');
            this.tidySchedule = ko.observable(config.tidySchedule || '0');
            this.glacierConfig = ko.observable(new vmGlacierConfig(config.glacierConfig));
        }

        toJS() {
            return {
                backupSchedule: this.backupSchedule(),
                tidySchedule: this.tidySchedule(),
                glacierConfig: this.glacierConfig().toJS(),
                folders: this.app.allBackupFolders()
                    .filter(f => f.isChecked())
                    .map(f => f.path)
            };
        }
    }

    class vmGlacierConfig {
        vaultName = ko.observable();
        accessKeyID = ko.observable();
        secretAccessKey = ko.observable();
        region = ko.observable();
        encPassword = ko.observable();

        constructor(config) {
            config = config || {};
            this.vaultName(config.vaultName);
            this.accessKeyID(config.accessKeyID);
            this.secretAccessKey(config.secretAccessKey);
            this.region(config.region);
            this.encPassword(config.encPassword);
        }

        toJS() {
            return {
                vaultName: this.vaultName(),
                accessKeyID: this.accessKeyID(),
                secretAccessKey: this.secretAccessKey(),
                region: this.region(),
                encPassword: this.encPassword()
            };
        }
    }

    class vmFolderInfo {
        isChecked = null;
        showChildren = null;
        parent = null;
        children = [];

        get hasChildren() { return this.children.length > 0 }
        get allChildren() {
            let res = [];

            function addChild(ch) {
                res.push(ch);
                ch.children.forEach(addChild);
            }
            this.children.forEach(addChild);

            return res;
        }

        get displayName() {
            let res = this.path;
            if (this.parent) {
                res = this.path.substring(this.parent.path.length);
            }

            return res.replace(/^\//g, '');
        }
    
        constructor(parent, path) {
            this.parent = parent;
            this.path = path;
            this.name = path.split('\\').pop('/').split();
            this.isChecked = ko.observable(false);
            this.showChildren = ko.observable(false);
    
            this.isVisible = ko.computed(this.getIsVisible, this, { deferEvaluation: true });
        }

        getIsVisible() {
            let hasVisibleChildren = () => this.allChildren.filter(c => c.isChecked()).length > 0;
            return !this.parent || this.parent.showChildren() || this.isChecked() || hasVisibleChildren();
        }

        toggleChildVisibility() {
            this.showChildren(!this.showChildren());
        }
    }

    class vmArchiveCollection {
        entries = ko.observable([]);

        constructor(entries) {
            this.entries(entries.map(r => new vmArchiveEntry(r)));
            
            this.sortedEntries = ko.computed(this.getSortedEntries.bind(this));
        }

        getSortedEntries() {
            return this.entries().sort(function (a, b) {
                return -a.creationDate.localeCompare(b.creationDate);
            });
        }
    }

    class vmArchiveEntry {
        constructor(data) {
            $.extend(this, data || {});
            $.extend(this, this.metadata || { tags: [] });

            this.formattedDate = ko.computed(this.getFormattedDate.bind(this));
            this.formattedTime = ko.computed(this.getFormattedTime.bind(this));
            this.tagsDesc = this.tags.join(',');
        }

        getFormattedDate() {
            return new Date(this.creationDate).toLocaleDateString();
        }

        getFormattedTime() {
            return new Date(this.creationDate).toLocaleString().split(',')[1].trim();
        }
    }

    class vmLogCollection {
        entries = ko.observable([]);

        constructor(entries) {
            this.entries(entries.map(r => new vmLogEntry(r)));
            
            this.sortedEntries = ko.computed(this.getSortedEntries.bind(this));
            this.groupedEntries = ko.computed(this.getGroupedEntries.bind(this));
        }

        getGroupedEntries() {
            let res = [];

            let currGroup = null;
            this.sortedEntries().forEach(function (e) {
                if (!currGroup || currGroup.groupName !== e.appName) {
                    currGroup = {
                        groupName: e.appName,
                        entries: []
                    };
                    res.push(currGroup);
                }

                currGroup.entries.push(e);
            });

            return res;
        }

        getSortedEntries() {
            return this.entries().sort(function (a, b) {
                var appCmp = a.appName.toLowerCase().localeCompare(b.appName.toLowerCase());
                var timeCmp = -a.time.localeCompare(b.time);

                return appCmp === 0 ? timeCmp : appCmp;
            });
        }
    }

    class vmLogEntry {
        constructor(data) {
            this.appName = data.appName;
            this.time = data.time;
            this.context = data.context;
            this.message = data.message;
            this.type = data.type;

            this.formattedDate = ko.computed(this.getFormattedDate.bind(this));
            this.formattedTime = ko.computed(this.getFormattedTime.bind(this));
        }

        getFormattedDate() {
            return new Date(this.time).toLocaleDateString();
        }

        getFormattedTime() {
            return new Date(this.time).toLocaleString().split(',')[1].trim();
        }
    }

    function autoRefresh(collectionObservable, fetchCollection) {
        let intervalMS = 1000;
        let intervalCount = 0;

        let origJSON = JSON.stringify(collectionObservable());
        function startPolling() {
            setTimeout(async function refreshLogs() {
                let collection = await fetchCollection();
                if (JSON.stringify(collection) !== origJSON) {
                    intervalCount = 0;
                    
                    origJSON = JSON.stringify(collection);
                    collectionObservable(collection);
                }
                else {
                    intervalCount++;
                }

                // Update the polling interval based on the frequency of changes to the log
                if (intervalCount > 30) {
                    intervalMS = 60000;
                }
                if (intervalCount > 15) {
                    intervalMS = 15000;
                }
                else if (intervalCount > 5) {
                    intervalMS = 5000;
                }
                else {
                    intervalMS = 1000;
                }

                startPolling();
            }, intervalMS);
        }
        startPolling();
    }

    var loader = setInterval(async function () {
        if (document.readyState !== "complete") return;
        clearInterval(loader);

        let vm = new vmApp();
        ko.applyBindings(vm, document.body);

        vm.setBackupFolders((await api('get_backup_folders')));
        vm.logs(new vmLogCollection(await api('get_logs')));
        vm.archives(new vmArchiveCollection(await api('get_archives')));
        vm.config(new vmConfig(vm, await api('get_config')));

        vm.loaded(true);
        vm.pushNote('Loading Completed', PushNoteType.Success);
        
        autoRefresh(vm.logs, async function () {
            return new vmLogCollection(await api('get_logs'));
        });

        autoRefresh(vm.archives, async function () {
            return new vmArchiveCollection(await api('get_archives'));
        });
    }, 100);

    ko.bindingHandlers.tab = {
        init: function (el) {
            $(el).on('mousedown click', function (ev) {
                $(this).closest('li')
                    .add(this.getAttribute('href'))
                    .addClass('active')
                    .siblings('.active')
                    .removeClass('active');

                ev.preventDefault();
            });
        }
    }
})();
